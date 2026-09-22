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
    public DamageKind Damage { get; set; }

    /// <summary>
    /// Whether the player passes through this surface instead of colliding with it, while still
    /// taking its <see cref="Damage"/> damage on contact.
    /// </summary>
    public bool DamagePassthrough
    {
        get => Physical.GameDamageFlags.HasFlag(DamageBehavior.DamagePassthrough);
        set => Physical.GameDamageFlags = value
            ? Physical.GameDamageFlags | DamageBehavior.DamagePassthrough
            : Physical.GameDamageFlags & ~DamageBehavior.DamagePassthrough;
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
    public PhysicsBehavior PhysFlags { get; set; }

    /// <summary>
    /// This surface's friction, from 0 to 1.
    /// </summary>
    public float Friction { get; set; }

    /// <summary>
    /// This surface's material appearance (bump/environment mapping, shininess).
    /// </summary>
    public MaterialEffect MaterialFx { get; set; } = new();

    /// <summary>
    /// This surface's color animation.
    /// </summary>
    public ColorEffect ColorFx { get; set; } = new();

    private ImmutableArray<TextureEffect> _textureAnims = DefaultTextureAnims();

    /// <summary>
    /// This surface's two independent texture animations. Each element's own
    /// <see cref="TextureEffect.IsEnabled"/> says whether it is active.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 2.</exception>
    public ImmutableArray<TextureEffect> TextureAnims
    {
        get => _textureAnims;
        set => _textureAnims = value.Length == 2
            ? value
            : throw new ArgumentException($"{nameof(TextureAnims)} must contain exactly 2 elements.", nameof(value));
    }

    private ImmutableArray<UVEffect> _uvfxs = DefaultUvfxs();

    /// <summary>
    /// This surface's two independent UV animations. Each element's own
    /// <see cref="UVEffect.IsEnabled"/> says whether it is active.
    /// </summary>
    /// <exception cref="ArgumentException">The assigned value's length isn't 2.</exception>
    public ImmutableArray<UVEffect> Uvfxs
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
    /// Only takes effect when <see cref="PhysicsBehavior.OutOfBounds"/> is set.
    /// </summary>
    /// <remarks>
    /// -1 defers to the game ini's <c>player.state.out_of_bounds.out_time</c>.
    /// </remarks>
    public float OutOfBoundsDelay { get; set; }

    /// <summary>
    /// Scales the player's horizontal wall-jump velocity off this surface. Applies only when
    /// <see cref="PhysicsBehavior.WallJump"/> is set.
    /// </summary>
    public float WallJumpScaleXZ { get; set; }

    /// <summary>
    /// Scales the player's vertical wall-jump velocity off this surface. Applies only when
    /// <see cref="PhysicsBehavior.WallJump"/> is set.
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

    private DamageBehavior _gameDamageFlags;
    DamageBehavior Physical.ISurfaceAsset.GameDamageFlags { get => _gameDamageFlags; set => _gameDamageFlags = value; }

    private byte _surfType;
    byte Physical.ISurfaceAsset.SurfType { get => _surfType; set => _surfType = value; }

    private byte _gameSticky;
    byte Physical.ISurfaceAsset.GameSticky { get => _gameSticky; set => _gameSticky = value; }

    private byte _isEnabled = 1;
    byte Physical.ISurfaceAsset.IsEnabled { get => _isEnabled; set => _isEnabled = value; }

    private AnimationSlot? _overriddenTextureAnimFlags;
    AnimationSlot Physical.ISurfaceAsset.TextureAnimFlags
    {
        get => _overriddenTextureAnimFlags ?? DerivedTextureAnimFlags;
        set => _overriddenTextureAnimFlags = value == DerivedTextureAnimFlags ? null : value;
    }

    private AnimationSlot DerivedTextureAnimFlags =>
        (TextureAnims[0].IsEnabled ? AnimationSlot.Slot0 : AnimationSlot.None) |
        (TextureAnims[1].IsEnabled ? AnimationSlot.Slot1 : AnimationSlot.None);

    private UVSlot? _overriddenUvfxFlags;
    UVSlot Physical.ISurfaceAsset.UvfxFlags
    {
        get => _overriddenUvfxFlags ?? DerivedUvfxFlags;
        set => _overriddenUvfxFlags = value == DerivedUvfxFlags ? null : value;
    }

    private UVSlot DerivedUvfxFlags =>
        (Uvfxs[0].IsEnabled ? UVSlot.Slot0 : UVSlot.None) |
        (Uvfxs[1].IsEnabled ? UVSlot.Slot1 : UVSlot.None);

    private static ImmutableArray<TextureEffect> DefaultTextureAnims() => [new(), new()];
    private static ImmutableArray<UVEffect> DefaultUvfxs() => [new(), new()];

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
    public enum DamageBehavior : byte
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,
        /// <summary>
        /// The player passes through this surface instead of colliding with it, while still taking its
        /// <see cref="Damage"/> damage on contact.
        /// </summary>
        DamagePassthrough = 1 << 0,
    }

    /// <summary>
    /// Flags governing player physics and mobility interactions with a surface.
    /// </summary>
    [Flags]
    public enum PhysicsBehavior : byte
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,
        /// <summary>
        /// The player slides off this surface, per <see cref="SlideStartAngle"/>/
        /// <see cref="SlideStopAngle"/>.
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
        /// <see cref="OutOfBoundsDelay"/>.
        /// </summary>
        OutOfBounds = 1 << 4,
        /// <summary>
        /// The player can wall jump off this surface. Required for
        /// <see cref="WallJumpScaleXZ"/> and <see cref="WallJumpScaleY"/> to
        /// apply - without it the move does not trigger at all.
        /// </summary>
        WallJump = 1 << 5,
    }

    /// <summary>
    /// Flags governing active texture animations on a surface.
    /// </summary>
    [Flags]
    public enum AnimationSlot : uint
    {
        /// <summary>
        /// Neither texture animation is active.
        /// </summary>
        None = 0,

        /// <summary>
        /// The first texture animation (<see cref="TextureAnims"/>[0]) is active.
        /// </summary>
        Slot0 = 1 << 0,

        /// <summary>
        /// The second texture animation (<see cref="TextureAnims"/>[1]) is active.
        /// </summary>
        Slot1 = 1 << 1,
    }

    /// <summary>
    /// Flags governing active UV coordinate animation effects on a surface.
    /// </summary>
    [Flags]
    public enum UVSlot : uint
    {
        /// <summary>
        /// Neither UV animation is active.
        /// </summary>
        None = 0,

        /// <summary>
        /// The first UV animation (<see cref="Uvfxs"/>[0]) is active.
        /// </summary>
        Slot0 = 1 << 0,

        /// <summary>
        /// The second UV animation (<see cref="Uvfxs"/>[1]) is active.
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
        /// <see cref="SurfaceAsset.DamageBehavior.DamagePassthrough"/> is exposed logically as
        /// <see cref="SurfaceAsset.DamagePassthrough"/>.
        /// </remarks>
        SurfaceAsset.DamageBehavior GameDamageFlags { get; set; }

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
        /// <see cref="SurfaceAsset.TextureEffect.IsEnabled"/> (bit 0 for the first element, bit 1 for the
        /// second).
        /// </summary>
        /// <remarks>
        /// When disagreements with the derived value exist, this field wins during serialization.
        /// </remarks>
        SurfaceAsset.AnimationSlot TextureAnimFlags { get; set; }

        /// <summary>
        /// The raw flags word backing <see cref="SurfaceAsset.Uvfxs"/>'s <see cref="SurfaceAsset.UVEffect.IsEnabled"/>
        /// (bit 0 for the first element, bit 1 for the second).
        /// </summary>
        /// <remarks>
        /// When disagreements with the derived value exist, this field wins during serialization.
        /// </remarks>
        SurfaceAsset.UVSlot UvfxFlags { get; set; }
    }
}
