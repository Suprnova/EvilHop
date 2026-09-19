using EvilHop.Common;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="SurfaceAsset"/>'s material appearance.
/// </summary>
public sealed class SurfaceMaterialFx
{
    /// <summary>
    /// Unknown.
    /// </summary>
    public SurfaceMaterialFxFlags Flags { get; set; }

    /// <summary>
    /// The bump map applied to this surface, if any.
    /// </summary>
    public AssetId BumpMapId { get; set; }

    /// <summary>
    /// The environment map applied to this surface, if any.
    /// </summary>
    public AssetId EnvMapId { get; set; }

    /// <summary>
    /// How shiny this surface's environment reflection is.
    /// </summary>
    public float Shininess { get; set; }

    /// <summary>
    /// How bumpy this surface's bump map is.
    /// </summary>
    public float Bumpiness { get; set; }

    /// <summary>
    /// A secondary map applied to this surface, if any.
    /// </summary>
    public AssetId DualMapId { get; set; }
}

/// <summary>
/// A <see cref="SurfaceAsset"/>'s color animation.
/// </summary>
public sealed class SurfaceColorFx
{
    /// <summary>
    /// Unknown.
    /// </summary>
    public SurfaceColorFxFlags Flags { get; set; } = SurfaceColorFxFlags.Valid;

    /// <summary>
    /// Unknown.
    /// </summary>
    public ushort Mode { get; set; }

    /// <summary>
    /// The speed of this color animation.
    /// </summary>
    public float Speed { get; set; }
}

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
/// One of a <see cref="SurfaceAsset"/>'s two UV animations.
/// </summary>
public sealed class SurfaceUvfx
{
    /// <summary>
    /// Whether this animation is active.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// How this animation drives <see cref="Translation"/>/<see cref="Scale"/>.
    /// </summary>
    public SurfaceUvfxMode Mode { get; set; }

    /// <summary>
    /// The current UV rotation, in degrees.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// The speed <see cref="Rotation"/> advances at, in degrees per second.
    /// </summary>
    public float RotationSpeed { get; set; }

    /// <summary>
    /// The current UV translation. <see cref="Vector3.Z"/> is always 0.
    /// </summary>
    public Vector3 Translation { get; set; }

    /// <summary>
    /// The speed <see cref="Translation"/> advances at. <see cref="Vector3.Z"/> is always 0.
    /// </summary>
    public Vector3 TranslationSpeed { get; set; }

    /// <summary>
    /// The current UV scale. <see cref="Vector3.Z"/> is always 0.
    /// </summary>
    public Vector3 Scale { get; set; }

    /// <summary>
    /// The speed <see cref="Scale"/> advances at.
    /// </summary>
    public Vector3 ScaleSpeed { get; set; }

    /// <summary>
    /// For <see cref="SurfaceUvfxMode.MinMaxOscillate"/>, the low end of the UV translation range.
    /// </summary>
    public Vector3 Min { get; set; }

    /// <summary>
    /// For <see cref="SurfaceUvfxMode.MinMaxOscillate"/>, the high end of the UV translation range.
    /// </summary>
    public Vector3 Max { get; set; }

    /// <summary>
    /// For <see cref="SurfaceUvfxMode.MinMaxOscillate"/>, how quickly the translation oscillates
    /// between <see cref="Min"/> and <see cref="Max"/>.
    /// </summary>
    public Vector3 MinMaxSpeed { get; set; }
}

/// <summary>
/// Flags governing material shader and texture mapping effects applied to a surface.
/// </summary>
[Flags]
public enum SurfaceMaterialFxFlags : uint
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
}

/// <summary>
/// Flags governing color animation effects applied to a surface.
/// </summary>
[SuppressMessage("Design", "CA2217:Do not mark enums with FlagsAttribute", Justification = "Only the composite Valid mask is currently confirmed.")]
[Flags]
public enum SurfaceColorFxFlags : ushort
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Always set on active color effects.
    /// </summary>
    Valid = (1 << 1) | (1 << 2) | (1 << 3),
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

/// <summary>
/// Flags governing active texture animations on a surface.
/// </summary>
[Flags]
public enum SurfaceTextureAnimFlags : uint
{
    /// <summary>
    /// Neither texture animation is active.
    /// </summary>
    None = 0,

    /// <summary>
    /// The first texture animation (<see cref="SurfaceAsset.TextureAnims"/>[0]) is active.
    /// </summary>
    Slot0 = 1 << 0,

    /// <summary>
    /// The second texture animation (<see cref="SurfaceAsset.TextureAnims"/>[1]) is active.
    /// </summary>
    Slot1 = 1 << 1,
}

/// <summary>
/// Defines the animation mode governing UV coordinate scrolling and scaling on a surface.
/// </summary>
public enum SurfaceUvfxMode
{
    /// <summary>
    /// <see cref="SurfaceUvfx.Rotation"/>, <see cref="SurfaceUvfx.Translation"/>, and
    /// <see cref="SurfaceUvfx.Scale"/> advance continuously at their respective speeds, wrapping
    /// around.
    /// </summary>
    Continuous = 0,
    /// <summary>
    /// <see cref="SurfaceUvfx.Translation"/> and <see cref="SurfaceUvfx.Scale"/> follow a sine wave
    /// driven by the frame counter. Never observed in any real archive.
    /// </summary>
    Sine = 1,
    /// <summary>
    /// <see cref="SurfaceUvfx.Translation"/> oscillates between <see cref="SurfaceUvfx.Min"/> and
    /// <see cref="SurfaceUvfx.Max"/> at <see cref="SurfaceUvfx.MinMaxSpeed"/>.
    /// </summary>
    MinMaxOscillate = 2,
}

/// <summary>
/// Flags governing active UV coordinate animation effects on a surface.
/// </summary>
[Flags]
public enum SurfaceUvfxFlags : uint
{
    /// <summary>
    /// Neither UV animation is active.
    /// </summary>
    None = 0,

    /// <summary>
    /// The first UV animation (<see cref="SurfaceAsset.Uvfxs"/>[0]) is active.
    /// </summary>
    Slot0 = 1 << 0,

    /// <summary>
    /// The second UV animation (<see cref="SurfaceAsset.Uvfxs"/>[1]) is active.
    /// </summary>
    Slot1 = 1 << 1,
}

