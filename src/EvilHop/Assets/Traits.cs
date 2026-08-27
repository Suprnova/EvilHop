using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Assets;

/// <summary>
/// Indicates an <see cref="EntityAsset"/> is capable of being grabbed.
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
public interface IGrabbable
{
    /// <summary>
    /// The <see cref="EntityAsset"/> is grabbable by Patrick in <see cref="GameVersion.BFBB"/>
    /// and <see cref="GameVersion.TSSM"/>.
    /// </summary>
    public bool IsGrabbable { get; set; }
}

/// <summary>
/// Indicates an <see cref="EntityAsset"/> is capable of having a <see cref="AssetType.Surface"/>
/// applied to it.
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
public interface IHasSurface
{
    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Surface"/> asset this
    /// <see cref="EntityAsset"/> uses, if any.
    /// </summary>
    public AssetId SurfaceId { get; set; }
}

/// <summary>
/// Indicates an <see cref="EntityAsset"/> can be displayed with a <see cref="AssetType.Model"/>.
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
public interface IHasModel
{
    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Model"/> or <see cref="AssetType.ModelInfo"/>
    /// that this <see cref="EntityAsset"/> uses, if any.
    /// </summary>
    public AssetId ModelId { get; set; }
}

/// <summary>
/// Indicates an <see cref="EntityAsset"/> can be animated with an <see cref="AssetType.Animation"/>.
/// </summary>
/// <remarks>
/// <para>Used by:</para>
/// <list type="bullet">
/// <item><see cref="AssetType.DestructibleObject"/></item>
/// <item><see cref="AssetType.Platform"/></item>
/// <item><see cref="AssetType.SimpleObject"/></item>
/// </list>
/// </remarks>
/// TODO: Double check is UI -> SND relationship belongs here, or in another trait
public interface IHasAnimList
{
    /// <summary>
    /// The <see cref="AssetId"/> of the <see cref="AssetType.Animation"/> or
    /// <see cref="AssetType.AnimationList"/> that this <see cref="EntityAsset"/> uses, if any.
    /// </summary>
    public AssetId AnimListId { get; set; }
}
