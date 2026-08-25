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
        get => _overriddenBaseId ?? this.Id;
        // Assigning a value equal to Id clears the override rather than pinning one. A codec reads
        // this straight from disk without checking, and the asset still follows later changes to
        // Id unless the file itself disagreed - which is the only case worth preserving.
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
        // Unlike BaseId and Type, this cannot self-normalize: a codec reads the count from the
        // fixed header before it has located the links themselves, so comparing against
        // Links.Count at that moment would always see 0. The rule is instead a codec-side one - a
        // codec that parses links into Links leaves this alone and lets it derive; one that cannot
        // locate them sets it, and Links stays empty. See the interface's own remarks.
        set => _overriddenLinkCount = value;
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
    /// Follows <see cref="Asset.Id"/> unless the two disagreed on disk. Setting this to a value
    /// equal to <see cref="Asset.Id"/> restores that behaviour; setting it to anything else pins
    /// the disagreement and preserves it through a round trip.
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
    /// Derives from <see cref="BaseAsset.Links"/>'s count until explicitly set, after which it
    /// keeps whatever it was given. A codec that parses links into <see cref="BaseAsset.Links"/>
    /// must leave this alone; a codec that cannot locate them sets it here and leaves
    /// <see cref="BaseAsset.Links"/> empty, so the two disagreeing is the signal that links exist
    /// on disk but nothing has parsed them.
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
