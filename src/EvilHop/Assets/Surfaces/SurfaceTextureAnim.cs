using EvilHop.Common;

namespace EvilHop.Assets;

/// <summary>
/// One of a <see cref="SurfaceAsset"/>'s two texture animations: cycles through the models in
/// <see cref="Group"/>.
/// </summary>
public sealed class SurfaceTextureAnim
{
    /// <summary>
    /// Whether this animation is active.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// How this animation advances through <see cref="Group"/>.
    /// </summary>
    public SurfaceTextureAnimMode Mode { get; set; }

    /// <summary>
    /// The <see cref="AssetType.Group"/> of models this animation cycles through.
    /// </summary>
    public AssetId Group { get; set; }

    /// <summary>
    /// How quickly this animation advances, in frames per second.
    /// </summary>
    public float Speed { get; set; }
}

/// <summary>
/// Defines how a surface texture animation advances through its target group of models.
/// </summary>
public enum SurfaceTextureAnimMode : ushort
{
    /// <summary>
    /// Cycles forward through the group.
    /// </summary>
    Forward = 0,
    /// <summary>
    /// Cycles backward through the group.
    /// </summary>
    Backward = 1,
    /// <summary>
    /// Jumps to a random member of the group.
    /// </summary>
    Random = 2,
}
