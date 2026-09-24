using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class SoundInfoAsset
{
    /// <summary>
    /// A Windows <c>WAVEFORMATEX</c> sound header, describing one sound's encoding, sample rate, and
    /// size. Stored on <see cref="Platform.Xbox"/>, whose sounds are either raw 16-bit PCM or Xbox
    /// ADPCM.
    /// </summary>
    /// <remarks>
    /// <seealso href="https://xboxdevwiki.net/Xbox_ADPCM">Xbox ADPCM</seealso>
    /// </remarks>
    public sealed class WaveHeader : SoundHeader
    {
        /// <summary>The sound's encoding.</summary>
        public WaveFormat Format { get; set; }

        /// <summary>The number of audio channels: 1 for mono, 2 for stereo.</summary>
        public ushort ChannelCount { get; set; }

        /// <summary>The average data rate, in bytes per second.</summary>
        public uint AverageBytesPerSecond { get; set; }

        /// <summary>
        /// The size of one block of audio data, in bytes: 2 per channel for <see cref="WaveFormat.Pcm"/>,
        /// 36 per channel for <see cref="WaveFormat.XboxAdpcm"/>.
        /// </summary>
        public ushort BlockAlign { get; set; }

        /// <summary>
        /// The number of bits per sample: 16 for <see cref="WaveFormat.Pcm"/>, 4 for
        /// <see cref="WaveFormat.XboxAdpcm"/>.
        /// </summary>
        public ushort BitsPerSample { get; set; }

        /// <summary>
        /// The number of format-specific bytes following the standard <c>WAVEFORMATEX</c> fields: 0 for
        /// <see cref="WaveFormat.Pcm"/>, 2 for <see cref="WaveFormat.XboxAdpcm"/> (covering
        /// <see cref="SamplesPerBlock"/>).
        /// </summary>
        public ushort ExtraSize { get; set; }

        /// <summary>
        /// The number of samples in each ADPCM block: 64 for <see cref="WaveFormat.XboxAdpcm"/>, 0 for
        /// <see cref="WaveFormat.Pcm"/>.
        /// </summary>
        public ushort SamplesPerBlock { get; set; }

        /// <summary>The size of the sound's audio data, in bytes.</summary>
        public uint DataSize { get; set; }

        /// <summary>How the sound plays back.</summary>
        public PlaybackMode Playback { get; set; }

        internal static new WaveHeader Read(EndianReader reader, FormatProfile _)
        {
            var header = new WaveHeader
            {
                Format = (WaveFormat)reader.ReadUInt16(),
                ChannelCount = reader.ReadUInt16(),
                SampleRate = reader.ReadUInt32(),
                AverageBytesPerSecond = reader.ReadUInt32(),
                BlockAlign = reader.ReadUInt16(),
                BitsPerSample = reader.ReadUInt16(),
                ExtraSize = reader.ReadUInt16(),
                SamplesPerBlock = reader.ReadUInt16(),
                DataSize = reader.ReadUInt32(),
                SoundAssetId = reader.ReadAssetId(),
                Playback = (PlaybackMode)reader.ReadUInt32(),
            };
            reader.ReadBytes(12); // 12 bytes of padding, always zero
            return header;
        }

        internal static void Write(WaveHeader value, EndianWriter writer, FormatProfile _)
        {
            writer.Write((ushort)value.Format);
            writer.Write(value.ChannelCount);
            writer.Write(value.SampleRate);
            writer.Write(value.AverageBytesPerSecond);
            writer.Write(value.BlockAlign);
            writer.Write(value.BitsPerSample);
            writer.Write(value.ExtraSize);
            writer.Write(value.SamplesPerBlock);
            writer.Write(value.DataSize);
            writer.Write(value.SoundAssetId);
            writer.Write((uint)value.Playback);
            writer.Write(new byte[12]); // padding
        }

        /// <summary>A <see cref="WaveHeader"/>'s audio encoding.</summary>
        public enum WaveFormat : ushort
        {
            /// <summary>Raw 16-bit linear PCM.</summary>
            Pcm = 0x01,

            /// <summary>Xbox ADPCM, a 4-bit IMA ADPCM variant.</summary>
            XboxAdpcm = 0x69,
        }

        /// <summary>How a <see cref="WaveHeader"/>'s sound plays back.</summary>
        public enum PlaybackMode : uint
        {
            /// <summary>The sound plays once.</summary>
            Once = 0,

            /// <summary>The sound loops.</summary>
            Looped = 1,

            /// <summary>
            /// Unknown. Set on every <see cref="Streams"/> entry in <see cref="GameVersion.Incredibles"/> and
            /// <see cref="GameVersion.ROTU"/>, and never elsewhere.
            /// </summary>
            Unknown2 = 2,
        }
    }
}
