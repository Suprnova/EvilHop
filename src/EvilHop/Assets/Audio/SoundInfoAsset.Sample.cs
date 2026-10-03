using EvilHop.Common;
using EvilHop.Primitives;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

public partial class SoundInfoAsset
{
    /// <summary>
    /// One sound stored in a <see cref="SoundBank"/>: its FMOD sample header and its encoded audio.
    /// </summary>
    /// <remarks>
    /// <see cref="Sound.SampleIndex"/> selects a sample by its position in <see cref="SoundBank.Samples"/>.
    /// </remarks>
    public sealed class Sample
    {
        /// <summary>
        /// The sample's name, at most 30 ASCII characters. Unused by the game, and not stored for
        /// samples written as basic headers - see <see cref="BankMode.BasicHeaders"/>.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The number of decoded samples in the sound.</summary>
        public uint SampleCount { get; set; }

        /// <summary>The loop start position, in decoded samples.</summary>
        public uint LoopStart { get; set; }

        /// <summary>The loop end position, in decoded samples, inclusive.</summary>
        public uint LoopEnd { get; set; }

        /// <summary>The sample's FMOD mode flags: encoding, channel layout, and FMOD-side looping.</summary>
        /// <remarks>
        /// The game loops a sound based on its <see cref="Sound.Flags"/>, not
        /// <see cref="SampleMode.LoopOff"/> - shipped looping sounds commonly keep
        /// <see cref="SampleMode.LoopOff"/> set.
        /// </remarks>
        public SampleMode Mode { get; set; } = SampleMode.LoopOff | SampleMode.Mono | SampleMode.Hardware3D | SampleMode.GcAdpcm;

        /// <summary>The playback rate, in Hz.</summary>
        public uint SampleRate { get; set; } = 32000;

        /// <summary>The default playback volume, from 0 to 255.</summary>
        public ushort Volume { get; set; } = 255;

        /// <summary>The default pan, from 0 (left) to 255 (right); 128 is centered.</summary>
        public short Pan { get; set; } = 128;

        /// <summary>The default playback priority, from 0 to 255.</summary>
        public ushort Priority { get; set; } = 255;

        /// <summary>The number of audio channels.</summary>
        public ushort ChannelCount { get; set; } = 1;

        /// <summary>The distance at which 3D playback starts attenuating.</summary>
        public float MinDistance { get; set; } = 1f;

        /// <summary>The distance beyond which 3D playback stops attenuating.</summary>
        public float MaxDistance { get; set; } = 1_000_000f;

        /// <summary>The random variation applied to <see cref="SampleRate"/> on playback, in Hz.</summary>
        public int SampleRateVariation { get; set; }

        /// <summary>The random variation applied to <see cref="Volume"/> on playback.</summary>
        public ushort VolumeVariation { get; set; }

        /// <summary>The random variation applied to <see cref="Pan"/> on playback.</summary>
        public short PanVariation { get; set; }

        /// <summary>
        /// The DSP-ADPCM decoder state for each channel, one per <see cref="ChannelCount"/> when
        /// <see cref="Mode"/> includes <see cref="SampleMode.GcAdpcm"/>, and empty otherwise.
        /// </summary>
        public Collection<DspChannel> DspChannels { get; } = [];

        /// <summary>The sample's encoded audio, interleaved by channel.</summary>
        public ImmutableArray<byte> Data { get; set; } = [];

        /// <summary>The size of a full sample header on disk, excluding its <see cref="DspChannels"/>.</summary>
        private const int FullHeaderSize = 0x50;

        private const int NameLength = 30;

        internal static Sample ReadFullHeader(EndianReader reader, EndianReader bigEndian, out int dataSize)
        {
            int headerSize = reader.ReadUInt16();
            var sample = new Sample
            {
                Name = ICutsceneHeader.ReadFixedString(reader, NameLength),
                SampleCount = reader.ReadUInt32(),
            };
            dataSize = reader.ReadInt32();
            sample.LoopStart = reader.ReadUInt32();
            sample.LoopEnd = reader.ReadUInt32();
            sample.Mode = (SampleMode)reader.ReadUInt32();
            sample.SampleRate = reader.ReadUInt32();
            sample.Volume = reader.ReadUInt16();
            sample.Pan = reader.ReadInt16();
            sample.Priority = reader.ReadUInt16();
            sample.ChannelCount = reader.ReadUInt16();
            sample.MinDistance = reader.ReadSingle();
            sample.MaxDistance = reader.ReadSingle();
            sample.SampleRateVariation = reader.ReadInt32();
            sample.VolumeVariation = reader.ReadUInt16();
            sample.PanVariation = reader.ReadInt16();

            int dspSize = headerSize - FullHeaderSize;
            if (dspSize < 0 || dspSize % DspChannel.Size != 0)
                throw new InvalidDataException($"Sample header size {headerSize} isn't {FullHeaderSize} plus whole DSP channels.");
            for (int i = 0; i < dspSize / DspChannel.Size; i++) sample.DspChannels.Add(DspChannel.Read(bigEndian));

            return sample;
        }

        /// <summary>
        /// Reads a basic header: <paramref name="first"/>'s fields with this sample's own lengths and
        /// DSP state, as FMOD itself expands it.
        /// </summary>
        internal static Sample ReadBasicHeader(EndianReader reader, EndianReader bigEndian, Sample first, out int dataSize)
        {
            var sample = new Sample
            {
                SampleCount = reader.ReadUInt32(),
                LoopStart = first.LoopStart,
                Mode = first.Mode,
                SampleRate = first.SampleRate,
                Volume = first.Volume,
                Pan = first.Pan,
                Priority = first.Priority,
                ChannelCount = first.ChannelCount,
                MinDistance = first.MinDistance,
                MaxDistance = first.MaxDistance,
                SampleRateVariation = first.SampleRateVariation,
                VolumeVariation = first.VolumeVariation,
                PanVariation = first.PanVariation,
            };
            dataSize = reader.ReadInt32();
            sample.LoopEnd = sample.SampleCount - 1;
            for (int i = 0; i < first.DspChannels.Count; i++) sample.DspChannels.Add(DspChannel.Read(bigEndian));
            return sample;
        }

        internal static void WriteFullHeader(Sample value, EndianWriter writer, EndianWriter bigEndian)
        {
            writer.Write((ushort)(FullHeaderSize + value.DspChannels.Count * DspChannel.Size));
            ICutsceneHeader.WriteFixedString(writer, value.Name, NameLength);
            writer.Write(value.SampleCount);
            writer.Write(value.Data.Length);
            writer.Write(value.LoopStart);
            writer.Write(value.LoopEnd);
            writer.Write((uint)value.Mode);
            writer.Write(value.SampleRate);
            writer.Write(value.Volume);
            writer.Write(value.Pan);
            writer.Write(value.Priority);
            writer.Write(value.ChannelCount);
            writer.Write(value.MinDistance);
            writer.Write(value.MaxDistance);
            writer.Write(value.SampleRateVariation);
            writer.Write(value.VolumeVariation);
            writer.Write(value.PanVariation);
            foreach (var channel in value.DspChannels) DspChannel.Write(channel, bigEndian);
        }

        internal static void WriteBasicHeader(Sample value, EndianWriter writer, EndianWriter bigEndian)
        {
            writer.Write(value.SampleCount);
            writer.Write(value.Data.Length);
            foreach (var channel in value.DspChannels) DspChannel.Write(channel, bigEndian);
        }

        internal static int FullHeaderLength(Sample value) => FullHeaderSize + value.DspChannels.Count * DspChannel.Size;

        internal static int BasicHeaderLength(Sample value) => 8 + value.DspChannels.Count * DspChannel.Size;
    }

    /// <summary>FMOD 3 mode flags for one <see cref="Sample"/>. Unnamed bits round-trip untouched.</summary>
    [Flags]
    public enum SampleMode : uint
    {
        /// <summary>No flags are set.</summary>
        None = 0,

        /// <summary>FMOD doesn't loop the sample.</summary>
        LoopOff = 1u << 0,

        /// <summary>FMOD loops the sample from <see cref="Sample.LoopEnd"/> back to <see cref="Sample.LoopStart"/>.</summary>
        LoopNormal = 1u << 1,

        /// <summary>FMOD loops the sample back and forth between its loop points.</summary>
        LoopBidirectional = 1u << 2,

        /// <summary>The sample has one channel.</summary>
        Mono = 1u << 5,

        /// <summary>The sample has two channels.</summary>
        Stereo = 1u << 6,

        /// <summary>The sample plays through a hardware 3D voice.</summary>
        Hardware3D = 1u << 12,

        /// <summary>The sample plays through a hardware 2D voice.</summary>
        Hardware2D = 1u << 19,

        /// <summary>
        /// The sample is Nintendo DSP-ADPCM encoded, with one <see cref="DspChannel"/> per channel.
        /// </summary>
        GcAdpcm = 1u << 25,
    }
}
