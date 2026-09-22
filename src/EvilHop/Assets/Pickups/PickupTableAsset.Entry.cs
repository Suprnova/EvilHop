using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class PickupTableAsset
{
    /// <summary>
    /// One <see cref="PickupTableAsset"/> entry, describing a single kind of pickup.
    /// </summary>
    public sealed class Entry
    {
        /// <summary>
        /// The hash <see cref="AssetType.Pickup"/> assets use to identify this pickup kind.
        /// </summary>
        public uint PickupHash { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public byte PickupType { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public byte PickupIndex { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        public ushort Flags { get; set; }

        /// <summary>
        /// How many of this pickup are granted at once.
        /// </summary>
        public uint Quantity { get; set; }

        /// <summary>
        /// The <see cref="AssetType.Model"/> or <see cref="AssetType.ModelInfo"/> this pickup displays as.
        /// </summary>
        public AssetId ModelId { get; set; }

        /// <summary>
        /// The <see cref="AssetType.Animation"/> or <see cref="AssetType.AnimationList"/> this pickup
        /// plays, if any.
        /// </summary>
        public AssetId AnimId { get; set; }

        internal static Entry Read(EndianReader reader, FormatProfile _) => new()
        {
            PickupHash = reader.ReadUInt32(),
            PickupType = reader.ReadByte(),
            PickupIndex = reader.ReadByte(),
            Flags = reader.ReadUInt16(),
            Quantity = reader.ReadUInt32(),
            ModelId = reader.ReadAssetId(),
            AnimId = reader.ReadAssetId(),
        };

        internal static void Write(Entry value, EndianWriter writer, FormatProfile _)
        {
            writer.Write(value.PickupHash);
            writer.Write(value.PickupType);
            writer.Write(value.PickupIndex);
            writer.Write(value.Flags);
            writer.Write(value.Quantity);
            writer.Write(value.ModelId);
            writer.Write(value.AnimId);
        }
    }
}
