using EvilHop.Common;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

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
