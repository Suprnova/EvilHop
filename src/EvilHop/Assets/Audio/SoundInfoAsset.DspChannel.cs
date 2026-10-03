using EvilHop.Primitives;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

public partial class SoundInfoAsset
{
    /// <summary>
    /// The Nintendo DSP-ADPCM decoder state for one channel of a GameCube-encoded <see cref="Sample"/>.
    /// </summary>
    public sealed class DspChannel
    {
        /// <summary>The channel's 16 ADPCM decoder coefficients.</summary>
        public Collection<short> Coefficients { get; } = new([.. new short[16]]);

        /// <summary>Unknown gain factor.</summary>
        public ushort Gain { get; set; }

        /// <summary>
        /// The predictor and scale value of the channel's first ADPCM frame, used to initialize the
        /// decoder.
        /// </summary>
        public ushort PredictorScale { get; set; }

        /// <summary>Decoder history data, used to maintain decoder state during sample playback.</summary>
        public short History1 { get; set; }

        /// <inheritdoc cref="History1"/>
        public short History2 { get; set; }

        /// <summary>The predictor and scale value for the loop point's ADPCM frame.</summary>
        public ushort LoopPredictorScale { get; set; }

        /// <summary>Decoder history data for the loop point.</summary>
        public short LoopHistory1 { get; set; }

        /// <inheritdoc cref="LoopHistory1"/>
        public short LoopHistory2 { get; set; }

        /// <summary>The size of one channel's decoder state on disk, in bytes.</summary>
        internal const int Size = 0x2E;

        /// <param name="reader">A <see cref="Endianness.Big"/> reader - the state is big-endian even inside little-endian FSB3 banks.</param>
        internal static DspChannel Read(EndianReader reader)
        {
            var channel = new DspChannel();
            for (int i = 0; i < channel.Coefficients.Count; i++) channel.Coefficients[i] = reader.ReadInt16();
            channel.Gain = reader.ReadUInt16();
            channel.PredictorScale = reader.ReadUInt16();
            channel.History1 = reader.ReadInt16();
            channel.History2 = reader.ReadInt16();
            channel.LoopPredictorScale = reader.ReadUInt16();
            channel.LoopHistory1 = reader.ReadInt16();
            channel.LoopHistory2 = reader.ReadInt16();
            return channel;
        }

        /// <param name="value">The channel to write.</param>
        /// <param name="writer">A <see cref="Endianness.Big"/> writer - see <see cref="Read"/>.</param>
        internal static void Write(DspChannel value, EndianWriter writer)
        {
            foreach (short coefficient in value.Coefficients) writer.Write(coefficient);
            writer.Write(value.Gain);
            writer.Write(value.PredictorScale);
            writer.Write(value.History1);
            writer.Write(value.History2);
            writer.Write(value.LoopPredictorScale);
            writer.Write(value.LoopHistory1);
            writer.Write(value.LoopHistory2);
        }
    }
}
