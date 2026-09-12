using EvilHop.Common;
using System.Collections.Immutable;
using System.Numerics;

namespace EvilHop.Assets;

/// <summary>
/// Physical and cosmetic properties applied to a <see cref="AssetType.Platform"/>,
/// <see cref="AssetType.SimpleObject"/>, <see cref="AssetType.Boulder"/>, or a region of a
/// <see cref="AssetType.JSP"/> through a <see cref="AssetType.SurfaceMapper"/>: friction, hazards,
/// wall-jump/ledge-grab/out-of-bounds behavior, and texture or UV animation.
/// </summary>
/// <remarks>
/// <seealso href="https://heavyironmodding.org/wiki/SURF">Heavy Iron Modding documentation</seealso>
/// </remarks>
public sealed partial class SurfaceAsset : BaseAsset, IPhysicalSurfaceAsset
{
    /// <summary>
    /// A damage category applied while standing on or touching this surface. Observed values are 0,
    /// 1, 2, 3, 4, 5, and 6; only 6 ("Hazard?" per the wiki) is guessed at with any confidence.
    /// </summary>
    public byte GameDamageType { get; set; }

    /// <summary>
    /// Whether this surface is "sticky". Always 0 in every sample checked.
    /// </summary>
    public byte GameSticky { get; set; }

    /// <summary>
    /// Flags controlling <see cref="GameDamageType"/>'s damage.
    /// </summary>
    public SurfaceGameDamageFlags GameDamageFlags { get; set; }

    /// <summary>
    /// The angle, in degrees, below which the player is not slid off this surface.
    /// </summary>
    public byte SlideStartAngle { get; set; }

    /// <summary>
    /// The angle, in degrees, above which the player is fully slid off this surface.
    /// </summary>
    public byte SlideStopAngle { get; set; }

    /// <summary>
    /// This surface's physics behavior.
    /// </summary>
    public SurfacePhysicsFlags PhysFlags { get; set; }

    /// <summary>
    /// This surface's friction, from 0 to 1.
    /// </summary>
    public float Friction { get; set; }

    /// <summary>
    /// This surface's material appearance (bump/environment mapping, shininess).
    /// </summary>
    public SurfaceMaterialFx MaterialFx { get; set; } = new();

    /// <summary>
    /// This surface's color animation.
    /// </summary>
    public SurfaceColorFx ColorFx { get; set; } = new();

    /// <summary>
    /// Which of <see cref="TextureAnims"/> are active.
    /// </summary>
    public SurfaceTextureAnimFlags TextureAnimFlags { get; set; }

    private ImmutableArray<SurfaceTextureAnim> _textureAnims = DefaultTextureAnims();

    /// <summary>
    /// This surface's two independent texture animations.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 2.</exception>
    public ImmutableArray<SurfaceTextureAnim> TextureAnims
    {
        get => _textureAnims;
        set => _textureAnims = value.Length == 2
            ? value
            : throw new ArgumentException($"{nameof(TextureAnims)} must contain exactly 2 elements.", nameof(value));
    }

    /// <summary>
    /// Which of <see cref="Uvfxs"/> are active.
    /// </summary>
    public SurfaceUvfxFlags UvfxFlags { get; set; }

    private ImmutableArray<SurfaceUvfx> _uvfxs = DefaultUvfxs();

    /// <summary>
    /// This surface's two independent UV animations.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 2.</exception>
    public ImmutableArray<SurfaceUvfx> Uvfxs
    {
        get => _uvfxs;
        set => _uvfxs = value.Length == 2
            ? value
            : throw new ArgumentException($"{nameof(Uvfxs)} must contain exactly 2 elements.", nameof(value));
    }

    /// <summary>
    /// Whether this surface starts enabled.
    /// </summary>
    public byte On { get; set; }

    /// <summary>
    /// The time, in seconds, the player can remain on this surface before an out-of-bounds
    /// <see cref="SurfacePhysicsFlags.OutOfBounds"/> reset triggers. Always -1 (i.e. the flag's own
    /// default delay applies) in most samples checked.
    /// </summary>
    public float OutOfBoundsDelay { get; set; }

    /// <summary>
    /// Scales the player's horizontal wall-jump velocity off this surface. Always 1 in every sample
    /// checked.
    /// </summary>
    public float WallJumpScaleXZ { get; set; }

    /// <summary>
    /// Scales the player's vertical wall-jump velocity off this surface.
    /// </summary>
    public float WallJumpScaleY { get; set; }

    /// <summary>
    /// Unknown. The wiki claims this is stored little-endian regardless of platform; real archives
    /// across every game and platform checked contradict that and store it the same way as every
    /// other field.
    /// </summary>
    public float DamageTimer { get; set; }

    /// <summary>
    /// Unknown. The wiki claims this is stored little-endian regardless of platform; real archives
    /// across every game and platform checked contradict that and store it the same way as every
    /// other field.
    /// </summary>
    public float DamageBounce { get; set; }

    /// <summary>
    /// Additional per-surface data appended after every field above and before
    /// <see cref="BaseAsset.Links"/>. Empty in most <see cref="GameVersion.BFBB"/> archives; from
    /// <see cref="GameVersion.TSSM"/> onward, typically 140 bytes, occasionally more in specific
    /// builds. Its layout is unknown - preserved byte-exact rather than modelled.
    /// </summary>
    public ImmutableArray<byte> ExtendedData { get; set; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override IPhysicalSurfaceAsset Physical => this;

    private byte _surfType;
    byte IPhysicalSurfaceAsset.SurfType { get => _surfType; set => _surfType = value; }

    internal SurfaceAsset() { }

    private static ImmutableArray<SurfaceTextureAnim> DefaultTextureAnims() => [new(), new()];
    private static ImmutableArray<SurfaceUvfx> DefaultUvfxs() => [new(), new()];
}

/// <summary>
/// An explicit interface used to interact with <see cref="SurfaceAsset"/>'s underlying values.
/// </summary>
public interface IPhysicalSurfaceAsset : IPhysicalBaseAsset
{
    /// <summary>
    /// Unknown. Always 0 in every sample checked.
    /// </summary>
    byte SurfType { get; set; }
}

/// <summary>
/// Represents all known values for <see cref="SurfaceAsset.GameDamageFlags"/>.
/// </summary>
[Flags]
public enum SurfaceGameDamageFlags : byte
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// Damage from <see cref="SurfaceAsset.GameDamageType"/> passes through invincibility.
    /// </summary>
    DamagePassthrough = 1 << 0,
}

/// <summary>
/// Represents all known values for <see cref="SurfaceAsset.PhysFlags"/>.
/// </summary>
/// <remarks>
/// The wiki lists <c>0x10</c> as "Wall Jump" and <c>0x20</c> as "Ledge Grab", but decompiled source
/// confirms <c>0x10</c> as <see cref="OutOfBounds"/>. Real archives also set <c>0x20</c> and
/// <c>0x40</c>, with no confirmed meaning for either.
/// </remarks>
[Flags]
public enum SurfacePhysicsFlags : byte
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,
    /// <summary>
    /// The player slides off this surface, per <see cref="SurfaceAsset.SlideStartAngle"/>/
    /// <see cref="SurfaceAsset.SlideStopAngle"/>.
    /// </summary>
    Slide = 1 << 0,
    /// <summary>
    /// The player's orientation matches this surface's angle.
    /// </summary>
    MatchOrient = 1 << 1,
    /// <summary>
    /// Unknown. Named "Step" in decompiled source.
    /// </summary>
    Step = 1 << 2,
    /// <summary>
    /// The player cannot stand on this surface.
    /// </summary>
    PreventStanding = 1 << 3,
    /// <summary>
    /// The player is considered out of bounds while on this surface.
    /// </summary>
    OutOfBounds = 1 << 4,
}
