using EvilHop.Common;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// The xCutsceneInfo header - the portion of the <see cref="AssetType.Cutscene"/> format duplicated
/// verbatim, sans the alignment padding that precedes it in a standalone file, by every
/// <see cref="CutsceneTableAsset"/> entry. Implemented by <see cref="CutsceneAsset"/> and
/// <see cref="CutsceneTableEntry"/> so <see cref="CutsceneAsset.ReadHeader"/>/
/// <see cref="CutsceneAsset.WriteHeader"/> can serve both without either type pretending to be the
/// other.
/// </summary>
/// TODO: i don't like the physical layer here, i'd prefer the header having every field like
/// normal and CutsceneAsset's Physical layer just binding to a private Header instance instead.
internal interface ICutsceneHeader
{
    /// <inheritdoc cref="CutsceneAsset.Data"/>
    Collection<CutsceneDataEntry> Data { get; }

    /// <inheritdoc cref="CutsceneAsset.SoundLeft"/>
    string SoundLeft { get; set; }

    /// <inheritdoc cref="CutsceneAsset.SoundRight"/>
    string SoundRight { get; set; }

    /// <inheritdoc cref="CutsceneAsset.AudioTracks"/>
    Collection<CutsceneAudioTrack> AudioTracks { get; }

    /// <inheritdoc cref="Asset.Physical"/>
    IPhysicalCutsceneHeader Physical { get; }

    /// <inheritdoc cref="Asset.GetUnparsedTail"/>
    Span<byte> GetUnparsedTail();

    /// <inheritdoc cref="Asset.SetUnparsedTail"/>
    void SetUnparsedTail(byte[] bytes);
}

/// <summary>
/// An explicit interface used to interact with an <see cref="ICutsceneHeader"/>'s underlying values.
/// </summary>
public interface IPhysicalCutsceneHeader
{
    /// <summary>
    /// The <see cref="Common.AssetId"/> stored in the header itself.
    /// </summary>
    AssetId AssetId { get; set; }
    /// <summary>
    /// The number of <see cref="ICutsceneHeader.Data"/> entries, read directly from the header.
    /// </summary>
    /// TODO: update to reflect the documentation pattern for other collection count fields in
    /// Physical
    uint NumData { get; set; }
    /// <summary>
    /// The number of TimeChunk offsets following the (currently unparsed) region after
    /// <see cref="ICutsceneHeader.Data"/>.
    /// </summary>
    uint NumTime { get; set; }
    /// <summary>The largest model, in bytes including padding, in the unparsed chunk data.</summary>
    uint MaxModel { get; set; }
    /// <summary>The largest TimeChunk with an even <c>ChunkIndex</c>, in bytes including padding.</summary>
    uint MaxBufEven { get; set; }
    /// <summary>The largest TimeChunk with an odd <c>ChunkIndex</c>, in bytes including padding.</summary>
    uint MaxBufOdd { get; set; }
    /// <summary>
    /// The size, in bytes, of this header region - the fixed header, <see cref="ICutsceneHeader.Data"/>,
    /// and the unparsed TimeChunk-offset/visibility/break tables that follow it, before a standalone
    /// <see cref="CutsceneAsset"/>'s chunked model/animation/camera/sound data begins.
    /// </summary>
    uint HeaderSize { get; set; }
    /// <summary>The number of visibility entries in the unparsed region.</summary>
    uint VisCount { get; set; }
    /// <summary>The total size, in 4-byte steps, of the visibility entries in the unparsed region.</summary>
    uint VisSize { get; set; }
    /// <summary>The number of break entries in the unparsed region.</summary>
    uint BreakCount { get; set; }
}
