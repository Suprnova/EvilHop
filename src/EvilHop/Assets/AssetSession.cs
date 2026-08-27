using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

/// <summary>
/// Owns an <see cref="Archive"/>'s assets for the duration of a scope. Opening one detaches the
/// blocks that describe assets from the block tree and parses them into <see cref="Layer"/>s of
/// <see cref="Asset"/>s; committing rebuilds those blocks and reattaches them.
/// </summary>
/// <remarks>
/// <para>
/// While a session is open, the block-level view of <c>ATOC</c>, <c>LTOC</c>, and <c>DPAK</c> is
/// unavailable: their fields are locked, and a reference captured before opening is orphaned.
/// <c>HIPA</c> and <c>PACK</c> stay attached and fully editable throughout.
/// </para>
/// <para>
/// Commit is total - every asset serializes unconditionally - so there is no failure path, which is
/// what makes committing from <see cref="Dispose"/> safe.
/// </para>
/// </remarks>
public sealed class AssetSession : IDisposable
{
    /// <summary>The archive's <see cref="Layer"/>s, in the order they are listed on disk.</summary>
    public IReadOnlyList<Layer> Layers => _layers;
    private readonly List<Layer> _layers = [];

    /// <summary>
    /// Problems encountered while opening, one line each. An asset that fails to parse degrades to
    /// its untyped form and is reported here rather than throwing.
    /// </summary>
    public IReadOnlyList<string> Diagnostics => _diagnostics;
    private readonly List<string> _diagnostics = [];

    /// <summary>
    /// The assets whose bytes differ from what they were at open, computed during
    /// <see cref="Commit"/> and empty before it. Compared using this session's own checksum of the
    /// bytes actually read, not the archive's stored <see cref="AssetDebug.Checksum"/>, which is
    /// occasionally already wrong on disk.
    /// </summary>
    public IReadOnlyList<AssetId> ChangedAssets { get; private set; } = [];

    private readonly Archive _archive;
    private readonly Blocks.Dictionary _dictionary;
    private readonly StreamData _streamData;
    private readonly uint _assetInfValue;
    private readonly uint _layerInfValue;
    private readonly byte _fillByte;
    private readonly List<AssetId> _originalAtocOrder;
    private readonly System.Collections.Generic.Dictionary<AssetId, uint> _openChecksums;
    private readonly System.Collections.Generic.Dictionary<(AssetId Before, AssetId After), byte[]> _capturedGaps;
    private HashSet<AssetId> _changedLookup = [];
    private bool _committed;

    private AssetSession(
        Archive archive,
        Blocks.Dictionary dictionary,
        StreamData streamData,
        uint assetInfValue,
        uint layerInfValue,
        byte fillByte,
        List<AssetId> originalAtocOrder,
        System.Collections.Generic.Dictionary<AssetId, uint> openChecksums,
        System.Collections.Generic.Dictionary<(AssetId, AssetId), byte[]> capturedGaps)
    {
        _archive = archive;
        _dictionary = dictionary;
        _streamData = streamData;
        _assetInfValue = assetInfValue;
        _layerInfValue = layerInfValue;
        _fillByte = fillByte;
        _originalAtocOrder = originalAtocOrder;
        _openChecksums = openChecksums;
        _capturedGaps = capturedGaps;
    }

    /// <summary>The fill byte assumed when an archive carries no padding to sample one from.</summary>
    private const byte DefaultFillByte = 0x33;

    /// <summary>The alignment assumed for an asset that declares a non-positive one.</summary>
    private const uint DefaultAlignment = 16;

    /// <summary><c>DPAK</c>'s data begins on a boundary of this many bytes.</summary>
    private const int DataAlignment = 32;

    internal static AssetSession Open(Archive archive)
    {
        var dictionary = archive.Roots.OfType<Blocks.Dictionary>().Single();
        var stream = archive.Roots.OfType<AssetStream>().Single();
        var assetTable = dictionary.AssetTable;
        var layerTable = dictionary.LayerTable;
        var streamData = stream.Data;

        var headers = assetTable.Headers.ToList();
        long dataStart = MeasureLength(archive) - streamData.Data.Length;

        var session = new AssetSession(
            archive,
            dictionary,
            streamData,
            assetTable.Inf.Value,
            layerTable.Inf.Value,
            streamData.Padding.Length > 0 ? streamData.Padding[0] : DefaultFillByte,
            [.. headers.Select(h => new AssetId(h.Id))],
            [],
            []);

        session.CaptureGaps(headers, dataStart);
        session.ParseLayers(layerTable, headers, dataStart);

        // Locked only after parsing - reading these blocks is how the assets get built.
        assetTable.LockFields();
        layerTable.LockFields();
        streamData.LockFields();

        dictionary.AssetTable = null!;
        dictionary.LayerTable = null!;
        streamData.UnlockFields();
        streamData.Data = [];
        streamData.LockFields();

        return session;
    }

    private void CaptureGaps(List<AssetHeader> headers, long dataStart)
    {
        var ordered = headers.OrderBy(h => h.Offset).ToList();
        for (int i = 0; i < ordered.Count - 1; i++)
        {
            uint gapStart = ordered[i].Offset + ordered[i].Size;
            if (ordered[i + 1].Offset <= gapStart) continue;

            uint gapSize = ordered[i + 1].Offset - gapStart;
            if (!TryGetRange(dataStart, _streamData.Data.Length, gapStart, gapSize, out var range)) continue;

            _capturedGaps[(new AssetId(ordered[i].Id), new AssetId(ordered[i + 1].Id))] =
                _streamData.Data[range];
        }
    }

    private void ParseLayers(LayerTable layerTable, List<AssetHeader> headers, long dataStart)
    {
        var byId = new System.Collections.Generic.Dictionary<uint, AssetHeader>();
        foreach (var header in headers) byId.TryAdd(header.Id, header);

        var seen = new HashSet<uint>();

        foreach (var layerHeader in layerTable.Headers)
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
                    _diagnostics.Add($"Asset 0x{id:X8} is listed by a layer but has no ATOC entry. Skipped.");
                    continue;
                }

                if (!seen.Add(id))
                {
                    _diagnostics.Add(
                        $"Asset 0x{id:X8} is listed more than once. The duplicate listing is dropped, " +
                        $"so this archive will not round-trip byte-for-byte.");
                    continue;
                }

                layer.Add(ParseOne(header, dataStart));
            }

            _layers.Add(layer);
        }
    }

    private Asset ParseOne(AssetHeader header, long dataStart)
    {
        var debug = header.Debug;
        ReadOnlySpan<byte> slice = TryGetRange(dataStart, _streamData.Data.Length, header.Offset, header.Size, out var range)
            ? _streamData.Data.AsSpan(range)
            : [];

        if (range.Equals(default(Range)) && header.Size > 0)
            _diagnostics.Add($"Asset 0x{header.Id:X8} ({header.Type}) declares bytes outside DPAK. Degraded to empty.");

        _openChecksums[new AssetId(header.Id)] = Crc32Mpeg2.Compute(slice);

        try
        {
            return AssetCodecs.Read(slice, header, debug, _archive.Serializer.Profile);
        }
        catch (Exception ex)
        {
            _diagnostics.Add($"Asset 0x{header.Id:X8} ({header.Type}) failed to parse: {ex.Message}. Degraded to raw bytes.");
            var fallback = new GenericAsset();
            AssetFields.Populate(fallback, header, debug);
            fallback.SetUnparsedTail(slice.ToArray());
            return fallback;
        }
    }

    /// <summary>
    /// Rebuilds <c>ATOC</c>, <c>LTOC</c>, and <c>DPAK</c> from the current assets and reattaches
    /// them. Calling this twice is a no-op the second time.
    /// </summary>
    public void Commit()
    {
        if (_committed) return;
        _committed = true;

        var ordered = _layers.SelectMany(layer => layer.Assets).ToList();
        var serialized = new System.Collections.Generic.Dictionary<AssetId, byte[]>();
        var checksums = new System.Collections.Generic.Dictionary<AssetId, uint>();

        foreach (var asset in ordered)
        {
            using var buffer = new MemoryStream();
            using (var writer = new BinaryWriter(buffer, System.Text.Encoding.ASCII, leaveOpen: true))
                AssetCodecs.Write(asset, writer, _archive.Serializer.Profile);

            byte[] bytes = buffer.ToArray();
            serialized[asset.Id] = bytes;
            checksums[asset.Id] = Crc32Mpeg2.Compute(bytes);
        }

        ChangedAssets = [.. checksums
            .Where(pair => !_openChecksums.TryGetValue(pair.Key, out uint open) || open != pair.Value)
            .Select(pair => pair.Key)];
        _changedLookup = [.. ChangedAssets];

        var headers = BuildHeaders(ordered, serialized, checksums);
        var assetTable = BuildAssetTable(headers);
        var layerTable = BuildLayerTable();

        _dictionary.AssetTable = assetTable;
        _dictionary.LayerTable = layerTable;

        _streamData.UnlockFields();
        _streamData.PaddingAmount = 0;
        _streamData.Padding = [];
        _streamData.Data = [];

        long dataStart = MeasureLength(_archive);
        int paddingAmount = (int)((DataAlignment - dataStart % DataAlignment) % DataAlignment);
        _streamData.PaddingAmount = (uint)paddingAmount;
        _streamData.Padding = FillBytes(paddingAmount);
        dataStart += paddingAmount;

        _streamData.Data = BuildData(ordered, serialized, headers, dataStart);
    }

    private System.Collections.Generic.Dictionary<AssetId, AssetHeader> BuildHeaders(
        List<Asset> ordered,
        System.Collections.Generic.Dictionary<AssetId, byte[]> serialized,
        System.Collections.Generic.Dictionary<AssetId, uint> checksums)
    {
        var headers = new System.Collections.Generic.Dictionary<AssetId, AssetHeader>();

        foreach (var asset in ordered)
        {
            var header = _archive.Serializer.CreateBlock<AssetHeader>();
            var debug = _archive.Serializer.CreateBlock<AssetDebug>();
            AssetFields.Apply(asset, header, debug);

            header.Size = (uint)serialized[asset.Id].Length;
            debug.Checksum = checksums[asset.Id];
            header.Debug = debug;

            headers[asset.Id] = header;
        }

        return headers;
    }

    private AssetTable BuildAssetTable(System.Collections.Generic.Dictionary<AssetId, AssetHeader> headers)
    {
        var table = _archive.Serializer.CreateBlock<AssetTable>();
        var inf = _archive.Serializer.CreateBlock<AssetInf>();
        inf.Value = _assetInfValue;
        table.Inf = inf;

        foreach (var id in ReplayAtocOrder(headers.Keys))
            table.Children.Add(headers[id]);

        return table;
    }

    /// <summary>
    /// Rebuilds <c>ATOC</c>'s ordering: surviving assets keep their captured relative order, removed
    /// ones simply drop out, and assets added during the session land at the end.
    /// </summary>
    private List<AssetId> ReplayAtocOrder(IEnumerable<AssetId> currentIds)
    {
        var current = currentIds.ToList();
        var present = current.ToHashSet();
        var replayed = _originalAtocOrder.Where(present.Contains).ToList();
        var replayedSet = replayed.ToHashSet();

        return [.. replayed, .. current.Where(id => !replayedSet.Contains(id))];
    }

    private LayerTable BuildLayerTable()
    {
        var table = _archive.Serializer.CreateBlock<LayerTable>();
        var inf = _archive.Serializer.CreateBlock<LayerInf>();
        inf.Value = _layerInfValue;
        table.Inf = inf;

        foreach (var layer in _layers)
        {
            var header = _archive.Serializer.CreateBlock<LayerHeader>();
            var debug = _archive.Serializer.CreateBlock<LayerDebug>();
            debug.Value = layer.DebugValue;

            header.Type = layer.Type;
            header.AssetCount = (uint)layer.Assets.Count;
            header.AssetIds = [.. layer.Assets.Select(asset => asset.Id.Value)];
            header.Debug = debug;

            table.Children.Add(header);
        }

        return table;
    }

    /// <summary>
    /// Lays every asset out in layer order, assigning <see cref="AssetHeader.Offset"/> and
    /// <see cref="AssetHeader.Plus"/> as it goes, and returns the resulting <c>DPAK</c> data.
    /// </summary>
    private byte[] BuildData(
        List<Asset> ordered,
        System.Collections.Generic.Dictionary<AssetId, byte[]> serialized,
        System.Collections.Generic.Dictionary<AssetId, AssetHeader> headers,
        long dataStart)
    {
        var lastInLayer = _layers
            .Where(layer => layer.Assets.Count > 0)
            .Select(layer => layer.Assets[^1])
            .ToHashSet();

        using var data = new MemoryStream();
        long position = dataStart;

        for (int i = 0; i < ordered.Count; i++)
        {
            var asset = ordered[i];
            byte[] bytes = serialized[asset.Id];
            var header = headers[asset.Id];

            header.Offset = (uint)position;
            data.Write(bytes);
            position += bytes.Length;

            if (i == ordered.Count - 1 || lastInLayer.Contains(asset))
            {
                header.Plus = 0;
                continue;
            }

            var next = ordered[i + 1];
            int nextAlignment = headers[next.Id].Debug.Alignment;
            uint alignment = nextAlignment > 0 ? (uint)nextAlignment : DefaultAlignment;
            uint plus = (uint)((alignment - position % alignment) % alignment);

            header.Plus = plus;
            data.Write(GapBytesFor(asset.Id, next.Id, (int)plus));
            position += plus;
        }

        return data.ToArray();
    }

    /// <summary>
    /// The bytes filling the gap between two adjacent assets: the originals if both ends are
    /// unchanged and still adjacent, otherwise flat fill.
    /// </summary>
    /// <remarks>
    /// Real archives are not reliably homogeneous here - a small but real fraction of gaps in every
    /// game carry something other than the fill byte - so what was there is replayed wherever it can
    /// still be trusted.
    /// </remarks>
    private byte[] GapBytesFor(AssetId before, AssetId after, int expectedLength)
    {
        bool unchanged = !_changedLookup.Contains(before) && !_changedLookup.Contains(after);

        return unchanged
            && _capturedGaps.TryGetValue((before, after), out byte[]? captured)
            && captured.Length == expectedLength
                ? captured
                : FillBytes(expectedLength);
    }

    private byte[] FillBytes(int length)
    {
        var bytes = new byte[length];
        Array.Fill(bytes, _fillByte);
        return bytes;
    }

    /// <summary>
    /// The serialized length of <paramref name="archive"/> in its current state, measured by writing
    /// it without retaining the bytes.
    /// </summary>
    /// <remarks>
    /// Used to locate where <c>DPAK</c>'s data begins, which is the archive's length minus that
    /// data's own length. Measuring rather than computing keeps block-envelope sizing in
    /// <c>Serializer</c> alone.
    /// </remarks>
    private static long MeasureLength(Archive archive)
    {
        using var counter = new LengthMeasuringStream();
        archive.Save(counter);
        return counter.Length;
    }

    private static bool TryGetRange(long dataStart, int dataLength, uint offset, uint size, out Range range)
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

    /// <inheritdoc/>
    public void Dispose() => Commit();

    /// <summary>
    /// A write-only <see cref="Stream"/> that records how many bytes pass through it without
    /// keeping them. Seekable, because <c>Serializer</c> backpatches each block's size field.
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
