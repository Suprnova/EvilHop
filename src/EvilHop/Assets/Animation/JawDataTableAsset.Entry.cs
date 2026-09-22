using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Buffers.Binary;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

public partial class JawDataTableAsset
{
    /// <summary>
    /// One <see cref="JawDataTableAsset"/> entry: a sound and the mouth open/close positions to animate
    /// while it plays.
    /// </summary>
    public sealed class Entry
    {
        /// <summary>
        /// The <see cref="AssetType.Sound"/> or <see cref="AssetType.StreamingSound"/> this jaw data
        /// animates alongside.
        /// </summary>
        public AssetId SoundId { get; set; }

        /// <summary>
        /// The speaker's mouth position at each point in time <see cref="SoundId"/> plays, one byte per
        /// frame from 0 (fully closed) to 255 (fully open).
        /// </summary>
        public Collection<byte> JawData { get; } = [];

        /// <summary>
        /// Unknown. Only present in <see cref="GameVersion.ROTU"/>.
        /// </summary>
        public uint Unknown { get; set; }

        /// <remarks>
        /// The owning <see cref="JawDataTableAsset"/> reads <paramref name="soundId"/> from its leading
        /// table-of-contents pass; this reads the rest of the entry from the payload region that follows
        /// every entry's table-of-contents record.
        /// </remarks>
        internal static Entry Read(EndianReader reader, AssetId soundId, FormatProfile profile)
        {
            bool hasUnknownField = profile.Game is GameVersion.ROTU;
            int length = hasUnknownField ? reader.ReadInt32() : BinaryPrimitives.ReadInt32LittleEndian(reader.ReadBytes(4));
            uint unknown = hasUnknownField ? reader.ReadUInt32() : 0;
            byte[] jawData = reader.ReadBytes(length);

            int padding = Align4(length) - length;
            if (padding > 0) reader.ReadBytes(padding); // 4-byte alignment padding, always zero

            var entry = new Entry { SoundId = soundId, Unknown = unknown };
            foreach (byte b in jawData) entry.JawData.Add(b);
            return entry;
        }

        internal static void Write(Entry value, EndianWriter writer, FormatProfile profile)
        {
            if (profile.Game is GameVersion.ROTU)
            {
                writer.Write(value.JawData.Count);
                writer.Write(value.Unknown);
            }
            else
            {
                Span<byte> writeBuffer = stackalloc byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(writeBuffer, value.JawData.Count);
                writer.Write(writeBuffer);
            }
            writer.Write(value.JawData.ToArray());

            int padding = Align4(value.JawData.Count) - value.JawData.Count;
            for (int i = 0; i < padding; i++) writer.Write((byte)0);
        }

        private static int Align4(int value) => (value + 3) & ~3;
    }
}
