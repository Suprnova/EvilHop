using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="Asset"/> that represents an object in the level, capable of interacting with
/// others via <see cref="Link"/> objects.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Assets#Base_Assets">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class BaseAsset : Asset
{
    /// <summary>
    /// The <see cref="Asset"/>'s ID, as stored in the <see cref="BaseAsset"/> header.
    /// This field is stored independently from <see cref="Asset.Id"/>, stored independently
    /// within the asset's own data.
    /// </summary>
    public AssetId BaseId { get; set; }
    /// <summary>
    /// The <see cref="BaseAsset"/>'s base type.
    /// </summary>
    public byte BaseType { get; internal set; }
    /// <summary>
    /// The <see cref="BaseAsset"/>'s <see cref="BaseAssetFlags"/>.
    /// </summary>
    public BaseAssetFlags BaseFlags { get; set; }

    /// <summary>
    /// The number of links stored for this <see cref="BaseAsset"/>, read directly from its fixed
    /// header.
    /// </summary>
    public byte LinkCount { get; set; }
    /// <summary>
    /// The <see cref="Assets.Link"/>s this <see cref="BaseAsset"/> owns.
    /// </summary>
    public List<Link> Links { get; } = [];
}

/// <summary>
/// Represents all known values for <see cref="BaseAsset.BaseFlags"/>.
/// </summary>
[Flags]
public enum BaseAssetFlags : short
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The <see cref="BaseAsset"/> is enabled.
    /// </summary>
    Enabled = 1 << 0,
    /// <summary>
    /// The <see cref="BaseAsset"/>'s state persists across level reloads.
    /// </summary>
    Persistent = 1 << 1,
    /// <summary>
    /// Always set. Meaning otherwise undocumented.
    /// </summary>
    Valid = 1 << 2,
    /// <summary>
    /// The <see cref="BaseAsset"/> remains visible during cutscenes.
    /// </summary>
    VisibleDuringCutscenes = 1 << 3,
    /// <summary>
    /// The <see cref="BaseAsset"/> receives shadows.
    /// </summary>
    ReceiveShadows = 1 << 4,
}
