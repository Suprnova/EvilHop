using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// A table of every <see cref="CutsceneAsset"/> header in the level, letting the game look up a
/// cutscene's metadata without loading its full streamed file.
/// </summary>
/// <remarks>
/// Only the header fields and referenced-model table are modelled; the trailing TimeChunk-offset/
/// visibility/break tables aren't parsed yet, so each <see cref="CutsceneTableEntry"/> keeps them in
/// its own <see cref="CutsceneTableEntry.GetUnparsedTail"/>, same as a standalone
/// <see cref="CutsceneAsset"/> keeps its chunked media there.
/// <seealso href="https://heavyironmodding.org/wiki/CTOC">Heavy Iron Modding documentation</seealso>
/// </remarks>
// TODO: Partial implementation - trailing TimeChunk-offset, visibility, and break tables are unmodelled and preserved in unparsed tail
public sealed class CutsceneTableAsset() : Asset(AssetType.CutsceneTable), IPhysicalCutsceneTableAsset
{
    /// <summary>
    /// The listed cutscenes' headers.
    /// </summary>
    public Collection<CutsceneTableEntry> Cutscenes { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalCutsceneTableAsset Physical => this;

    private uint? _overriddenCount;
    uint IPhysicalCutsceneTableAsset.Count
    {
        get => _overriddenCount ?? (uint)Cutscenes.Count;
        set => _overriddenCount = value == (uint)Cutscenes.Count ? null : value;
    }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.CutsceneTable"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
    };

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HeaderReader's result never owns a resource worth disposing - see its remarks.")]
    internal static CutsceneTableAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new CutsceneTableAsset();
        AssetFields.Populate(asset, header, debug);
        reader = CutsceneAsset.HeaderReader(reader, profile);

        uint count = reader.ReadUInt32();
        for (int i = 0; i < count; i++)
        {
            var entry = new CutsceneTableEntry();
            long start = reader.BaseStream.Position;
            CutsceneAsset.ReadHeader(entry, reader, profile);
            int consumed = (int)(reader.BaseStream.Position - start);
            entry.SetUnparsedTail(reader.ReadBytes((int)entry.Physical.HeaderSize - consumed));
            asset.Cutscenes.Add(entry);
        }
        asset.Physical.Count = (uint)asset.Cutscenes.Count;

        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HeaderWriter's result never owns a resource worth disposing - see HeaderReader's remarks.")]
    internal static void Write(CutsceneTableAsset asset, EndianWriter writer, FormatProfile profile)
    {
        writer = CutsceneAsset.HeaderWriter(writer, profile);
        writer.Write(asset.Physical.Count);
        foreach (var entry in asset.Cutscenes)
        {
            CutsceneAsset.WriteHeader(entry, writer, profile);
            writer.Write(entry.GetUnparsedTail());
        }
        writer.Write(asset.GetUnparsedTail());
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="CutsceneTableAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalCutsceneTableAsset : IPhysicalAsset
{
    /// <summary>
    /// The number of <see cref="CutsceneTableAsset.Cutscenes"/> stored for this asset, read directly
    /// from its leading count field.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="CutsceneTableAsset.Cutscenes"/>.Count exist, this field
    /// wins during serialization.
    /// </remarks>
    uint Count { get; set; }
}

/// <summary>
/// One <see cref="CutsceneTableAsset.Cutscenes"/> entry: a <see cref="CutsceneAsset"/>'s xCutsceneInfo
/// header, duplicated verbatim from its standalone file.
/// </summary>
/// <remarks>
/// Unlike <see cref="CutsceneAsset"/>, this is not an <see cref="Asset"/> - it has no AHDR/ADBG block
/// of its own, and (since it is never followed by the chunked media a header's
/// <see cref="IPhysicalCutsceneHeader.HeaderSize"/> and <c>xCutsceneData</c> offsets describe) never
/// will.
/// </remarks>
public sealed class CutsceneTableEntry : ICutsceneHeader, IPhysicalCutsceneHeader
{
    /// <inheritdoc cref="CutsceneAsset.Data"/>
    public Collection<CutsceneDataEntry> Data { get; } = [];

    /// <inheritdoc cref="CutsceneAsset.SoundLeft"/>
    public string SoundLeft { get; set; } = string.Empty;

    /// <inheritdoc cref="CutsceneAsset.SoundRight"/>
    public string SoundRight { get; set; } = string.Empty;

    /// <inheritdoc cref="CutsceneAsset.AudioTracks"/>
    public Collection<CutsceneAudioTrack> AudioTracks { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public IPhysicalCutsceneHeader Physical => this;

    private AssetId _assetId;
    AssetId IPhysicalCutsceneHeader.AssetId { get => _assetId; set => _assetId = value; }

    private uint? _overriddenNumData;
    uint IPhysicalCutsceneHeader.NumData
    {
        get => _overriddenNumData ?? (uint)Data.Count;
        set => _overriddenNumData = value == (uint)Data.Count ? null : value;
    }

    private uint _numTime;
    uint IPhysicalCutsceneHeader.NumTime { get => _numTime; set => _numTime = value; }

    private uint _maxModel;
    uint IPhysicalCutsceneHeader.MaxModel { get => _maxModel; set => _maxModel = value; }

    private uint _maxBufEven;
    uint IPhysicalCutsceneHeader.MaxBufEven { get => _maxBufEven; set => _maxBufEven = value; }

    private uint _maxBufOdd;
    uint IPhysicalCutsceneHeader.MaxBufOdd { get => _maxBufOdd; set => _maxBufOdd = value; }

    private uint _headerSize;
    uint IPhysicalCutsceneHeader.HeaderSize { get => _headerSize; set => _headerSize = value; }

    private uint _visCount;
    uint IPhysicalCutsceneHeader.VisCount { get => _visCount; set => _visCount = value; }

    private uint _visSize;
    uint IPhysicalCutsceneHeader.VisSize { get => _visSize; set => _visSize = value; }

    private uint _breakCount;
    uint IPhysicalCutsceneHeader.BreakCount { get => _breakCount; set => _breakCount = value; }

    private byte[] _unparsedTail = [];

    /// <inheritdoc cref="Asset.GetUnparsedTail"/>
    public Span<byte> GetUnparsedTail() => _unparsedTail;

    /// <inheritdoc cref="Asset.SetUnparsedTail"/>
    public void SetUnparsedTail(byte[] bytes) => _unparsedTail = bytes;
}
