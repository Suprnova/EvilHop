using EvilHop.Common;
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
public sealed partial class CutsceneAsset() : Asset(AssetType.Cutscene), IPhysicalCutsceneAsset
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
    public override IPhysicalCutsceneAsset Physical => this;

    private AssetId? _overriddenAssetId;
    AssetId IPhysicalCutsceneAsset.AssetId
    {
        get => _overriddenAssetId ?? Id;
        set => _overriddenAssetId = value == Id ? null : value;
    }

    private uint? _overriddenNumData;
    uint IPhysicalCutsceneAsset.NumData
    {
        get => _overriddenNumData ?? (uint)Data.Count;
        set => _overriddenNumData = value == (uint)Data.Count ? null : value;
    }

    private uint _numTime;
    uint IPhysicalCutsceneAsset.NumTime { get => _numTime; set => _numTime = value; }

    private uint _maxModel;
    uint IPhysicalCutsceneAsset.MaxModel { get => _maxModel; set => _maxModel = value; }

    private uint _maxBufEven;
    uint IPhysicalCutsceneAsset.MaxBufEven { get => _maxBufEven; set => _maxBufEven = value; }

    private uint _maxBufOdd;
    uint IPhysicalCutsceneAsset.MaxBufOdd { get => _maxBufOdd; set => _maxBufOdd = value; }

    private uint _headerSize;
    uint IPhysicalCutsceneAsset.HeaderSize { get => _headerSize; set => _headerSize = value; }

    private uint _visCount;
    uint IPhysicalCutsceneAsset.VisCount { get => _visCount; set => _visCount = value; }

    private uint _visSize;
    uint IPhysicalCutsceneAsset.VisSize { get => _visSize; set => _visSize = value; }

    private uint _breakCount;
    uint IPhysicalCutsceneAsset.BreakCount { get => _breakCount; set => _breakCount = value; }

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Cutscene"/> is known to be read by.
    /// </summary>
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
    };
}

/// <summary>
/// An explicit interface used to interact with <see cref="CutsceneAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalCutsceneAsset : IPhysicalAsset
{
    /// <summary>
    /// The <see cref="Asset"/>'s ID, as stored in the <see cref="CutsceneAsset"/>'s own header. This
    /// field is stored independently from <see cref="Asset.Id"/>, mirroring
    /// <see cref="IPhysicalBaseAsset.BaseId"/>.
    /// </summary>
    AssetId AssetId { get; set; }
    /// <summary>
    /// The number of <see cref="CutsceneAsset.Data"/> entries, read directly from the header.
    /// </summary>
    /// TODO: update to reflect the documentation pattern for other collection count fields in
    /// Physical
    uint NumData { get; set; }
    /// <summary>
    /// The number of TimeChunk offsets following the (currently unparsed) region after
    /// <see cref="CutsceneAsset.Data"/>.
    /// </summary>
    uint NumTime { get; set; }
    /// <summary>The largest model, in bytes including padding, in the unparsed chunk data.</summary>
    uint MaxModel { get; set; }
    /// <summary>The largest TimeChunk with an even <c>ChunkIndex</c>, in bytes including padding.</summary>
    uint MaxBufEven { get; set; }
    /// <summary>The largest TimeChunk with an odd <c>ChunkIndex</c>, in bytes including padding.</summary>
    uint MaxBufOdd { get; set; }
    /// <summary>
    /// The size, in bytes, of this asset's header region - this fixed header, <see cref="CutsceneAsset.Data"/>,
    /// and the unparsed TimeChunk-offset/visibility/break tables that follow it, before the
    /// chunked model/animation/camera/sound data begins.
    /// </summary>
    uint HeaderSize { get; set; }
    /// <summary>The number of visibility entries in the unparsed region.</summary>
    uint VisCount { get; set; }
    /// <summary>The total size, in 4-byte steps, of the visibility entries in the unparsed region.</summary>
    uint VisSize { get; set; }
    /// <summary>The number of break entries in the unparsed region.</summary>
    uint BreakCount { get; set; }
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
}

/// <summary>
/// Represents all known values for <see cref="CutsceneDataEntry.DataType"/>.
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
public readonly record struct CutsceneAudioTrack(AssetId LeftSoundId, AssetId RightSoundId, string LeftSound, string RightSound);
