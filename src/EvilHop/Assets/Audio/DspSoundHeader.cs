using EvilHop.Common;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// A Nintendo DSP-ADPCM sound header, describing one sound's sample rate, decoder coefficients, and
/// loop points, and linking it to its <see cref="AssetType.Sound"/>, <see cref="AssetType.StreamingSound"/>,
/// or <see cref="AssetType.CutsceneStreamingSound"/> asset.
/// </summary>
public sealed class DspSoundHeader
{
    /// <summary>The number of raw (decoded) samples in the sound.</summary>
    public uint SampleCount { get; set; }

    /// <summary>The number of ADPCM nibbles in the sound, including frame headers.</summary>
    public uint NibbleCount { get; set; }

    /// <summary>The sound's sample rate, in Hz.</summary>
    public uint SampleRate { get; set; }

    /// <summary>Whether the sound loops.</summary>
    public bool IsLooped { get; set; }

    /// <summary>Unknown.</summary>
    public ushort Format { get; set; }

    /// <summary>The loop start offset, in nibbles.</summary>
    public uint LoopStart { get; set; }

    /// <summary>The loop end offset, in nibbles.</summary>
    public uint LoopEnd { get; set; }

    /// <summary>The decoder's initial offset value.</summary>
    public uint InitialOffset { get; set; }

    /// <summary>The sound's 16 ADPCM decoder coefficients.</summary>
    public Collection<short> Coefficients { get; } = new([.. new short[16]]);

    /// <summary>Unknown gain factor.</summary>
    public ushort Gain { get; set; }

    /// <summary>
    /// The predictor and scale value of the sound's first ADPCM frame, used to initialize the
    /// decoder.
    /// </summary>
    public ushort PredictorScale { get; set; }

    /// <summary>Decoder history data, used to maintain decoder state during sample playback.</summary>
    public short History1 { get; set; }

    /// <inheritdoc cref="History1"/>
    public short History2 { get; set; }

    /// <summary>
    /// The predictor and scale value for the loop point's ADPCM frame. Zero if <see cref="IsLooped"/>
    /// is <see langword="false"/>.
    /// </summary>
    public ushort LoopPredictorScale { get; set; }

    /// <summary>
    /// Decoder history data for the loop point. Zero if <see cref="IsLooped"/> is
    /// <see langword="false"/>.
    /// </summary>
    public short LoopHistory1 { get; set; }

    /// <inheritdoc cref="LoopHistory1"/>
    public short LoopHistory2 { get; set; }

    /// <summary>Unknown.</summary>
    public Collection<byte> Unknown { get; } = new([.. new byte[22]]);

    /// <summary>
    /// The <see cref="AssetType.Sound"/>, <see cref="AssetType.StreamingSound"/>, or
    /// <see cref="AssetType.CutsceneStreamingSound"/> asset this header describes.
    /// </summary>
    public AssetId SoundAssetId { get; set; }
}
