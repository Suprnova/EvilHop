using EvilHop.Common;

namespace EvilHop.Assets;

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
}
