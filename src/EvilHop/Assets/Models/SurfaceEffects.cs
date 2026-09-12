using EvilHop.Common;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// A <see cref="SurfaceAsset"/>'s material appearance.
/// </summary>
public sealed class SurfaceMaterialFx
{
    /// <summary>
    /// Unknown. Observed values are 0, 1, and 8.
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// The bump map applied to this surface, if any. Always <see cref="AssetId.None"/> in every
    /// sample checked.
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
    /// How bumpy this surface's bump map is. Always 0 in every sample checked.
    /// </summary>
    public float Bumpiness { get; set; }

    /// <summary>
    /// A secondary map applied to this surface, if any. Always <see cref="AssetId.None"/> in every
    /// sample checked.
    /// </summary>
    public AssetId DualMapId { get; set; }
}

/// <summary>
/// A <see cref="SurfaceAsset"/>'s color animation.
/// </summary>
public sealed class SurfaceColorFx
{
    /// <summary>
    /// Unknown. Always 0x000E in every sample checked.
    /// </summary>
    public ushort Flags { get; set; } = 0x000E;

    /// <summary>
    /// Unknown. Always 0 in every sample checked.
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
    /// How this animation drives <see cref="Translation"/>/<see cref="Scale"/>.
    /// </summary>
    public SurfaceUvfxMode Mode { get; set; }

    /// <summary>
    /// The current UV rotation, in degrees. The wiki claims this is always 0; real archives show
    /// 0, 90, 180, and 270.
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
    /// The speed <see cref="Scale"/> advances at. Usually <see cref="Vector3.Zero"/>.
    /// </summary>
    public Vector3 ScaleSpeed { get; set; }

    /// <summary>
    /// For <see cref="SurfaceUvfxMode.MinMaxOscillate"/>, the low end of the UV translation range.
    /// Usually <see cref="Vector3.Zero"/>.
    /// </summary>
    public Vector3 Min { get; set; }

    /// <summary>
    /// For <see cref="SurfaceUvfxMode.MinMaxOscillate"/>, the high end of the UV translation range.
    /// Usually <see cref="Vector3.Zero"/>.
    /// </summary>
    public Vector3 Max { get; set; }

    /// <summary>
    /// For <see cref="SurfaceUvfxMode.MinMaxOscillate"/>, how quickly the translation oscillates
    /// between <see cref="Min"/> and <see cref="Max"/>. Usually <see cref="Vector3.Zero"/>.
    /// </summary>
    public Vector3 MinMaxSpeed { get; set; }
}

/// <summary>
/// Represents all known values for <see cref="SurfaceAsset.TextureAnimFlags"/>.
/// </summary>
[Flags]
public enum SurfaceTextureAnimFlags : uint
{
    /// <summary>
    /// Neither texture animation is active.
    /// </summary>
    None = 0,
    /// <summary>
    /// The first of <see cref="SurfaceAsset.TextureAnims"/> is active.
    /// </summary>
    Slot0 = 1 << 0,
    /// <summary>
    /// The second of <see cref="SurfaceAsset.TextureAnims"/> is active.
    /// </summary>
    Slot1 = 1 << 1,
}

/// <summary>
/// Represents all known values for <see cref="SurfaceTextureAnim.Mode"/>.
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
/// Represents all known values for <see cref="SurfaceAsset.UvfxFlags"/>.
/// </summary>
[Flags]
public enum SurfaceUvfxFlags : uint
{
    /// <summary>
    /// Neither UV animation is active.
    /// </summary>
    None = 0,
    /// <summary>
    /// The first of <see cref="SurfaceAsset.Uvfxs"/> is active.
    /// </summary>
    Slot0 = 1 << 0,
    /// <summary>
    /// The second of <see cref="SurfaceAsset.Uvfxs"/> is active.
    /// </summary>
    Slot1 = 1 << 1,
}

/// <summary>
/// Represents all known values for <see cref="SurfaceUvfx.Mode"/>.
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
