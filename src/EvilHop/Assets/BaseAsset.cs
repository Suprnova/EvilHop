using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="Asset"/> that represents an object in the level, capable of interacting with
/// others via <see cref="Link"/> objects.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Assets#Base_Assets">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class BaseAsset : Asset, IPhysicalBaseAsset
{
    /// <summary>
    /// The <see cref="BaseAsset"/>'s <see cref="BaseAssetFlags"/>.
    /// </summary>
    public BaseAssetFlags BaseFlags { get; set; }
    /// <summary>
    /// The <see cref="Link"/>s this <see cref="BaseAsset"/> owns.
    /// </summary>
    public List<Link> Links { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalBaseAsset Physical => this;

    private AssetId? _overriddenBaseId;
    AssetId IPhysicalBaseAsset.BaseId
    {
        get => _overriddenBaseId ?? Id;
        // prevents equivalent id assignments from being interpretted as an "override"
        set => _overriddenBaseId = value == Id ? null : value;
    }

    private protected byte _baseType;
    byte IPhysicalBaseAsset.BaseType
    {
        get => _baseType;
        set => _baseType = value;
    }

    private byte? _overriddenLinkCount;
    byte IPhysicalBaseAsset.LinkCount
    {
        get => _overriddenLinkCount ?? (byte)Links.Count;
        set => _overriddenLinkCount = value == (byte)Links.Count ? null : value;
    }
}

/// <summary>
/// An explicit interface used to interact with <see cref="BaseAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalBaseAsset : IPhysicalAsset
{
    /// <summary>
    /// The <see cref="Asset"/>'s ID, as stored in the <see cref="BaseAsset"/> header.
    /// This field is stored independently from <see cref="Asset.Id"/>, within the
    /// asset's own data.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="Asset.Id"/> exist, this field wins during serialization.
    /// </remarks>
    AssetId BaseId { get; set; }
    /// <summary>
    /// The <see cref="BaseAsset"/>'s base type.
    /// </summary>
    byte BaseType { get; set; }
    /// <summary>
    /// The number of links stored for this <see cref="BaseAsset"/>, read directly from its fixed
    /// header.
    /// </summary>
    /// <remarks>
    /// When disagreements with <see cref="BaseAsset.Links"/>.Count exist, this field wins during
    /// serialization. 
    /// </remarks>
    byte LinkCount { get; set; }
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
