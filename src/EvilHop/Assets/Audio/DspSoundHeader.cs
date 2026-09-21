using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
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

    internal static DspSoundHeader Read(EndianReader reader, FormatProfile _)
    {
        var header = new DspSoundHeader
        {
            SampleCount = reader.ReadUInt32(),
            NibbleCount = reader.ReadUInt32(),
            SampleRate = reader.ReadUInt32(),
            IsLooped = reader.ReadUInt16() != 0,
            Format = reader.ReadUInt16(),
            LoopStart = reader.ReadUInt32(),
            LoopEnd = reader.ReadUInt32(),
            InitialOffset = reader.ReadUInt32(),
        };
        for (int i = 0; i < header.Coefficients.Count; i++) header.Coefficients[i] = reader.ReadInt16();
        header.Gain = reader.ReadUInt16();
        header.PredictorScale = reader.ReadUInt16();
        header.History1 = reader.ReadInt16();
        header.History2 = reader.ReadInt16();
        header.LoopPredictorScale = reader.ReadUInt16();
        header.LoopHistory1 = reader.ReadInt16();
        header.LoopHistory2 = reader.ReadInt16();
        for (int i = 0; i < header.Unknown.Count; i++) header.Unknown[i] = reader.ReadByte();
        header.SoundAssetId = reader.ReadAssetId();
        return header;
    }

    internal static void Write(DspSoundHeader value, EndianWriter writer, FormatProfile _)
    {
        writer.Write(value.SampleCount);
        writer.Write(value.NibbleCount);
        writer.Write(value.SampleRate);
        writer.Write((ushort)(value.IsLooped ? 1 : 0));
        writer.Write(value.Format);
        writer.Write(value.LoopStart);
        writer.Write(value.LoopEnd);
        writer.Write(value.InitialOffset);
        foreach (short coefficient in value.Coefficients) writer.Write(coefficient);
        writer.Write(value.Gain);
        writer.Write(value.PredictorScale);
        writer.Write(value.History1);
        writer.Write(value.History2);
        writer.Write(value.LoopPredictorScale);
        writer.Write(value.LoopHistory1);
        writer.Write(value.LoopHistory2);
        foreach (byte b in value.Unknown) writer.Write(b);
        writer.Write(value.SoundAssetId);
    }
}
