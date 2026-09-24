using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

public partial class SoundInfoAsset
{
    /// <summary>
    /// A Sony VAG sound header, describing one sound's sample rate and size. Stored on
    /// <see cref="Platform.PlayStation2"/>.
    /// </summary>
    /// <remarks>
    /// In <see cref="GameVersion.N100F"/>, <see cref="Version"/>, <see cref="DataSize"/>, and
    /// <see cref="SoundHeader.SampleRate"/> are stored big-endian while <see cref="SoundHeader.SoundAssetId"/>
    /// is little-endian; every other game stores the whole header little-endian. Two
    /// <see cref="GameVersion.N100F"/> entries (in <c>I001.HIP</c> and <c>S005.HIP</c>) hold a RIFF WAVE
    /// header instead - these still round-trip, but their fields don't carry the meanings documented
    /// here.
    /// </remarks>
    public sealed class VagHeader : SoundHeader
    {
        /// <summary>A four-character magic number.</summary>
        public uint Magic { get; set; } = 0x70474156; // "VAGp"

        /// <summary>The VAG format version.</summary>
        public uint Version { get; set; }

        /// <summary>The size of the sound's audio data, in bytes.</summary>
        /// <remarks>
        /// In <see cref="GameVersion.N100F"/>, the <see cref="AssetType.Sound"/>/<see cref="AssetType.StreamingSound"/>
        /// asset also carries a copy of this 0x30-byte header ahead of its audio data, so it is 0x30 bytes
        /// larger than this value.
        /// </remarks>
        public uint DataSize { get; set; }

        /// <summary>Unknown. Always zero outside <see cref="GameVersion.N100F"/>.</summary>
        public Collection<byte> Unknown { get; } = new([.. new byte[12]]);

        /// <summary>
        /// The sound's original file name, as null-terminated ASCII. Unused by the game; bytes after the
        /// terminator are leftover data from the export tool.
        /// </summary>
        public Collection<byte> Name { get; } = new([.. new byte[16]]);

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "May be the caller's own reader - see FieldReader.")]
        internal static new VagHeader Read(EndianReader reader, FormatProfile profile)
        {
            var numbers = FieldReader(reader, profile);
            var header = new VagHeader
            {
                Magic = reader.ReadUInt32(),
                Version = numbers.ReadUInt32(),
                SoundAssetId = reader.ReadAssetId(),
                DataSize = numbers.ReadUInt32(),
                SampleRate = numbers.ReadUInt32(),
            };
            for (int i = 0; i < header.Unknown.Count; i++) header.Unknown[i] = reader.ReadByte();
            for (int i = 0; i < header.Name.Count; i++) header.Name[i] = reader.ReadByte();
            return header;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "May be the caller's own writer - see FieldWriter.")]
        internal static void Write(VagHeader value, EndianWriter writer, FormatProfile profile)
        {
            var numbers = FieldWriter(writer, profile);
            writer.Write(value.Magic);
            numbers.Write(value.Version);
            writer.Write(value.SoundAssetId);
            numbers.Write(value.DataSize);
            numbers.Write(value.SampleRate);
            foreach (byte b in value.Unknown) writer.Write(b);
            foreach (byte b in value.Name) writer.Write(b);
        }

        /// <summary>
        /// Wraps <paramref name="reader"/> to force <see cref="Endianness.Big"/> under
        /// <see cref="GameVersion.N100F"/>, for the fields it stores big-endian.
        /// </summary>
        /// <remarks>
        /// Never disposed: it shares <paramref name="reader"/>'s underlying stream with
        /// <c>leaveOpen: true</c> and owns nothing else.
        /// </remarks>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Thin, non-owning wrapper over the caller's own stream - see remarks.")]
        private static EndianReader FieldReader(EndianReader reader, FormatProfile profile) =>
            profile.Game == GameVersion.N100F && reader.Endianness != Endianness.Big
                ? new EndianReader(reader.BaseStream, Endianness.Big, leaveOpen: true)
                : reader;

        /// <summary>See <see cref="FieldReader"/>.</summary>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Thin, non-owning wrapper over the caller's own stream - see FieldReader.")]
        private static EndianWriter FieldWriter(EndianWriter writer, FormatProfile profile) =>
            profile.Game == GameVersion.N100F && writer.Endianness != Endianness.Big
                ? new EndianWriter(writer.BaseStream, Endianness.Big, leaveOpen: true)
                : writer;
    }
}
