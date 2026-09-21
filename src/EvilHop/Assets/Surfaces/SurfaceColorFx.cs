using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

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
