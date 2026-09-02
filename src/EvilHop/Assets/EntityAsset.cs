using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Validation;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="BaseAsset"/> representing an object placed in the game world.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/EvilEngine/Assets#Entity_Assets">Heavy Iron Modding documentation</seealso>
/// </remarks>
public abstract class EntityAsset : BaseAsset, IPhysicalEntityAsset
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
    public RgbaColor ColorMultiplier { get; set; }

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalEntityAsset Physical => this;

    private protected byte _subtype;
    byte IPhysicalEntityAsset.Subtype { get => _subtype; set => _subtype = value; }

    private protected CollisionFlags _collisionFlags;
    CollisionFlags IPhysicalEntityAsset.CollisionFlags { get => _collisionFlags; set => _collisionFlags = value; }

    private protected byte _pFlags;
    byte IPhysicalEntityAsset.PFlags { get => _pFlags; set => _pFlags = value; }

    private protected AssetId _surfaceId;
    AssetId IPhysicalEntityAsset.SurfaceId { get => _surfaceId; set => _surfaceId = value; }

    private protected AssetId _modelId;
    AssetId IPhysicalEntityAsset.ModelId { get => _modelId; set => _modelId = value; }

    private protected AssetId _animListId;
    AssetId IPhysicalEntityAsset.AnimListId { get => _animListId; set => _animListId = value; }

    private protected float _seeThroughSpeed;
    float IPhysicalEntityAsset.SeeThroughSpeed { get => _seeThroughSpeed; set => _seeThroughSpeed = value; }

    /// <summary>
    /// Reads a single <see cref="CollisionFlags"/> bit, for a derived type projecting it as a
    /// named trait. Traits must project the bit, never store a copy of it.
    /// </summary>
    private protected bool HasCollisionFlag(CollisionFlags flag) => (_collisionFlags & flag) != 0;

    /// <summary>
    /// Sets or clears a single <see cref="CollisionFlags"/> bit, leaving every other bit alone.
    /// </summary>
    private protected void SetCollisionFlag(CollisionFlags flag, bool value) =>
        _collisionFlags = value ? _collisionFlags | flag : _collisionFlags & ~flag;
}

/// <summary>
/// An explicit interface used to interact with <see cref="EntityAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalEntityAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// The <see cref="EntityAsset"/>'s subtype, if applicable.
    /// </summary>
    /// <remarks>
    /// <para>Used by:</para>
    /// <list type="bullet">
    /// <item><see cref="AssetType.Pickup"/></item>
    /// <item><see cref="AssetType.Platform"/></item>
    /// <item><see cref="AssetType.Trigger"/></item>
    /// </list>
    /// <para>
    /// Recorded per asset type as well as across the corpus, since "which subtypes does this type
    /// use" is the question the list above answers by hand today.
    /// </para>
    /// </remarks>
    [Observed(Cardinality = ObservableCardinality.Enumerated)]
    [Observed(Cardinality = ObservableCardinality.Enumerated, By = ObservableGrouping.AssetType)]
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

/// <summary>
/// An RGBA color multiplier.
/// </summary>
/// <param name="R">The red channel.</param>
/// <param name="G">The green channel.</param>
/// <param name="B">The blue channel.</param>
/// <param name="A">The alpha channel.</param>
public readonly record struct RgbaColor(float R, float G, float B, float A);

/// <summary>
/// Represents all known values for <see cref="EntityAsset.EntityFlags"/>.
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
/// Represents all known values for <see cref="IPhysicalEntityAsset.CollisionFlags"/>.
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
    /// Unknown. Used by <see cref="AssetType.ElectricArcGenerator"/>.
    /// </summary>
    Unknown = 1 << 2,
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
