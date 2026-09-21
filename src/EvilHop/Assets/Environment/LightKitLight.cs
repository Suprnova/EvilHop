using EvilHop.Primitives;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// One light in a <see cref="LightKitAsset"/>.
/// </summary>
/// <remarks>
/// In decompiled source, <see cref="Right"/>, <see cref="Up"/>, and <see cref="At"/> are individually
/// normalized and assembled into the light's orientation, with <see cref="Right"/> and <see cref="At"/>
/// negated in the process; <see cref="Position"/> becomes its world position. For an
/// <see cref="LightKitLightType.Ambient"/> light, all four are <see cref="Vector3.Zero"/>.
/// </remarks>
public sealed class LightKitLight
{
    /// <summary>
    /// This light's type, determining which of this light's other fields apply.
    /// </summary>
    public LightKitLightType Type { get; set; }

    /// <summary>
    /// This light's color.
    /// </summary>
    public Rgba Color { get; set; }

    /// <summary>
    /// The right vector of this light's orientation.
    /// </summary>
    public Vector3 Right { get; set; }

    /// <summary>
    /// The up vector of this light's orientation.
    /// </summary>
    public Vector3 Up { get; set; }

    /// <summary>
    /// The direction a <see cref="LightKitLightType.Directional"/> light points towards, as an angle
    /// throughout the level rather than a position.
    /// </summary>
    public Vector3 At { get; set; }

    /// <summary>
    /// This light's world position, for <see cref="LightKitLightType.Point"/> and
    /// <see cref="LightKitLightType.Spot"/> lights.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Unknown. The fourth component alongside <see cref="Position"/>, forming a complete
    /// homogeneous 4-vector with <see cref="Right"/>/<see cref="Up"/>/<see cref="At"/> (each of whose
    /// own fourth components are always 0 and not modelled). Always 1 for every
    /// non-<see cref="LightKitLightType.Ambient"/> light type.
    /// </summary>
    public float PositionW { get; set; }

    /// <summary>
    /// The radius of a <see cref="LightKitLightType.Point"/> or <see cref="LightKitLightType.Spot"/>
    /// light.
    /// </summary>
    public float Radius { get; set; }

    /// <summary>
    /// The cone angle of a <see cref="LightKitLightType.Spot"/> light.
    /// </summary>
    public float Angle { get; set; }
}

/// <summary>
/// Defines the lighting type of a <see cref="LightKitLight"/>.
/// </summary>
public enum LightKitLightType : uint
{
    /// <summary>
    /// A uniform light with no direction or position, illuminating everything equally.
    /// </summary>
    Ambient = 1,
    /// <summary>
    /// A light shining uniformly along <see cref="LightKitLight.At"/> from everywhere, with no
    /// position of its own.
    /// </summary>
    Directional = 2,
    /// <summary>
    /// A light radiating from <see cref="LightKitLight.Position"/> in every direction, out to
    /// <see cref="LightKitLight.Radius"/>.
    /// </summary>
    Point = 3,
    /// <summary>
    /// A light radiating from <see cref="LightKitLight.Position"/> in a cone along
    /// <see cref="LightKitLight.At"/>, out to <see cref="LightKitLight.Radius"/> and
    /// <see cref="LightKitLight.Angle"/>.
    /// </summary>
    Spot = 4,
}
