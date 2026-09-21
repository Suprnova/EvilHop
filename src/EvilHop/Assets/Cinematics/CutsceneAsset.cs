using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A cinematic sequence of streamed camera, animation, sound, and model data, played back from
/// aligned chunks.
/// </summary>
/// <remarks>
/// <para>
/// Only this asset's fixed-size header and its table of referenced models are currently modelled.
/// The chunked data is available via <see cref="Asset.GetUnparsedTail"/>, and will be implemented
/// properly once their dependent asset types are appropriately modelled. 
/// </para>
/// <seealso href="https://heavyironmodding.org/wiki/CSN">Heavy Iron Modding documentation</seealso>
/// </remarks>
// TODO: Partial implementation - chunked media data is unmodelled and preserved in unparsed tail
public sealed class CutsceneAsset() : Asset(AssetType.Cutscene), ICutsceneHeader, Physical.ICutsceneAsset
{
    /// <summary>
    /// The models this cutscene needs loaded before playback.
    /// </summary>
    public Collection<CutsceneDataEntry> Data { get; } = [];

    /// <summary>
    /// The left-channel sound name for this cutscene's dialog track.
    /// Only present in <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public string SoundLeft { get; set; } = string.Empty;

    /// <summary>
    /// The right-channel sound name for this cutscene's dialog track.
    /// Only present in <see cref="GameVersion.N100F"/> and <see cref="GameVersion.BFBB"/>.
    /// </summary>
    public string SoundRight { get; set; } = string.Empty;

    /// <summary>
    /// Up to 32 stereo sound track slots this cutscene can play from.
    /// Only present in <see cref="GameVersion.TSSM"/> and <see cref="GameVersion.Incredibles"/>.
    /// </summary>
    public Collection<CutsceneAudioTrack> AudioTracks { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ICutsceneAsset Physical => this;

    Physical.ICutsceneHeader ICutsceneHeader.Physical => this;

    private AssetId? _overriddenAssetId;
    AssetId Physical.ICutsceneHeader.AssetId
    {
        get => _overriddenAssetId ?? Id;
        set => _overriddenAssetId = value == Id ? null : value;
    }

    private uint? _overriddenNumData;
    uint Physical.ICutsceneHeader.NumData
    {
        get => _overriddenNumData ?? (uint)Data.Count;
        set => _overriddenNumData = value == (uint)Data.Count ? null : value;
    }

    private uint _numTime;
    uint Physical.ICutsceneHeader.NumTime { get => _numTime; set => _numTime = value; }

    private uint _maxModel;
    uint Physical.ICutsceneHeader.MaxModel { get => _maxModel; set => _maxModel = value; }

    private uint _maxBufEven;
    uint Physical.ICutsceneHeader.MaxBufEven { get => _maxBufEven; set => _maxBufEven = value; }

    private uint _maxBufOdd;
    uint Physical.ICutsceneHeader.MaxBufOdd { get => _maxBufOdd; set => _maxBufOdd = value; }

    private uint _headerSize;
    uint Physical.ICutsceneHeader.HeaderSize { get => _headerSize; set => _headerSize = value; }

    private uint _visCount;
    uint Physical.ICutsceneHeader.VisCount { get => _visCount; set => _visCount = value; }

    private uint _visSize;
    uint Physical.ICutsceneHeader.VisSize { get => _visSize; set => _visSize = value; }

    private uint _breakCount;
    uint Physical.ICutsceneHeader.BreakCount { get => _breakCount; set => _breakCount = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Cutscene"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
    };

    internal static CutsceneAsset Read(EndianReader reader, AssetHeader header, AssetDebug debug, FormatProfile profile)
    {
        var asset = new CutsceneAsset();
        AssetFields.Populate(asset, header, debug);
        ICutsceneHeader.ReadHeader(asset, reader, profile);
        asset.SetUnparsedTail(reader.ReadRemainingBytes());
        return asset;
    }

    internal static void Write(CutsceneAsset asset, EndianWriter writer, FormatProfile profile)
    {
        ICutsceneHeader.WriteHeader(asset, writer, profile);
        writer.Write(asset.GetUnparsedTail());
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="CutsceneAsset"/>'s underlying values.
    /// </summary>
    /// <remarks>
    /// Adds nothing beyond <see cref="ICutsceneHeader"/> - it exists purely so
    /// <see cref="Asset.Physical"/> can be covariantly overridden with <see cref="IAsset"/>
    /// included, which <see cref="ICutsceneHeader"/> itself deliberately omits so it can also be
    /// implemented by the non-<see cref="Asset"/> <see cref="CutsceneTableEntry"/>.
    /// </remarks>
    public interface ICutsceneAsset : IAsset, ICutsceneHeader;
}

/// <summary>
/// One entry in a <see cref="CutsceneAsset.Data"/> table, referencing a model this cutscene needs
/// loaded before playback.
/// </summary>
public record struct CutsceneDataEntry
{
    /// <summary>Which kind of model this entry is.</summary>
    public CutsceneDataType DataType { get; set; }

    /// <summary>The <see cref="Common.AssetId"/> of the referenced model.</summary>
    public AssetId AssetId { get; set; }

    /// <summary>The size, in bytes, of this model's data within the cutscene's unparsed chunk data.</summary>
    public uint ChunkSize { get; set; }

    /// <summary>
    /// The offset, in bytes, to this model's data within the cutscene's unparsed chunk data. Zero
    /// alongside a zero <see cref="ChunkSize"/> when the model is instead an external
    /// <see cref="AssetType.Model"/> asset.
    /// </summary>
    public uint FileOffset { get; set; }

    internal static CutsceneDataEntry Read(EndianReader reader, FormatProfile _) => new()
    {
        DataType = (CutsceneDataType)reader.ReadUInt32(),
        AssetId = reader.ReadAssetId(),
        ChunkSize = reader.ReadUInt32(),
        FileOffset = reader.ReadUInt32(),
    };

    internal static void Write(CutsceneDataEntry value, EndianWriter writer, FormatProfile _)
    {
        writer.Write((uint)value.DataType);
        writer.Write(value.AssetId);
        writer.Write(value.ChunkSize);
        writer.Write(value.FileOffset);
    }
}

/// <summary>
/// Identifies the media or animation stream type contained in a cutscene data chunk.
/// </summary>
public enum CutsceneDataType : uint
{
    /// <summary>An embedded RenderWare clump model.</summary>
    RWModel = 1,
    /// <summary>
    /// A JDTM model. Not present in <see cref="GameVersion.N100F"/>.
    /// </summary>
    JdtmModel = 6,
}

/// <summary>
/// One stereo sound track slot in a <see cref="GameVersion.TSSM"/>/<see cref="GameVersion.Incredibles"/>
/// <see cref="CutsceneAsset"/>.
/// </summary>
public readonly record struct CutsceneAudioTrack(AssetId LeftSoundId, AssetId RightSoundId, string LeftSound, string RightSound)
{
    internal static CutsceneAudioTrack Read(EndianReader reader, int soundLength, FormatProfile _) => new(
        reader.ReadAssetId(),
        reader.ReadAssetId(),
        ICutsceneHeader.ReadFixedString(reader, soundLength),
        ICutsceneHeader.ReadFixedString(reader, soundLength));

    internal static void Write(CutsceneAudioTrack value, EndianWriter writer, int soundLength, FormatProfile _)
    {
        writer.Write(value.LeftSoundId);
        writer.Write(value.RightSoundId);
        ICutsceneHeader.WriteFixedString(writer, value.LeftSound, soundLength);
        ICutsceneHeader.WriteFixedString(writer, value.RightSound, soundLength);
    }
}
