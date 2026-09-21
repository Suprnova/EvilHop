using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class ThrowableTableAsset
{
    /// <summary>
    /// One <see cref="ThrowableTableAsset"/> row, defining properties for a single throwable model.
    /// </summary>
    public sealed class ThrowableTableRow
    {
        /// <summary>
        /// The <see cref="AssetType.Model"/> used for the throwable object.
        /// </summary>
        public AssetId ModelId { get; set; }

        /// <summary>
        /// The throwable behavior type index.
        /// </summary>
        public uint Type { get; set; }

        /// <summary>
        /// The <see cref="AssetType.Shrapnel"/> spawned when the throwable object breaks.
        /// </summary>
        public AssetId ShrapnelId { get; set; }

        /// <summary>
        /// The amount of damage dealt on impact.
        /// </summary>
        public int Damage { get; set; }

        /// <summary>
        /// The blast radius of the damage on impact. Not present in version 2 assets.
        /// </summary>
        public float DamageRadius { get; set; }

        internal static ThrowableTableRow Read(EndianReader reader, FormatProfile _, int version)
        {
            var row = new ThrowableTableRow
            {
                ModelId = reader.ReadAssetId(),
                Type = reader.ReadUInt32(),
                ShrapnelId = reader.ReadAssetId(),
                Damage = reader.ReadInt32(),
            };
            if (version >= 3)
            {
                row.DamageRadius = reader.ReadSingle();
            }
            return row;
        }

        internal static void Write(ThrowableTableRow value, EndianWriter writer, FormatProfile _, int version)
        {
            writer.Write(value.ModelId);
            writer.Write(value.Type);
            writer.Write(value.ShrapnelId);
            writer.Write(value.Damage);
            if (version >= 3)
            {
                writer.Write(value.DamageRadius);
            }
        }
    }
}
