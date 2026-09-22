using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class PipeInfoTableAsset
{
    /// <summary>
    /// One <see cref="PipeInfoTableAsset"/> entry, applying rendering information to a <see
    /// cref="AssetType.Model"/> asset or a subset of its atomics.
    /// </summary>
    public sealed class Entry
    {
        /// <summary>
        /// The <see cref="AssetType.Model"/> asset this entry applies rendering information to.
        /// </summary>
        public AssetId ModelId { get; set; }

        /// <summary>
        /// A bitmask selecting which of <see cref="ModelId"/>'s RenderWare atomics this entry applies to,
        /// one bit per atomic starting with the model's last atomic as the least significant bit (0x1)
        /// and ending with its first atomic as the most significant bit. 0xFFFFFFFF applies the entry to
        /// every atomic.
        /// </summary>
        public uint SubObjectBits { get; set; }

        /// <summary>
        /// Rendering flags applied to the selected atomics - alpha compare, fog, blending, lighting,
        /// culling, and depth-testing.
        /// </summary>
        public RenderBehavior Flags { get; set; }

        /// <summary>
        /// When to draw the selected atomics relative to other transparent geometry. Not present in
        /// <see cref="GameVersion.BFBB"/>.
        /// </summary>
        public RenderingLayer Layer { get; set; }

        /// <summary>
        /// Unknown. Not present in <see cref="GameVersion.BFBB"/>.
        /// </summary>
        public byte AlphaDiscard { get; set; }

        internal static Entry Read(EndianReader reader, FormatProfile profile)
        {
            var entry = new Entry
            {
                ModelId = reader.ReadAssetId(),
                SubObjectBits = reader.ReadUInt32(),
                Flags = new RenderBehavior(reader.ReadUInt32()),
            };

            if (profile.Game is not GameVersion.BFBB)
            {
                entry.Layer = (RenderingLayer)reader.ReadByte();
                entry.AlphaDiscard = reader.ReadByte();
                reader.ReadInt16(); // padding, always zero
            }

            return entry;
        }

        internal static void Write(Entry value, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(value.ModelId);
            writer.Write(value.SubObjectBits);
            writer.Write(value.Flags.Value);

            if (profile.Game is not GameVersion.BFBB)
            {
                writer.Write((byte)value.Layer);
                writer.Write(value.AlphaDiscard);
                writer.Write((short)0); // padding
            }
        }
    }
}
