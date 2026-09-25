using EvilHop.Common;
using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> representing an object placed in the game world.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Assets#Entity_Assets">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class EntityAsset(AssetType type, byte baseType = 0) : BaseAsset(type, baseType), IEntity, Physical.IEntityAsset
{
    /// <summary>
    /// Information about the <see cref="EntityAsset"/>'s properties in-game.
    /// </summary>
    public EntityFlags EntityFlags { get; set; }

    /// <summary>
    /// The <see cref="EntityAsset"/>'s rotation.
    /// </summary>
    public Vector3 Angle { get; set; }

    /// <summary>
    /// The <see cref="EntityAsset"/>'s position in the game world.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// The <see cref="EntityAsset"/>'s scale.
    /// </summary>
    public Vector3 Scale { get; set; }

    /// <summary>
    /// The <see cref="EntityAsset"/>'s color multiplier, alpha included.
    /// </summary>
    public Rgba ColorMultiplier { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.IEntityAsset Physical => this;

    Physical.IEntity IEntity.Physical => this;

    byte Physical.IEntity.Subtype { get => Subtype; set => Subtype = value; }

    /// <summary>
    /// Backs <see cref="Physical.IEntity.Subtype"/>, for a derived type whose subtype follows
    /// from its own data.
    /// </summary>
    private protected virtual byte Subtype { get; set; }

    private protected CollisionFlags _collisionFlags;
    CollisionFlags Physical.IEntity.CollisionFlags { get => _collisionFlags; set => _collisionFlags = value; }

    private protected byte _pFlags;
    byte Physical.IEntity.PFlags { get => _pFlags; set => _pFlags = value; }

    private protected AssetId _surfaceId;
    AssetId Physical.IEntity.SurfaceId { get => _surfaceId; set => _surfaceId = value; }

    private protected AssetId _modelId;
    AssetId Physical.IEntity.ModelId { get => _modelId; set => _modelId = value; }

    private protected AssetId _animListId;
    AssetId Physical.IEntity.AnimListId { get => _animListId; set => _animListId = value; }

    private protected float _seeThroughSpeed;
    float Physical.IEntity.SeeThroughSpeed { get => _seeThroughSpeed; set => _seeThroughSpeed = value; }

    /// <summary>
    /// Reads a single <see cref="CollisionFlags"/> bit, for a derived type projecting it as a
    /// named trait. Traits must project the bit, never store a copy of it.
    /// </summary>
    private protected bool HasCollisionFlag(CollisionFlags flag) => _collisionFlags.HasFlag(flag);

    /// <summary>
    /// Sets or clears a single <see cref="CollisionFlags"/> bit, leaving every other bit alone.
    /// </summary>
    private protected void SetCollisionFlag(CollisionFlags flag, bool value) =>
        _collisionFlags = _collisionFlags.WithFlag(flag, value);
}

/// <summary>
/// The xEntAsset fields every entity stores after its <see cref="BaseAsset"/> header. Implemented by
/// <see cref="EntityAsset"/>, and by an entity embedded inside another asset
/// (<see cref="DuplicatorAsset.Villain"/>), so <see cref="Serialization.EntityAssetPrefix"/> can serve
/// both without the embedded one pretending to be an <see cref="Asset"/>.
/// </summary>
internal interface IEntity
{
    /// <inheritdoc cref="EntityAsset.EntityFlags"/>
    EntityFlags EntityFlags { get; set; }

    /// <inheritdoc cref="EntityAsset.Angle"/>
    Vector3 Angle { get; set; }

    /// <inheritdoc cref="EntityAsset.Position"/>
    Vector3 Position { get; set; }

    /// <inheritdoc cref="EntityAsset.Scale"/>
    Vector3 Scale { get; set; }

    /// <inheritdoc cref="EntityAsset.ColorMultiplier"/>
    Rgba ColorMultiplier { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    Physical.IEntity Physical { get; }
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="EntityAsset"/>'s underlying values.
    /// </summary>
    /// <remarks>
    /// Adds nothing beyond <see cref="IEntity"/> - it exists purely so <see cref="EntityAsset.Physical"/>
    /// includes <see cref="IBaseAsset"/>'s header fields, which <see cref="IEntity"/> deliberately omits
    /// so it can also describe an entity embedded inside another asset.
    /// </remarks>
    public interface IEntityAsset : IBaseAsset, IEntity;

    /// <summary>
    /// An explicit interface used to interact with an entity's underlying values, whether it's an
    /// <see cref="EntityAsset"/> or embedded inside another asset.
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// The <see cref="EntityAsset"/>'s subtype, if applicable.
        /// </summary>
        /// <remarks>
        /// <para>Used by:</para>
        /// <list type="bullet">
        /// <item><see cref="AssetType.NPC"/></item>
        /// <item><see cref="AssetType.Pickup"/></item>
        /// <item><see cref="AssetType.Platform"/></item>
        /// <item><see cref="AssetType.Trigger"/></item>
        /// </list>
        /// </remarks>
        byte Subtype { get; set; }
        /// <summary>
        /// Unknown. Always 0.
        /// </summary>
        byte PFlags { get; set; }
        /// <summary>
        /// Flags relating to this <see cref="EntityAsset"/>'s collision.
        /// </summary>
        CollisionFlags CollisionFlags { get; set; }
        /// <summary>
        /// The <see cref="AssetId"/> of the <see cref="AssetType.Surface"/> asset this
        /// <see cref="EntityAsset"/> uses, if any.
        /// </summary>
        AssetId SurfaceId { get; set; }
        /// <summary>
        /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> or <see cref="AssetType.ModelInfo"/>
        /// that this <see cref="EntityAsset"/> uses, if any.
        /// </summary>
        AssetId ModelId { get; set; }
        /// <summary>
        /// The <see cref="AssetId"/> of the <see cref="AssetType.Animation"/> or
        /// <see cref="AssetType.AnimationList"/> that this <see cref="EntityAsset"/> uses, if any.
        /// </summary>
        AssetId AnimListId { get; set; }
        /// <summary>
        /// Always 255. Unused.
        /// </summary>
        float SeeThroughSpeed { get; set; }
    }
}

/// <summary>
/// Flags governing the rendering, visibility, and shadow behavior of an <see cref="EntityAsset"/>.
/// </summary>
[Flags]
public enum EntityFlags : byte
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The <see cref="EntityAsset"/> is visible.
    /// </summary>
    /// <remarks>
    /// <para>Used by:</para>
    /// <list type="bullet">
    /// <item><see cref="AssetType.Boulder"/></item>
    /// <item><see cref="AssetType.Button"/></item>
    /// <item><see cref="AssetType.DestructibleObject"/></item>
    /// <item><see cref="AssetType.ElectricArcGenerator"/></item>
    /// <item><see cref="AssetType.Pickup"/></item>
    /// <item><see cref="AssetType.Platform"/></item>
    /// <item><see cref="AssetType.Player"/></item>
    /// <item><see cref="AssetType.SimpleObject"/></item>
    /// <item><see cref="AssetType.Trigger"/></item>
    /// <item><see cref="AssetType.UI"/></item>
    /// <item><see cref="AssetType.UIFont"/></item>
    /// <item><see cref="AssetType.Villain"/></item>
    /// </list>
    /// </remarks>
    Visible = 1 << 0,
    /// <summary>
    /// The <see cref="EntityAsset"/> is capable of falling and stacking on top of other objects.
    /// </summary>
    /// <remarks>
    /// <para>Known to work with:</para>
    /// <list type="bullet">
    /// <item><see cref="AssetType.Platform"/></item>
    /// <item><see cref="AssetType.Button"/></item>
    /// <item><see cref="AssetType.DestructibleObject"/></item>
    /// <item><see cref="AssetType.Villain"/></item>
    /// </list>
    /// <para>Will crash the game if enabled on a <see cref="AssetType.SimpleObject"/>.</para>
    /// </remarks>
    Stackable = 1 << 1,
    /// <summary>
    /// Unknown. Used by <see cref="AssetType.Platform"/>.
    /// </summary>
    Unknown = 1 << 3,
    /// <summary>
    /// Disables shadow rendering. Used by <see cref="AssetType.Villain"/>.
    /// </summary>
    NoShadow = 1 << 6,
}

/// <summary>
/// Flags governing the physical collision, hit detection, and interaction properties of an <see cref="EntityAsset"/>.
/// </summary>
[Flags]
public enum CollisionFlags : byte
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The <see cref="EntityAsset"/>'s collision is the shape of its model.
    /// </summary>
    /// <remarks>
    /// <para>Used by:</para>
    /// <list type="bullet">
    /// <item><see cref="AssetType.Button"/></item>
    /// <item><see cref="AssetType.DestructibleObject"/></item>
    /// <item><see cref="AssetType.ElectricArcGenerator"/></item>
    /// <item><see cref="AssetType.Pickup"/></item>
    /// <item><see cref="AssetType.Platform"/></item>
    /// <item><see cref="AssetType.SimpleObject"/></item>
    /// <item><see cref="AssetType.UI"/></item>
    /// <item><see cref="AssetType.UIFont"/></item>
    /// <item><see cref="AssetType.Villain"/></item>
    /// </list>
    /// </remarks>
    PreciseCollision = 1 << 1,
    /// <summary>
    /// Disables player and NPC collision against the <see cref="EntityAsset"/>. Read by
    /// <see cref="AssetType.ElectricArcGenerator"/>.
    /// </summary>
    NoPlayerOrNpcCollision = 1 << 2,
    /// <summary>
    /// The <see cref="EntityAsset"/> is grabbable by Patrick in <see cref="GameVersion.BFBB"/>
    /// and <see cref="GameVersion.TSSM"/>.
    /// </summary>
    Grabbable = 1 << 3,
    /// <summary>
    /// The <see cref="EntityAsset"/> sends <b>Hit</b> events when attacked.
    /// </summary>
    Hittable = 1 << 4,
    /// <summary>
    /// The <see cref="EntityAsset"/>'s collision shape will update as its
    /// <see cref="AssetType.Animation"/> updates. Requires <see cref="PreciseCollision"/>.
    /// </summary>
    /// <remarks>
    /// <para>Used by:</para>
    /// <list type="bullet">
    /// <item><see cref="AssetType.Platform"/></item>
    /// <item><see cref="AssetType.SimpleObject"/></item>
    /// </list>
    /// </remarks>
    AnimateCollision = 1 << 5,
    /// <summary>
    /// The <see cref="EntityAsset"/> can be ledge-grabbed by the player. Requires
    /// <see cref="PreciseCollision"/>.
    /// </summary>
    /// <remarks>
    /// <para>Used by:</para>
    /// <list type="bullet">
    /// <item><see cref="AssetType.Platform"/></item>
    /// <item><see cref="AssetType.SimpleObject"/></item>
    /// </list>
    /// </remarks>
    LedgeGrab = 1 << 7,
}
