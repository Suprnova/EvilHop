using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// Where an asset's bytes sat in the archive when it was read.
/// </summary>
/// <param name="Id">The asset's ID, as its <see cref="AssetHeader"/> stated it.</param>
/// <param name="Offset">The absolute offset of the asset's bytes in the archive.</param>
/// <param name="Size">The length of the asset's bytes.</param>
internal readonly record struct AssetPlacement(AssetId Id, uint Offset, uint Size);

/// <summary>
/// An archive's assets as read from its blocks.
/// </summary>
/// <param name="Layers">The parsed layers, in the order they are listed on disk.</param>
/// <param name="Diagnostics">Problems encountered while parsing.</param>
/// <param name="Placements">Every asset's placement, in the order <see cref="AssetTable"/> lists them.</param>
/// <param name="PlacementOf">Each parsed asset's placement, matched by reference.</param>
/// <param name="Checksums">Each asset's checksum, computed over its bytes as read.</param>
/// <param name="Data">The <see cref="StreamData"/> data the assets were read from.</param>
/// <param name="DataStart">The absolute offset of <paramref name="Data"/> in the archive.</param>
internal sealed record AssetRead(
    List<Layer> Layers,
    List<AssetDiagnostic> Diagnostics,
    List<AssetPlacement> Placements,
    Dictionary<Asset, AssetPlacement> PlacementOf,
    Dictionary<AssetId, uint> Checksums,
    byte[] Data,
    long DataStart);

/// <summary>
/// Parses an <see cref="Archive"/>'s asset blocks into <see cref="Layer"/>s of <see cref="Asset"/>s,
/// leaving the blocks as they are.
/// </summary>
internal static class AssetReader
{
    /// <summary>
    /// The profile to read <paramref name="archive"/>'s assets with: its serializer's, with
    /// <paramref name="supplied"/> filling in a platform the serializer doesn't state.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when neither states a platform, or when they state different ones.
    /// </exception>
    internal static FormatProfile ResolveProfile(Archive archive, Platform? supplied)
    {
        var stated = archive.Serializer.Profile;
        if (stated.Platform is { } statedPlatform && supplied is { } suppliedPlatform && statedPlatform != suppliedPlatform)
            throw new InvalidOperationException(
                $"This archive's {nameof(Serializer)} states {statedPlatform}, so assets can't be opened as {suppliedPlatform}.");

        var platform = stated.Platform ?? supplied
            ?? throw new InvalidOperationException(
                $"Assets can't be opened without a platform: this archive's {nameof(Serializer)} states none.");
        return stated with { Platform = platform };
    }

    /// <summary>
    /// Parses every layer and asset <paramref name="archive"/>'s blocks describe under
    /// <paramref name="profile"/>. An asset that fails to parse degrades to its untyped form and is
    /// reported in <see cref="AssetRead.Diagnostics"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a session owns <paramref name="archive"/>'s assets: their blocks are detached.
    /// </exception>
    internal static AssetRead Read(Archive archive, FormatProfile profile)
    {
        if (archive.OpenSession is not null)
            throw new InvalidOperationException(
                $"This archive's assets are open in an {nameof(AssetSession)}. Commit it before reading them again.");

        var dictionary = archive.Roots.OfType<Dictionary>().Single();
        var stream = archive.Roots.OfType<AssetStream>().Single();
        byte[] data = stream.Data.Data;

        var headers = dictionary.AssetTable.Headers.ToList();
        var read = new AssetRead(
            [],
            [],
            [.. headers.Select(PlacementOf)],
            new(ReferenceEqualityComparer.Instance),
            [],
            data,
            MeasureThrough(archive, stream) - data.Length);

        var byId = new Dictionary<uint, AssetHeader>();
        foreach (var header in headers) byId.TryAdd(header.Id, header);

        var seen = new HashSet<uint>();

        foreach (var layerHeader in dictionary.LayerTable.Headers)
        {
            var layer = new Layer
            {
                Type = layerHeader.Type,
                DebugValue = layerHeader.Debug.Value
            };

            foreach (uint id in layerHeader.AssetIds)
            {
                if (!byId.TryGetValue(id, out var header))
                {
                    read.Diagnostics.Add(new AssetDiagnostic(new AssetId(id), "Listed by a layer but has no ATOC entry. Skipped."));
                    continue;
                }

                if (!seen.Add(id))
                {
                    read.Diagnostics.Add(new AssetDiagnostic(new AssetId(id), "Listed more than once. The duplicate listing is dropped."));
                    continue;
                }

                var asset = ParseOne(header, read, profile);
                read.PlacementOf[asset] = PlacementOf(header);
                layer.Add(asset);
            }

            read.Layers.Add(layer);
        }

        return read;
    }

    private static AssetPlacement PlacementOf(AssetHeader header) => new(new AssetId(header.Id), header.Offset, header.Size);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Each asset is parsed independently; a malformed one must not abort the rest of the archive.")]
    private static Asset ParseOne(AssetHeader header, AssetRead read, FormatProfile profile)
    {
        byte[] data = read.Data;
        var debug = header.Debug;
        bool inRange = TryGetRange(read.DataStart, data.Length, header.Offset, header.Size, out var range);
        ReadOnlySpan<byte> slice = inRange ? data.AsSpan(range) : [];

        if (!inRange && header.Size > 0)
            read.Diagnostics.Add(new AssetDiagnostic(new AssetId(header.Id), $"({header.Type}) declares bytes outside DPAK. Degraded to empty."));

        uint computed = Crc32Mpeg2.Compute(slice);
        read.Checksums[new AssetId(header.Id)] = computed;

        if (header.Size == 0)
        {
            var empty = new EmptyAsset(header.Type);
            AssetFields.Populate(empty, header, debug);
            return AdoptChecksum(empty, computed, debug);
        }

        try
        {
            var (offset, length) = range.GetOffsetAndLength(data.Length);
            using var stream = new MemoryStream(data, offset, length, writable: false);
            using var reader = new EndianReader(stream, profile.Endianness);
            return AdoptChecksum(AssetCodecs.Read(reader, header, debug, profile), computed, debug);
        }
        catch (Exception ex)
        {
            read.Diagnostics.Add(new AssetDiagnostic(new AssetId(header.Id), $"({header.Type}) failed to parse: {ex.Message}. Degraded to raw bytes."));
            var fallback = new GenericAsset(header.Type);
            AssetFields.Populate(fallback, header, debug);
            fallback.SetUnparsedTail(slice.ToArray());
            return AdoptChecksum(fallback, computed, debug);
        }
    }

    /// <summary>
    /// Gives <paramref name="asset"/> the checksum its <see cref="AssetDebug"/> declared, against a
    /// baseline of what its data actually hashes to.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="Asset.ComputedChecksum"/> first is what makes the two agreeing the normal
    /// case and leaves no override behind. They disagree only in archives that shipped a wrong
    /// checksum, and there the declared value is recorded as an override so it survives back out.
    /// </remarks>
    private static Asset AdoptChecksum(Asset asset, uint computed, AssetDebug debug)
    {
        asset.ComputedChecksum = computed;
        asset.Physical.Checksum = debug.Checksum;
        return asset;
    }

    /// <summary>
    /// The range of <see cref="StreamData"/>'s data holding the <paramref name="size"/> bytes at
    /// absolute <paramref name="offset"/>, when the data holds all of them.
    /// </summary>
    internal static bool TryGetRange(long dataStart, int dataLength, uint offset, uint size, out Range range)
    {
        long start = offset - dataStart;
        if (start < 0 || size > int.MaxValue || start + size > dataLength)
        {
            range = default;
            return false;
        }

        range = new Range((int)start, (int)(start + size));
        return true;
    }

    /// <summary>
    /// The serialized length of <paramref name="archive"/>'s roots up to and including
    /// <paramref name="stream"/>, measured by writing them without retaining the bytes.
    /// </summary>
    /// <remarks>
    /// <see cref="StreamData"/>'s data ends where <paramref name="stream"/> does, not where the
    /// archive does: a <see cref="HIPB"/> can follow it.
    /// </remarks>
    internal static long MeasureThrough(Archive archive, AssetStream stream)
    {
        var roots = archive.Roots.TakeWhile(root => root != stream).Append(stream);
        using var counter = new LengthMeasuringStream();
        archive.Serializer.Write(counter, roots);
        return counter.Length;
    }

    /// <summary>
    /// A write-only <see cref="Stream"/> that records how many bytes pass through it without
    /// keeping them. Seekable, because <see cref="Serializer"/> backpatches each block's size field.
    /// </summary>
    private sealed class LengthMeasuringStream : Stream
    {
        private long _position;
        private long _length;

        public override bool CanRead => false;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public override void Write(byte[] buffer, int offset, int count) =>
            Advance(count);

        public override void Write(ReadOnlySpan<byte> buffer) =>
            Advance(buffer.Length);

        public override void WriteByte(byte value) => Advance(1);

        private void Advance(int count)
        {
            _position += count;
            if (_position > _length) _length = _position;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                _ => _length + offset
            };
            return _position;
        }

        public override void SetLength(long value) => _length = value;
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
