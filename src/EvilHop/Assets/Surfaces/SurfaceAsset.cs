using EvilHop.Common;
using System.Collections.Immutable;

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
public sealed partial class SurfaceAsset() : BaseAsset(AssetType.Surface, baseType: 0x1A), Physical.ISurfaceAsset
{
    /// <summary>
    /// What touching this surface does to the player.
    /// </summary>
    public SurfaceGameDamageType GameDamageType { get; set; }

    /// <summary>
    /// Whether the player passes through this surface instead of colliding with it, while still
    /// taking its <see cref="GameDamageType"/> damage on contact.
    /// </summary>
    public bool DamagePassthrough
    {
        get => Physical.GameDamageFlags.HasFlag(SurfaceGameDamageFlags.DamagePassthrough);
        set => Physical.GameDamageFlags = value
            ? Physical.GameDamageFlags | SurfaceGameDamageFlags.DamagePassthrough
            : Physical.GameDamageFlags & ~SurfaceGameDamageFlags.DamagePassthrough;
    }

    /// <summary>
    /// The time, in seconds, the player is immune to this surface's damage after being hit by it.
    /// Further contact within the window neither damages nor knocks the player back. 0 defers to
    /// the game ini's <c>G.DamageTimeSurface</c>.
    /// </summary>
    public float DamageTimer { get; set; }

    /// <summary>
    /// The vertical velocity applied to the player as knockback when this surface damages them. 0
    /// defers to the game ini's <c>G.DamageSurfKnock</c>.
    /// </summary>
    public float DamageBounce { get; set; }

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

    private ImmutableArray<SurfaceTextureAnim> _textureAnims = DefaultTextureAnims();

    /// <summary>
    /// This surface's two independent texture animations. Each element's own
    /// <see cref="SurfaceTextureAnim.IsEnabled"/> says whether it is active.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 2.</exception>
    public ImmutableArray<SurfaceTextureAnim> TextureAnims
    {
        get => _textureAnims;
        set => _textureAnims = value.Length == 2
            ? value
            : throw new ArgumentException($"{nameof(TextureAnims)} must contain exactly 2 elements.", nameof(value));
    }

    private ImmutableArray<SurfaceUvfx> _uvfxs = DefaultUvfxs();

    /// <summary>
    /// This surface's two independent UV animations. Each element's own
    /// <see cref="SurfaceUvfx.IsEnabled"/> says whether it is active.
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
    public bool IsEnabled
    {
        get => Physical.IsEnabled != 0;
        set => Physical.IsEnabled = (byte)(value ? 1 : 0);
    }

    /// <summary>
    /// The time, in seconds, the player can remain out of bounds on this surface before being reset.
    /// Only takes effect when <see cref="SurfacePhysicsFlags.OutOfBounds"/> is set.
    /// </summary>
    /// <remarks>
    /// -1 defers to the game ini's <c>player.state.out_of_bounds.out_time</c>.
    /// </remarks>
    public float OutOfBoundsDelay { get; set; }

    /// <summary>
    /// Scales the player's horizontal wall-jump velocity off this surface. Applies only when
    /// <see cref="SurfacePhysicsFlags.WallJump"/> is set.
    /// </summary>
    public float WallJumpScaleXZ { get; set; }

    /// <summary>
    /// Scales the player's vertical wall-jump velocity off this surface. Applies only when
    /// <see cref="SurfacePhysicsFlags.WallJump"/> is set.
    /// </summary>
    public float WallJumpScaleY { get; set; }

    /// <summary>
    /// Additional per-surface data appended after every field above and before
    /// <see cref="BaseAsset.Links"/>. Empty in most <see cref="GameVersion.BFBB"/> archives; from
    /// <see cref="GameVersion.TSSM"/> onward, typically 140 bytes, occasionally more in specific
    /// builds. Its layout is unknown - preserved byte-exact rather than modelled.
    /// </summary>
    public ImmutableArray<byte> ExtendedData { get; set; } = [];

    /// <inheritdoc cref="Asset.Physical"/>
    public override Physical.ISurfaceAsset Physical => this;

    private SurfaceGameDamageFlags _gameDamageFlags;
    SurfaceGameDamageFlags Physical.ISurfaceAsset.GameDamageFlags { get => _gameDamageFlags; set => _gameDamageFlags = value; }

    private byte _surfType;
    byte Physical.ISurfaceAsset.SurfType { get => _surfType; set => _surfType = value; }

    private byte _gameSticky;
    byte Physical.ISurfaceAsset.GameSticky { get => _gameSticky; set => _gameSticky = value; }

    private byte _isEnabled = 1;
    byte Physical.ISurfaceAsset.IsEnabled { get => _isEnabled; set => _isEnabled = value; }

    private SurfaceTextureAnimFlags? _overriddenTextureAnimFlags;
    SurfaceTextureAnimFlags Physical.ISurfaceAsset.TextureAnimFlags
    {
        get => _overriddenTextureAnimFlags ?? DerivedTextureAnimFlags;
        set => _overriddenTextureAnimFlags = value == DerivedTextureAnimFlags ? null : value;
    }

    private SurfaceTextureAnimFlags DerivedTextureAnimFlags =>
        (TextureAnims[0].IsEnabled ? SurfaceTextureAnimFlags.Slot0 : SurfaceTextureAnimFlags.None) |
        (TextureAnims[1].IsEnabled ? SurfaceTextureAnimFlags.Slot1 : SurfaceTextureAnimFlags.None);

    private SurfaceUvfxFlags? _overriddenUvfxFlags;
    SurfaceUvfxFlags Physical.ISurfaceAsset.UvfxFlags
    {
        get => _overriddenUvfxFlags ?? DerivedUvfxFlags;
        set => _overriddenUvfxFlags = value == DerivedUvfxFlags ? null : value;
    }

    private SurfaceUvfxFlags DerivedUvfxFlags =>
        (Uvfxs[0].IsEnabled ? SurfaceUvfxFlags.Slot0 : SurfaceUvfxFlags.None) |
        (Uvfxs[1].IsEnabled ? SurfaceUvfxFlags.Slot1 : SurfaceUvfxFlags.None);

    private static ImmutableArray<SurfaceTextureAnim> DefaultTextureAnims() => [new(), new()];
    private static ImmutableArray<SurfaceUvfx> DefaultUvfxs() => [new(), new()];

    /// <summary>
    /// The <see cref="GameVersion"/>s <see cref="AssetType.Surface"/> is known to be read by.
    /// </summary>
    /// <remarks>
    /// <see cref="GameVersion.N100F"/>'s <c>SURF</c> layout is substantially smaller than every
    /// other game's and does not match decompiled source; it is not modelled here.
    /// </remarks>
    // TODO: Partial implementation - N100F uses a smaller SURF layout not modeled here.
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    /// <summary>
    /// Flags controlling collision and pass-through behavior when applying surface damage.
    /// </summary>
    [Flags]
    public enum SurfaceGameDamageFlags : byte
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,
        /// <summary>
        /// The player passes through this surface instead of colliding with it, while still taking its
        /// <see cref="SurfaceAsset.GameDamageType"/> damage on contact.
        /// </summary>
        DamagePassthrough = 1 << 0,
    }

    /// <summary>
    /// Flags governing player physics and mobility interactions with a surface.
    /// </summary>
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
        /// Unknown.
        /// </summary>
        Step = 1 << 2,
        /// <summary>
        /// The player cannot stand on this surface: contact with it from above is treated as airborne
        /// rather than grounded, and the player is pushed off rather than coming to rest.
        /// </summary>
        PreventStanding = 1 << 3,
        /// <summary>
        /// The player is considered out of bounds while on this surface, and is reset after
        /// <see cref="SurfaceAsset.OutOfBoundsDelay"/>.
        /// </summary>
        OutOfBounds = 1 << 4,
        /// <summary>
        /// The player can wall jump off this surface. Required for
        /// <see cref="SurfaceAsset.WallJumpScaleXZ"/> and <see cref="SurfaceAsset.WallJumpScaleY"/> to
        /// apply - without it the move does not trigger at all.
        /// </summary>
        WallJump = 1 << 5,
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
}

public static partial class Physical
{
    /// <summary>
    /// An explicit interface used to interact with <see cref="SurfaceAsset"/>'s underlying values.
    /// </summary>
    public interface ISurfaceAsset : IBaseAsset
    {
        /// <summary>
        /// Flags controlling how this surface's damage is applied, read directly from disk.
        /// </summary>
        /// <remarks>
        /// <see cref="SurfaceAsset.SurfaceGameDamageFlags.DamagePassthrough"/> is exposed logically as
        /// <see cref="SurfaceAsset.DamagePassthrough"/>.
        /// </remarks>
        SurfaceAsset.SurfaceGameDamageFlags GameDamageFlags { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        /// TODO: decomp's zFeetStepVillainCB calls zFeetGetIDs (which calls zSurfaceGetName), maybe
        /// this field changes footstep sounds for NPCs?
        byte SurfType { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        /// TODO: decomp's zThrown has a copy of this field, maybe it controls whether thrown objects
        /// (i.e. melons, tikis) stick to the surface? might also affect Friction?
        /// TODO: 1 for "sticky" surfaces (where you can't jump without the boots) in n100f, unclear
        /// if it does stuff in other games.
        byte GameSticky { get; set; }

        /// <summary>
        /// Backs <see cref="SurfaceAsset.IsEnabled"/>.
        /// </summary>
        byte IsEnabled { get; set; }

        /// <summary>
        /// The raw flags word backing <see cref="SurfaceAsset.TextureAnims"/>'s
        /// <see cref="SurfaceAsset.SurfaceTextureAnim.IsEnabled"/> (bit 0 for the first element, bit 1 for the
        /// second).
        /// </summary>
        /// <remarks>
        /// When disagreements with the derived value exist, this field wins during serialization.
        /// </remarks>
        SurfaceAsset.SurfaceTextureAnimFlags TextureAnimFlags { get; set; }

        /// <summary>
        /// The raw flags word backing <see cref="SurfaceAsset.Uvfxs"/>'s <see cref="SurfaceAsset.SurfaceUvfx.IsEnabled"/>
        /// (bit 0 for the first element, bit 1 for the second).
        /// </summary>
        /// <remarks>
        /// When disagreements with the derived value exist, this field wins during serialization.
        /// </remarks>
        SurfaceAsset.SurfaceUvfxFlags UvfxFlags { get; set; }
    }
}
