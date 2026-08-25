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
public abstract class EntityAsset : BaseAsset
{
    /// <summary>
    /// Information about the <see cref="EntityAsset"/>'s properties in-game.
    /// </summary>
    public EntityFlags EntityFlags { get; set; }

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
    /// </remarks>
    public byte Subtype { get; set; }

    /// <summary>
    /// Unknown. Always 0.
    /// </summary>
    public byte PFlags { get; set; }

    /// <summary>
    /// The <see cref="EntityAsset"/>'s <c>moreFlags</c> byte.
    /// </summary>
    public CollisionFlags CollisionFlags { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Surface"/> asset this
    /// <see cref="EntityAsset"/> uses, if any.
    /// </summary>
    /// <remarks>
    /// <para>Used by:</para>
    /// <list type="bullet">
    /// <item><see cref="AssetType.ElectricArcGenerator"/></item>
    /// <item><see cref="AssetType.SimpleObject"/></item>
    /// <item><see cref="AssetType.Platform"/></item>
    /// <item><see cref="AssetType.UI"/></item>
    /// </list>
    /// </remarks>
    public AssetId SurfaceId { get; set; }

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

    // TODO: see if this should be a Color struct (System.Drawing doesn't seem like it'll work)

    /// <summary>
    /// The red channel of the <see cref="EntityAsset"/>'s color multiplier.
    /// </summary>
    public float RedMultiplier { get; set; }

    /// <summary>
    /// The green channel of the <see cref="EntityAsset"/>'s color multiplier.
    /// </summary>
    public float GreenMultiplier { get; set; }

    /// <summary>
    /// The blue channel of the <see cref="EntityAsset"/>'s color multiplier.
    /// </summary>
    public float BlueMultiplier { get; set; }

    /// <summary>
    /// The <see cref="EntityAsset"/>'s alpha channel multiplier.
    /// </summary>
    public float SeeThrough { get; set; }

    /// <summary>
    /// Unknown. Usually 255.
    /// </summary>
    public float SeeThroughSpeed { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> or <see cref="AssetType.ModelInfo"/>
    /// that this <see cref="EntityAsset"/> uses, if any.
    /// </summary>
    /// <remarks>
    /// <para>Used by:</para>
    /// <list type="bullet">
    /// <item><see cref="AssetType.Boulder"/></item>
    /// <item><see cref="AssetType.Button"/></item>
    /// <item><see cref="AssetType.DestructibleObject"/></item>
    /// <item><see cref="AssetType.ElectricArcGenerator"/></item>
    /// <item><see cref="AssetType.Hangable"/></item>
    /// <item><see cref="AssetType.Pendulum"/></item>
    /// <item><see cref="AssetType.Platform"/></item>
    /// <item><see cref="AssetType.Pickup"/></item>
    /// <item><see cref="AssetType.Player"/></item>
    /// <item><see cref="AssetType.SimpleObject"/></item>
    /// <item><see cref="AssetType.UI"/></item>
    /// <item><see cref="AssetType.Villain"/></item>
    /// </list>
    /// </remarks>
    public AssetId ModelId { get; set; }

    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Animation"/> or
    /// <see cref="AssetType.AnimationList"/> that this <see cref="EntityAsset"/> uses, if any.
    /// </summary>
    /// <remarks>
    /// <para>Used by:</para>
    /// <list type="bullet">
    /// <item><see cref="AssetType.DestructibleObject"/></item>
    /// <item><see cref="AssetType.Platform"/></item>
    /// <item><see cref="AssetType.SimpleObject"/></item>
    /// </list>
    /// <para>If <see langword="this"/> is a <see cref="AssetType.UI"/> asset, this points to a
    /// <see cref="AssetType.Sound"/> asset instead.</para>
    /// </remarks>
    public AssetId AnimListId { get; set; }
}

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
/// Represents all known values for <see cref="EntityAsset.CollisionFlags"/>.
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
    /// <remarks>
    /// <para>Used by:</para>
    /// <list type="bullet">
    /// <item><see cref="AssetType.Boulder"/></item>
    /// <item><see cref="AssetType.DestructibleAsset"/></item>
    /// <item><see cref="AssetType.SimpleObject"/></item>
    /// <item><see cref="AssetType.Villain"/></item>
    /// </list>
    /// </remarks>
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
    NoShadow = 1 << 5,
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
