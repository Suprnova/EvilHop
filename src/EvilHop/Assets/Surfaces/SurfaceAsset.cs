using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
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
        set => Physical.GameDamageFlags = Physical.GameDamageFlags.WithFlag(DamageBehavior.DamagePassthrough, value);
    }

    /// <summary>
    /// The time, in seconds, the player is immune to this surface's damage after being hit by it.
    /// Further contact within the window neither damages nor knocks the player back. 0 defers to
    /// the game ini's <c>G.DamageTimeSurface</c>.
    /// </summary>
    /// <remarks>
    /// Not present in <see cref="GameVersion.N100F"/>.
    /// </remarks>
    public float DamageTimer { get; set; }

    /// <summary>
    /// The vertical velocity applied to the player as knockback when this surface damages them. 0
    /// defers to the game ini's <c>G.DamageSurfKnock</c>.
    /// </summary>
    /// <remarks>
    /// Not present in <see cref="GameVersion.N100F"/>.
    /// </remarks>
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
    /// -1 defers to the game ini's <c>player.state.out_of_bounds.out_time</c>. Not present in
    /// <see cref="GameVersion.N100F"/>.
    /// </remarks>
    public float OutOfBoundsDelay { get; set; }

    /// <summary>
    /// Scales the player's horizontal wall-jump velocity off this surface. Applies only when
    /// <see cref="PhysicsBehavior.WallJump"/> is set.
    /// </summary>
    /// <remarks>
    /// Not present in <see cref="GameVersion.N100F"/>.
    /// </remarks>
    public float WallJumpScaleXZ { get; set; }

    /// <summary>
    /// Scales the player's vertical wall-jump velocity off this surface. Applies only when
    /// <see cref="PhysicsBehavior.WallJump"/> is set.
    /// </summary>
    /// <remarks>
    /// Not present in <see cref="GameVersion.N100F"/>.
    /// </remarks>
    public float WallJumpScaleY { get; set; }

    /// <summary>
    /// The damage the player takes on contact with this surface; 0 or less deals none.
    /// </summary>
    /// <remarks>
    /// Only present in <see cref="GameVersion.TSSM"/>, <see cref="GameVersion.Incredibles"/>,
    /// <see cref="GameVersion.ROTU"/>, and <see cref="GameVersion.Ratatouille"/>. Ignored by
    /// <see cref="GameVersion.TSSM"/>.
    /// </remarks>
    public int DamageAmount { get; set; }

    /// <summary>
    /// The hit source the player's <see cref="DamageAmount"/> damage is dealt as.
    /// </summary>
    /// <remarks>
    /// A value of the game's own <c>zHitSource</c> enumeration, which differs per game. Only present
    /// in <see cref="GameVersion.TSSM"/>, <see cref="GameVersion.Incredibles"/>,
    /// <see cref="GameVersion.ROTU"/>, and <see cref="GameVersion.Ratatouille"/>. Ignored by
    /// <see cref="GameVersion.TSSM"/>.
    /// </remarks>
    public int DamageSource { get; set; }

    /// <summary>
    /// The footstep effects of a player walking on this surface.
    /// </summary>
    /// <remarks>
    /// Only present in <see cref="GameVersion.TSSM"/>, <see cref="GameVersion.Incredibles"/>,
    /// <see cref="GameVersion.ROTU"/>, and <see cref="GameVersion.Ratatouille"/>. Ignored by
    /// <see cref="GameVersion.TSSM"/>.
    /// </remarks>
    public FootstepEffect OnSurfaceFootsteps { get; set; } = new();

    /// <summary>
    /// The footstep effects of a player who has just left this surface, for
    /// <see cref="OffSurfaceTime"/> seconds.
    /// </summary>
    /// <remarks>
    /// Only present in <see cref="GameVersion.TSSM"/>, <see cref="GameVersion.Incredibles"/>,
    /// <see cref="GameVersion.ROTU"/>, and <see cref="GameVersion.Ratatouille"/>. Ignored by
    /// <see cref="GameVersion.TSSM"/>.
    /// </remarks>
    public FootstepEffect OffSurfaceFootsteps { get; set; } = new();

    /// <summary>
    /// The time, in seconds, <see cref="OffSurfaceFootsteps"/> stays in effect after the player
    /// leaves this surface; 0 or less disables it.
    /// </summary>
    /// <remarks>
    /// Only present in <see cref="GameVersion.TSSM"/>, <see cref="GameVersion.Incredibles"/>,
    /// <see cref="GameVersion.ROTU"/>, and <see cref="GameVersion.Ratatouille"/>. Ignored by
    /// <see cref="GameVersion.TSSM"/>.
    /// </remarks>
    public float OffSurfaceTime { get; set; }

    /// <summary>
    /// Whether this surface is water the player swims in.
    /// </summary>
    /// <remarks>
    /// Projected onto <see cref="Physical.ISurfaceAsset.Swimmable"/>. Only present in
    /// <see cref="GameVersion.TSSM"/>, <see cref="GameVersion.Incredibles"/>,
    /// <see cref="GameVersion.ROTU"/>, and <see cref="GameVersion.Ratatouille"/>. Ignored by
    /// <see cref="GameVersion.TSSM"/>.
    /// </remarks>
    public bool IsSwimmable
    {
        get => Physical.Swimmable != 0;
        set => Physical.Swimmable = (byte)(value ? 1 : 0);
    }

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

    private AssetId _impactSound;
    AssetId Physical.ISurfaceAsset.ImpactSound { get => _impactSound; set => _impactSound = value; }

    private byte _dashImpactType = 0xFF;
    byte Physical.ISurfaceAsset.DashImpactType { get => _dashImpactType; set => _dashImpactType = value; }

    private float _dashImpactThrowBack = 5f;
    float Physical.ISurfaceAsset.DashImpactThrowBack { get => _dashImpactThrowBack; set => _dashImpactThrowBack = value; }

    private float _dashSprayMagnitude = 1f;
    float Physical.ISurfaceAsset.DashSprayMagnitude { get => _dashSprayMagnitude; set => _dashSprayMagnitude = value; }

    private float _dashCoolRate = 0.05f;
    float Physical.ISurfaceAsset.DashCoolRate { get => _dashCoolRate; set => _dashCoolRate = value; }

    private float _dashCoolAmount = 0.3f;
    float Physical.ISurfaceAsset.DashCoolAmount { get => _dashCoolAmount; set => _dashCoolAmount = value; }

    private float _dashPass;
    float Physical.ISurfaceAsset.DashPass { get => _dashPass; set => _dashPass = value; }

    private float _dashRampMaxDistance = 30f;
    float Physical.ISurfaceAsset.DashRampMaxDistance { get => _dashRampMaxDistance; set => _dashRampMaxDistance = value; }

    private float _dashRampMinDistance = 20f;
    float Physical.ISurfaceAsset.DashRampMinDistance { get => _dashRampMinDistance; set => _dashRampMinDistance = value; }

    private float _dashRampKeySpeed = 25f;
    float Physical.ISurfaceAsset.DashRampKeySpeed { get => _dashRampKeySpeed; set => _dashRampKeySpeed = value; }

    private float _dashRampHeight = 10f;
    float Physical.ISurfaceAsset.DashRampHeight { get => _dashRampHeight; set => _dashRampHeight = value; }

    private AssetId _dashRampTarget;
    AssetId Physical.ISurfaceAsset.DashRampTarget { get => _dashRampTarget; set => _dashRampTarget = value; }

    private ImmutableArray<HitDecal> _hitDecals = [default, default, default];
    ImmutableArray<HitDecal> Physical.ISurfaceAsset.HitDecals
    {
        get => _hitDecals;
        set => _hitDecals = value.Length == 3
            ? value
            : throw new ArgumentException("HitDecals must contain exactly 3 elements.", nameof(value));
    }

    private byte _swimmable;
    byte Physical.ISurfaceAsset.Swimmable { get => _swimmable; set => _swimmable = value; }

    private byte _dashFall;
    byte Physical.ISurfaceAsset.DashFall { get => _dashFall; set => _dashFall = value; }

    private byte _needButtonPress;
    byte Physical.ISurfaceAsset.NeedButtonPress { get => _needButtonPress; set => _needButtonPress = value; }

    private byte _dashAttach;
    byte Physical.ISurfaceAsset.DashAttach { get => _dashAttach; set => _dashAttach = value; }

    private byte _footstepDecals;
    byte Physical.ISurfaceAsset.FootstepDecals { get => _footstepDecals; set => _footstepDecals = value; }

    private byte _drivingSurfaceType;
    byte Physical.ISurfaceAsset.DrivingSurfaceType { get => _drivingSurfaceType; set => _drivingSurfaceType = value; }

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
    internal static IReadOnlySet<GameVersion> SupportedGames { get; } = new HashSet<GameVersion>
    {
        GameVersion.N100F,
        GameVersion.BFBB,
        GameVersion.TSSM,
        GameVersion.Incredibles,
        GameVersion.ROTU,
        GameVersion.Ratatouille,
    };

    /// <summary>
    /// One of a <see cref="SurfaceAsset"/>'s three hit decals.
    /// </summary>
    /// <param name="Texture">Unknown.</param>
    /// <param name="XSize">Unknown.</param>
    /// <param name="YSize">Unknown.</param>
    public readonly record struct HitDecal(AssetId Texture, float XSize, float YSize)
    {
        internal static HitDecal Read(EndianReader reader, FormatProfile _) =>
            new(reader.ReadAssetId(), reader.ReadSingle(), reader.ReadSingle());

        internal static void Write(HitDecal decal, EndianWriter writer, FormatProfile _)
        {
            writer.Write(decal.Texture);
            writer.Write(decal.XSize);
            writer.Write(decal.YSize);
        }
    }

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

        /// <summary>
        /// Unknown.
        /// </summary>
        /// <remarks>
        /// Only present in <see cref="GameVersion.TSSM"/>, <see cref="GameVersion.Incredibles"/>,
        /// <see cref="GameVersion.ROTU"/>, and <see cref="GameVersion.Ratatouille"/>.
        /// </remarks>
        AssetId ImpactSound { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        /// <remarks>
        /// Read by <see cref="GameVersion.Incredibles"/>' Dash levels when Dash trips or hits a
        /// wall on this surface. Only present in <see cref="GameVersion.TSSM"/>,
        /// <see cref="GameVersion.Incredibles"/>, <see cref="GameVersion.ROTU"/>, and
        /// <see cref="GameVersion.Ratatouille"/>.
        /// </remarks>
        byte DashImpactType { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        float DashImpactThrowBack { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        float DashSprayMagnitude { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        float DashCoolRate { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        float DashCoolAmount { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        float DashPass { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        float DashRampMaxDistance { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        float DashRampMinDistance { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        float DashRampKeySpeed { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        float DashRampHeight { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        AssetId DashRampTarget { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        /// <remarks>
        /// Only present in <see cref="GameVersion.TSSM"/>, <see cref="GameVersion.Incredibles"/>,
        /// <see cref="GameVersion.ROTU"/>, and <see cref="GameVersion.Ratatouille"/>.
        /// </remarks>
        /// <exception cref="ArgumentException">The assigned value's length isn't 3.</exception>
        ImmutableArray<SurfaceAsset.HitDecal> HitDecals { get; set; }

        /// <summary>
        /// Backs <see cref="SurfaceAsset.IsSwimmable"/>.
        /// </summary>
        byte Swimmable { get; set; }

        /// <summary>
        /// Whether <see cref="GameVersion.Incredibles"/>' Dash falls through this surface rather
        /// than colliding with it.
        /// </summary>
        /// <remarks>
        /// Only present in <see cref="GameVersion.TSSM"/>, <see cref="GameVersion.Incredibles"/>,
        /// <see cref="GameVersion.ROTU"/>, and <see cref="GameVersion.Ratatouille"/>. Ignored by
        /// every game but <see cref="GameVersion.Incredibles"/>.
        /// </remarks>
        byte DashFall { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        byte NeedButtonPress { get; set; }

        /// <summary>
        /// Unknown.
        /// </summary>
        /// <remarks>
        /// Read by <see cref="GameVersion.Incredibles"/>' Dash levels to decide whether Dash stays
        /// attached to this surface's floor. Only present in <see cref="GameVersion.TSSM"/>,
        /// <see cref="GameVersion.Incredibles"/>, <see cref="GameVersion.ROTU"/>, and
        /// <see cref="GameVersion.Ratatouille"/>.
        /// </remarks>
        byte DashAttach { get; set; }

        /// <inheritdoc cref="ImpactSound"/>
        byte FootstepDecals { get; set; }

        /// <summary>
        /// Which kind of ground this surface is to <see cref="GameVersion.TSSM"/>'s driveable car and
        /// the player's slide effects.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The car records every value it touches; 11 damages it, as does a
        /// <see cref="SurfaceAsset.Damage"/> of <see cref="SurfaceAsset.DamageKind.Damage6"/>, and
        /// 13 makes it rebound. While the player slides, 14 plays a slide sound and 15 to 17 emit
        /// slide dust. The meaning of every other value is unknown.
        /// </para>
        /// <para>
        /// Only present in <see cref="GameVersion.TSSM"/>, <see cref="GameVersion.Incredibles"/>,
        /// <see cref="GameVersion.ROTU"/>, and <see cref="GameVersion.Ratatouille"/>. Ignored by
        /// every game but <see cref="GameVersion.TSSM"/>.
        /// </para>
        /// </remarks>
        byte DrivingSurfaceType { get; set; }
    }
}
