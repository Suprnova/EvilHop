using EvilHop.Common;
using System.Collections.ObjectModel;

namespace EvilHop.Assets;

/// <summary>
/// An <see cref="Asset"/> that represents an object in the level, capable of interacting with
/// others via <see cref="Link"/> objects.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Assets#Base_Assets">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class BaseAsset(AssetType type, byte baseType = 0) : Asset(type), Physical.IBaseAsset
{
    /// <summary>
    /// Whether the <see cref="BaseAsset"/> responds to events and fires its <see cref="Links"/>.
    /// </summary>
    /// <remarks>
    /// Projected from <see cref="BaseAssetFlags.Enabled"/> in <see cref="Physical.IBaseAsset.BaseFlags"/>.
    /// </remarks>
    public bool IsEnabled
    {
        get => Physical.BaseFlags.HasFlag(BaseAssetFlags.Enabled);
        set => Physical.BaseFlags = Physical.BaseFlags.WithFlag(BaseAssetFlags.Enabled, value);
    }

    /// <summary>
    /// Whether the <see cref="BaseAsset"/>'s state is saved with the scene and restored after a scene reset.
    /// </summary>
    /// <remarks>
    /// Projected from <see cref="BaseAssetFlags.Persistent"/> in <see cref="Physical.IBaseAsset.BaseFlags"/>.
    /// </remarks>
    public bool IsPersistent
    {
        get => Physical.BaseFlags.HasFlag(BaseAssetFlags.Persistent);
        set => Physical.BaseFlags = Physical.BaseFlags.WithFlag(BaseAssetFlags.Persistent, value);
    }

    /// <summary>
    /// Whether the entity stays visible while a cutscene plays.
    /// </summary>
    /// <remarks>
    /// Projected from <see cref="BaseAssetFlags.VisibleDuringCutscenes"/> in
    /// <see cref="Physical.IBaseAsset.BaseFlags"/>.
    /// </remarks>
    public bool VisibleDuringCutscenes
    {
        get => Physical.BaseFlags.HasFlag(BaseAssetFlags.VisibleDuringCutscenes);
        set => Physical.BaseFlags = Physical.BaseFlags.WithFlag(BaseAssetFlags.VisibleDuringCutscenes, value);
    }

    /// <summary>
    /// Whether shadows are drawn onto the entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ignored by <see cref="GameVersion.N100F"/> in most cases, see  
    /// <see cref="BaseAssetFlags.ReceiveShadows"/> for more details.
    /// </para>
    /// Projected from <see cref="BaseAssetFlags.ReceiveShadows"/> in <see cref="Physical.IBaseAsset.BaseFlags"/>.
    /// </remarks>
    public bool ReceivesShadows
    {
        get => Physical.BaseFlags.HasFlag(BaseAssetFlags.ReceiveShadows);
        set => Physical.BaseFlags = Physical.BaseFlags.WithFlag(BaseAssetFlags.ReceiveShadows, value);
    }

    /// <summary>
    /// The <see cref="Link"/>s this <see cref="BaseAsset"/> owns.
    /// </summary>
    public Collection<Link> Links { get; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IBaseAsset Physical => this;

    private AssetId? _overriddenBaseId;
    AssetId Physical.IBaseAsset.BaseId
    {
        get => _overriddenBaseId ?? Id;
        set => _overriddenBaseId = value == Id ? null : value;
    }

    private protected byte _baseType = baseType;
    byte Physical.IBaseAsset.BaseType
    {
        get => _baseType;
        set => _baseType = value;
    }

    private byte? _overriddenLinkCount;
    byte Physical.IBaseAsset.LinkCount
    {
        get => _overriddenLinkCount ?? (byte)Links.Count;
        set => _overriddenLinkCount = value == (byte)Links.Count ? null : value;
    }

    private BaseAssetFlags _baseFlags;
    BaseAssetFlags Physical.IBaseAsset.BaseFlags
    {
        get => _baseFlags;
        set => _baseFlags = value;
    }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="BaseAsset"/>'s underlying values.
    /// </summary>
    public interface IBaseAsset : IAsset
    {
        /// <summary>
        /// The ID the engine uses for link and scene lookups.
        /// </summary>
        /// <remarks>
        /// Stored in the asset's own data, independently of <see cref="Asset.Id"/>. When the two
        /// disagree, this field wins during serialization.
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
        /// <summary>
        /// The <see cref="BaseAsset"/>'s <see cref="BaseAssetFlags"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="BaseAsset.IsEnabled"/>, <see cref="BaseAsset.IsPersistent"/>,
        /// <see cref="BaseAsset.VisibleDuringCutscenes"/> and <see cref="BaseAsset.ReceivesShadows"/>
        /// project single bits of this field.
        /// </remarks>
        BaseAssetFlags BaseFlags { get; set; }
    }
}

/// <summary>
/// Flags controlling how a <see cref="BaseAsset"/> handles events, persists across scene resets, and is drawn.
/// </summary>
[Flags]
public enum BaseAssetFlags : short
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The <see cref="BaseAsset"/> responds to events and fires its <see cref="BaseAsset.Links"/>.
    /// </summary>
    /// <remarks>
    /// A disabled <see cref="BaseAsset"/> ignores every event except Enable. Disabling does not
    /// hide an entity, remove its collision, or stop updates. It only affects links.
    /// </remarks>
    Enabled = 1 << 0,
    /// <summary>
    /// The <see cref="BaseAsset"/>'s state is saved with the scene and restored after a scene reset.
    /// </summary>
    Persistent = 1 << 1,
    /// <summary>
    /// Ignored.
    /// </summary>
    /// <remarks>
    /// The engine sets this bit on every <see cref="BaseAsset"/> when it loads or resets, whatever the
    /// asset stores.
    /// </remarks>
    Valid = 1 << 2,
    /// <summary>
    /// The entity stays visible while a cutscene plays.
    /// </summary>
    /// <remarks>
    /// Entities without it are hidden for the cutscene's duration.
    /// </remarks>
    VisibleDuringCutscenes = 1 << 3,
    /// <summary>
    /// Shadows are drawn onto the entity.
    /// </summary>
    /// <remarks>
    /// In <see cref="GameVersion.N100F"/>, only platforms, simple objects, buttons and
    /// destructible objects receive shadows, and this flag is honoured only in scene <c>W027</c>;
    /// in every other scene they receive shadows whether or not it is set.
    /// </remarks>
    ReceiveShadows = 1 << 4,
    /// <summary>
    /// The entity is never skipped by distance-based update culling.
    /// </summary>
    /// <remarks>
    /// Without it, an entity farther from the camera than its cull distance stops being updated until
    /// the camera comes back in range. The cull distance is 70 units, or 10 units beyond the no-render
    /// distance of the LOD table entry for the entity's model when it has one. Ignored by
    /// <see cref="GameVersion.N100F"/>, which has no update culling.
    /// </remarks>
    NeverUpdateCulled = 1 << 7,
}
