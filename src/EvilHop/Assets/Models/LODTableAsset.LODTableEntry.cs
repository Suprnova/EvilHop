using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Assets;

public partial class LODTableAsset
{
    /// <summary>
    /// One <see cref="LODTableAsset"/> entry: a base model plus up to three lower-detail replacements
    /// for it, each swapped in once the camera passes its associated distance.
    /// </summary>
    public sealed class LODTableEntry
    {
        /// <summary>
        /// The <see cref="AssetType.Model"/> used while nearer than <see cref="Lod1Distance"/>, or for
        /// the entry's entire range if no LOD levels are set.
        /// </summary>
        public AssetId BaseModelId { get; set; }

        /// <summary>
        /// The distance from the camera beyond which the model fades out of view entirely.
        /// </summary>
        public float NoRenderDistance { get; set; }

        /// <summary>
        /// Unknown. Not present in <see cref="GameVersion.BFBB"/>.
        /// </summary>
        public uint Flags { get; set; }

        /// <summary>
        /// The <see cref="AssetType.Model"/> used once farther than <see cref="Lod1Distance"/>, if any.
        /// </summary>
        public AssetId Lod1ModelId { get; set; }

        /// <summary>
        /// The <see cref="AssetType.Model"/> used once farther than <see cref="Lod2Distance"/>, if any.
        /// </summary>
        public AssetId Lod2ModelId { get; set; }

        /// <summary>
        /// The <see cref="AssetType.Model"/> used once farther than <see cref="Lod3Distance"/>, if any.
        /// </summary>
        public AssetId Lod3ModelId { get; set; }

        /// <summary>
        /// The distance from the camera beyond which <see cref="Lod1ModelId"/> replaces
        /// <see cref="BaseModelId"/>.
        /// </summary>
        public float Lod1Distance { get; set; }

        /// <summary>
        /// The distance from the camera beyond which <see cref="Lod2ModelId"/> replaces
        /// <see cref="Lod1ModelId"/>.
        /// </summary>
        public float Lod2Distance { get; set; }

        /// <summary>
        /// The distance from the camera beyond which <see cref="Lod3ModelId"/> replaces
        /// <see cref="Lod2ModelId"/>.
        /// </summary>
        public float Lod3Distance { get; set; }

        internal static LODTableEntry Read(EndianReader reader, FormatProfile profile) => new()
        {
            BaseModelId = reader.ReadAssetId(),
            NoRenderDistance = reader.ReadSingle(),
            Flags = profile.Game is not GameVersion.BFBB ? reader.ReadUInt32() : 0,
            Lod1ModelId = reader.ReadAssetId(),
            Lod2ModelId = reader.ReadAssetId(),
            Lod3ModelId = reader.ReadAssetId(),
            Lod1Distance = reader.ReadSingle(),
            Lod2Distance = reader.ReadSingle(),
            Lod3Distance = reader.ReadSingle(),
        };

        internal static void Write(LODTableEntry value, EndianWriter writer, FormatProfile profile)
        {
            writer.Write(value.BaseModelId);
            writer.Write(value.NoRenderDistance);
            if (profile.Game is not GameVersion.BFBB) writer.Write(value.Flags);
            writer.Write(value.Lod1ModelId);
            writer.Write(value.Lod2ModelId);
            writer.Write(value.Lod3ModelId);
            writer.Write(value.Lod1Distance);
            writer.Write(value.Lod2Distance);
            writer.Write(value.Lod3Distance);
        }
    }
}
