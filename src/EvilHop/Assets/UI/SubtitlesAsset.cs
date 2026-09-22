using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Text;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> defining timed subtitle text lines displayed during cutscenes or gameplay.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SUBT">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed class SubtitlesAsset() : BaseAsset(AssetType.Subtitles, baseType: 0x00), Physical.ISubtitlesAsset
{
    /// <summary>
    /// The subtitle lines in this asset, displayed in sequence.
    /// </summary>
    public Collection<SubtitleLine> Lines { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ISubtitlesAsset Physical => this;

    private ushort? _overriddenNumLines;
    ushort Physical.ISubtitlesAsset.NumLines
    {
        get => _overriddenNumLines ?? (ushort)Lines.Count;
        set => _overriddenNumLines = value == (ushort)Lines.Count ? null : value;
    }

    private ushort? _overriddenByteCount;
    ushort Physical.ISubtitlesAsset.ByteCount
    {
        get => _overriddenByteCount ?? CalculateByteCount();
        set => _overriddenByteCount = value == CalculateByteCount() ? null : value;
    }

    internal ushort CalculateByteCount()
    {
        int total = Lines.Count * 12;
        foreach (var line in Lines)
            total += Encoding.Latin1.GetByteCount(line.Text ?? string.Empty) + 1;
        total += (4 - total % 4) % 4;
        return (ushort)total;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Subtitles"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.Incredibles,
        GameVersion.ROTU,
    };

    internal static SubtitlesAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new SubtitlesAsset();
        AssetFields.Populate(asset, header, debug);
        BaseAssetPrefix.Read(asset, reader);

        ushort numLines = reader.ReadUInt16();
        ushort byteCount = reader.ReadUInt16();

        var descriptors = new (float StartTime, float StopTime, uint StringOffset)[numLines];
        for (int i = 0; i < numLines; i++)
        {
            descriptors[i] = (reader.ReadSingle(), reader.ReadSingle(), reader.ReadUInt32());
        }

        int descriptorSize = numLines * 12;
        int stringPoolSize = byteCount >= descriptorSize ? byteCount - descriptorSize : 0;
        byte[] stringPool = reader.ReadBytes(stringPoolSize);

        for (int i = 0; i < numLines; i++)
        {
            var (StartTime, StopTime, StringOffset) = descriptors[i];
            string text = string.Empty;
            if (StringOffset < stringPool.Length)
            {
                int start = (int)StringOffset;
                int nullIndex = Array.IndexOf(stringPool, (byte)0, start);
                int length = nullIndex >= 0 ? nullIndex - start : stringPool.Length - start;
                text = Encoding.Latin1.GetString(stringPool, start, length);
            }
            asset.Lines.Add(new SubtitleLine
            {
                StartTime = StartTime,
                StopTime = StopTime,
                Text = text,
            });
        }

        asset.Physical.NumLines = numLines;
        asset.Physical.ByteCount = byteCount;

        for (var i = 0; i < asset.Physical.LinkCount; i++)
            asset.Links.Add(Link.Read(reader, profile));
        asset.Physical.LinkCount = (byte)asset.Links.Count;
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(SubtitlesAsset asset, EndianWriter writer, FormatProfile profile)
    {
        BaseAssetPrefix.Write(asset, writer);

        writer.Write(asset.Physical.NumLines);
        writer.Write(asset.Physical.ByteCount);

        int currentOffset = 0;
        foreach (var line in asset.Lines)
        {
            writer.Write(line.StartTime);
            writer.Write(line.StopTime);
            writer.Write((uint)currentOffset);
            currentOffset += Encoding.Latin1.GetByteCount(line.Text ?? string.Empty) + 1;
        }

        foreach (var line in asset.Lines)
        {
            byte[] textBytes = Encoding.Latin1.GetBytes(line.Text ?? string.Empty);
            writer.Write(textBytes);
            writer.Write((byte)0);
        }

        int poolPadding = asset.Physical.ByteCount - (asset.Lines.Count * 12) - currentOffset;
        if (poolPadding > 0) writer.Write(new byte[poolPadding]);

        foreach (var link in asset.Links)
            Link.Write(link, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }

    /// <summary>
    /// A single timed subtitle line within a <see cref="SubtitlesAsset"/>.
    /// </summary>
    public sealed class SubtitleLine
    {
        /// <summary>The time, in seconds, when this subtitle line begins displaying.</summary>
        public float StartTime { get; set; }

        /// <summary>The time, in seconds, when this subtitle line stops displaying.</summary>
        public float StopTime { get; set; }

        /// <summary>The text displayed for this subtitle line.</summary>
        public string Text { get; set; } = string.Empty;
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="SubtitlesAsset"/>'s underlying values.
    /// </summary>
    public interface ISubtitlesAsset : IBaseAsset
    {
        /// <summary>
        /// The number of subtitle lines stored in this asset.
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="SubtitlesAsset.Lines"/>.Count exist, this field wins during serialization.
        /// </remarks>
        ushort NumLines { get; set; }

        /// <summary>
        /// The byte count of this asset following the 12-byte header (line descriptors and string pool).
        /// </summary>
        /// <remarks>
        /// When disagreements with <see cref="SubtitlesAsset.Lines"/> exist, this field wins during serialization.
        /// </remarks>
        ushort ByteCount { get; set; }
    }
}
