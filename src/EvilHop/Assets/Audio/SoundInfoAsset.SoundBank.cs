using EvilHop.Primitives;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

public partial class SoundInfoAsset
{
    /// <summary>
    /// An FMOD 3 "FSB3" sample bank, holding one or more <see cref="Sample"/>s.
    /// </summary>
    /// <remarks>
    /// A bank is self-contained: moving a <see cref="Sample"/> between banks, or into a new bank of its
    /// own, carries everything FMOD needs to play it. Use <see cref="Load"/> and <see cref="SaveTo"/> to
    /// exchange banks with standalone <c>.fsb</c> files.
    /// </remarks>
    public sealed class SoundBank
    {
        /// <summary>The bank format version. Every shipped bank is <c>0x30001</c>.</summary>
        public uint Version { get; set; } = 0x30001;

        /// <summary>How the bank stores its sample headers.</summary>
        public BankMode Mode { get; set; }

        /// <summary>The bank's samples, in the order <see cref="Sound.SampleIndex"/> counts them.</summary>
        public Collection<Sample> Samples { get; } = [];

        /// <summary>
        /// The bytes that follow the bank inside a <see cref="SoundInfoAsset"/>, filling the gap up to
        /// the next 32-byte boundary.
        /// </summary>
        /// <remarks>
        /// Shipped archives often fill the gap with leftover data rather than zeros, so it's kept to
        /// reproduce them byte-for-byte. The gap's length always follows from the bank's size; these
        /// bytes are written truncated or zero-extended to fit it. Not part of a standalone
        /// <c>.fsb</c> file.
        /// </remarks>
        public ImmutableArray<byte> Padding { get; set; } = [];

        private const uint Magic = 0x33425346; // "FSB3"

        /// <summary>Reads a standalone FSB3 file from <paramref name="stream"/>.</summary>
        /// <param name="stream">The stream to read from, positioned at the start of the bank.</param>
        /// <returns>The bank read from <paramref name="stream"/>.</returns>
        /// <exception cref="InvalidDataException"><paramref name="stream"/> doesn't hold an FSB3 bank.</exception>
        public static SoundBank Load(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            using var reader = new EndianReader(stream, Endianness.Little, leaveOpen: true);
            return Read(reader);
        }

        /// <summary>Reads a standalone FSB3 file from <paramref name="path"/>.</summary>
        /// <param name="path">The path to read the bank from.</param>
        /// <returns>The bank read from <paramref name="path"/>.</returns>
        /// <exception cref="InvalidDataException">The file doesn't hold an FSB3 bank.</exception>
        public static SoundBank LoadFromFile(string path)
        {
            using var file = File.OpenRead(path);
            return Load(file);
        }

        /// <summary>Writes this bank to <paramref name="stream"/> as a standalone FSB3 file.</summary>
        /// <param name="stream">The stream to write to.</param>
        public void SaveTo(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            using var writer = new EndianWriter(stream, Endianness.Little, leaveOpen: true);
            Write(this, writer);
        }

        /// <summary>Writes this bank to <paramref name="path"/> as a standalone FSB3 file.</summary>
        /// <param name="path">The path to write the bank to.</param>
        public void SaveToFile(string path)
        {
            using var file = File.Create(path);
            SaveTo(file);
        }

        /// <param name="reader">A <see cref="Endianness.Little"/> reader positioned at the bank's magic.</param>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Thin, non-owning wrapper over the caller's own stream.")]
        internal static SoundBank Read(EndianReader reader)
        {
            if (reader.ReadUInt32() != Magic) throw new InvalidDataException("Not an FSB3 sound bank.");
            var bigEndian = new EndianReader(reader.BaseStream, Endianness.Big, leaveOpen: true);

            int sampleCount = reader.ReadInt32();
            reader.ReadInt32(); // total sample header size, derived from Samples on write
            reader.ReadInt32(); // total data size, ditto
            var bank = new SoundBank { Version = reader.ReadUInt32(), Mode = (BankMode)reader.ReadUInt32() };

            var dataSizes = new int[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                bank.Samples.Add(i > 0 && bank.Mode.HasFlag(BankMode.BasicHeaders)
                    ? Sample.ReadBasicHeader(reader, bigEndian, bank.Samples[0], out dataSizes[i])
                    : Sample.ReadFullHeader(reader, bigEndian, out dataSizes[i]));
            }

            for (int i = 0; i < sampleCount; i++) bank.Samples[i].Data = [.. reader.ReadBytes(dataSizes[i])];

            return bank;
        }

        /// <param name="value">The bank to write.</param>
        /// <param name="writer">A <see cref="Endianness.Little"/> writer.</param>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Thin, non-owning wrapper over the caller's own stream.")]
        internal static void Write(SoundBank value, EndianWriter writer)
        {
            var bigEndian = new EndianWriter(writer.BaseStream, Endianness.Big, leaveOpen: true);
            bool basic = value.Mode.HasFlag(BankMode.BasicHeaders);

            writer.Write(Magic);
            writer.Write(value.Samples.Count);
            writer.Write(value.Samples.Select((sample, i) => IsBasic(i) ? Sample.BasicHeaderLength(sample) : Sample.FullHeaderLength(sample)).Sum());
            writer.Write(value.Samples.Sum(sample => sample.Data.Length));
            writer.Write(value.Version);
            writer.Write((uint)value.Mode);

            for (int i = 0; i < value.Samples.Count; i++)
            {
                if (IsBasic(i)) Sample.WriteBasicHeader(value.Samples[i], writer, bigEndian);
                else Sample.WriteFullHeader(value.Samples[i], writer, bigEndian);
            }

            foreach (var sample in value.Samples) writer.Write(sample.Data.AsSpan());

            bool IsBasic(int index) => basic && index > 0;
        }

        /// <summary>Writes this bank followed by its <see cref="Padding"/>, sized to end on a 32-byte boundary.</summary>
        internal static byte[] WritePadded(SoundBank value)
        {
            using var stream = new MemoryStream();
            using (var writer = new EndianWriter(stream, Endianness.Little, leaveOpen: true))
                Write(value, writer);

            int gap = (32 - (int)(stream.Length % 32)) % 32;
            var padding = value.Padding.AsSpan();
            stream.Write(padding[..Math.Min(gap, padding.Length)]);
            stream.Write(new byte[Math.Max(0, gap - padding.Length)]);
            return stream.ToArray();
        }
    }

    /// <summary>How a <see cref="SoundBank"/> stores its sample headers. Unnamed bits round-trip untouched.</summary>
    [Flags]
    public enum BankMode : uint
    {
        /// <summary>Every sample has a full header.</summary>
        None = 0,

        /// <summary>
        /// Only the first sample has a full header. Every later sample stores just its
        /// <see cref="Sample.SampleCount"/>, data size, and <see cref="Sample.DspChannels"/>, and
        /// inherits every other field from the first sample - its <see cref="Sample.Name"/> is empty and
        /// its <see cref="Sample.LoopEnd"/> is its last sample. Fields of later samples that differ from
        /// the first sample's are not written.
        /// </summary>
        BasicHeaders = 1u << 1,
    }
}
