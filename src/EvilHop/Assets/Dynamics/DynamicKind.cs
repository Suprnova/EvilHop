using EvilHop.Common;
using System.Diagnostics.CodeAnalysis;

namespace EvilHop.Assets;

/// <summary>
/// Identifies which kind of object a <see cref="DynamicAsset"/> is.
/// </summary>
/// <remarks>
/// <para>
/// Each value is the <see cref="BKDRHash"/> of the name the game registers the kind under, given in
/// each member's summary. Members are that name in PascalCase with its separators removed, and with
/// the <c>game_object:</c> category dropped, since it groups nothing in particular.
/// </para>
/// <para>
/// A dynamic can hold any value, not only the ones named here. The <c>UnknownN</c> members are kinds
/// found in shipped archives whose names haven't been recovered.
/// </para>
/// </remarks>
public enum DynamicKind : uint
{
    /// <summary><c>Analog Deflection</c>.</summary>
    AnalogDeflection = 0x16B0A88D,
    /// <summary><c>Analog Direction</c>.</summary>
    AnalogDirection = 0xC0288F1F,
    /// <summary><c>audio:conversation</c>.</summary>
    AudioConversation = 0x3A335FCF,
    /// <summary><c>camera:binary_poi</c>.</summary>
    CameraBinaryPOI = 0xFA0E4015,
    /// <summary><c>camera:preset</c>.</summary>
    CameraPreset = 0xCDAB9190,
    /// <summary><c>camera:transition_time</c>.</summary>
    CameraTransitionTime = 0xBC304E86,
    /// <summary><c>Carrying:Carryable Object</c>.</summary>
    CarryingCarryableObject = 0x284375FD,
    /// <summary><c>Carrying:Carryable Property:Generic Use Property</c>.</summary>
    CarryingCarryablePropertyGenericUseProperty = 0x35F3B22A,
    /// <summary><c>Carrying:Carryable Property:Use Property Attract</c>.</summary>
    CarryingCarryablePropertyUsePropertyAttract = 0x45F261C6,
    /// <summary><c>Carrying:Carryable Property:Use Property Repel</c>.</summary>
    CarryingCarryablePropertyUsePropertyRepel = 0x0A21FFAD,
    /// <summary><c>Carrying:Carryable Property:Use Property Swipe</c>.</summary>
    CarryingCarryablePropertyUsePropertySwipe = 0x1E175B3F,
    /// <summary><c>Checkpoint</c>.</summary>
    Checkpoint = 0x2DE7AB98,
    /// <summary><c>Context Object:Pole Swing</c>.</summary>
    ContextObjectPoleSwing = 0xD9CA96BC,
    /// <summary><c>Context Object:Springboard</c>.</summary>
    ContextObjectSpringboard = 0x2D0D198B,
    /// <summary><c>Context Object:Tightrope</c>.</summary>
    ContextObjectTightrope = 0x105DFF22,

    /// <summary><c>effect:Flamethrower</c>.</summary>
    EffectFlamethrower = 0xFB1179F5,
    /// <summary><c>effect:grass</c>.</summary>
    EffectGrass = 0x081A3629,
    /// <summary><c>effect:Lens Flare Element</c>.</summary>
    EffectLensFlareElement = 0x2CD29541,
    /// <summary><c>effect:Lens Flare Source</c>.</summary>
    EffectLensFlareSource = 0xA072A4DA,
    /// <summary><c>effect:light</c>.</summary>
    EffectLight = 0x5EAB97E1,
    /// <summary><c>effect:Light Effect Flicker</c>.</summary>
    EffectLightEffectFlicker = 0x53CE3CA4,
    /// <summary><c>effect:Light Effect Strobe</c>.</summary>
    EffectLightEffectStrobe = 0x96727F69,
    /// <summary><c>effect:Lightning</c>.</summary>
    EffectLightning = 0x94B8EF2D,
    /// <summary><c>effect:particle_generator</c>.</summary>
    EffectParticleGenerator = 0x4AF4ABC7,
    /// <summary><c>effect:Rumble</c>.</summary>
    EffectRumble = 0x2A59443A,
    /// <summary><c>effect:Rumble Box Emitter</c>.</summary>
    EffectRumbleBoxEmitter = 0x56F5D96F,
    /// <summary><c>effect:Rumble Spherical Emitter</c>.</summary>
    EffectRumbleSphericalEmitter = 0x1337E641,
    /// <summary><c>effect:ScreenFade</c>.</summary>
    EffectScreenFade = 0x9535DB9D,
    /// <summary><c>effect:Screen Warp</c>.</summary>
    EffectScreenWarp = 0xC2783A7F,
    /// <summary><c>effect:smoke_emitter</c>.</summary>
    EffectSmokeEmitter = 0x0903FBB9,
    /// <summary><c>effect:spark_emitter</c>.</summary>
    EffectSparkEmitter = 0xA7039867,
    /// <summary><c>effect:Splash</c>.</summary>
    EffectSplash = 0xCDF6730C,
    /// <summary><c>effect:spotlight</c>.</summary>
    EffectSpotlight = 0x6AA8BF67,
    /// <summary><c>effect:uber_laser</c>.</summary>
    EffectUberLaser = 0xA866726F,
    /// <summary><c>effect:water_body</c>.</summary>
    EffectWaterBody = 0x90D4BA5B,

    /// <summary><c>Enemy:IN2:Bomber</c>.</summary>
    EnemyIN2Bomber = 0xC6C76EEE,
    /// <summary><c>Enemy:IN2:BossUnderminerDrill</c>.</summary>
    EnemyIN2BossUnderminerDrill = 0x4EE03B24,
    /// <summary><c>Enemy:IN2:BossUnderminerUM</c>.</summary>
    EnemyIN2BossUnderminerUM = 0xCDB57387,
    /// <summary><c>Enemy:IN2:Chicken</c>.</summary>
    EnemyIN2Chicken = 0x460F4FB2,
    /// <summary><c>Enemy:IN2:Driller</c>.</summary>
    EnemyIN2Driller = 0xCF21DB89,
    /// <summary><c>Enemy:IN2:Enforcer</c>.</summary>
    EnemyIN2Enforcer = 0xE5D82D97,
    /// <summary><c>Enemy:IN2:Humanoid</c>.</summary>
    EnemyIN2Humanoid = 0x2743B85C,
    /// <summary><c>Enemy:IN2:RobotTank</c>.</summary>
    EnemyIN2RobotTank = 0xAD7CB421,
    /// <summary><c>Enemy:IN2:Scientist</c>.</summary>
    EnemyIN2Scientist = 0xE2301EA9,
    /// <summary><c>Enemy:IN2:Shooter</c>.</summary>
    EnemyIN2Shooter = 0xFC2951C1,
    /// <summary><c>Enemy:NPC Gate</c>.</summary>
    EnemyNPCGate = 0x175ED698,
    /// <summary><c>Enemy:NPC Walls</c>.</summary>
    EnemyNPCWalls = 0x0E612078,
    /// <summary><c>Enemy:RATS:LeftArm</c>.</summary>
    EnemyRATSLeftArm = 0xB34B0083,
    /// <summary><c>Enemy:RATS:RightArm</c>.</summary>
    EnemyRATSRightArm = 0x89F5441A,
    /// <summary><c>Enemy:RATS:Swarm:Bug</c>.</summary>
    EnemyRATSSwarmBug = 0x544AA34C,
    /// <summary><c>Enemy:RATS:Swarm:Owl</c>.</summary>
    EnemyRATSSwarmOwl = 0x544E0BCC,
    /// <summary><c>Enemy:RATS:Thief</c>.</summary>
    EnemyRATSThief = 0xEF5FD10C,
    /// <summary><c>Enemy:RATS:Waiter</c>.</summary>
    EnemyRATSWaiter = 0xF5B8CC9C,
    /// <summary><c>Enemy:SB:BucketOTron</c>.</summary>
    EnemySBBucketOTron = 0xD2D6A1E5,
    /// <summary><c>Enemy:SB:CastNCrew</c>.</summary>
    EnemySBCastNCrew = 0x1F9D54BB,
    /// <summary><c>Enemy:SB:Critter</c>.</summary>
    EnemySBCritter = 0x45B73B62,
    /// <summary><c>Enemy:SB:Dennis</c>.</summary>
    EnemySBDennis = 0xCE41C144,
    /// <summary><c>Enemy:SB:FrogFish</c>.</summary>
    EnemySBFrogFish = 0x11FCF451,
    /// <summary><c>Enemy:SB:Mindy</c>.</summary>
    EnemySBMindy = 0xC92170B2,
    /// <summary><c>Enemy:SB:Neptune</c>.</summary>
    EnemySBNeptune = 0xBE8C5CAC,
    /// <summary><c>Enemy:SB:Standard</c>.</summary>
    EnemySBStandard = 0x44EA147A,
    /// <summary><c>Enemy:SB:SupplyCrate</c>.</summary>
    EnemySBSupplyCrate = 0x495BFF9B,
    /// <summary><c>Enemy:SB:Turret</c>.</summary>
    EnemySBTurret = 0x9FEC1E09,

    /// <summary><c>game_object:BoulderGenerator</c>.</summary>
    BoulderGenerator = 0xBB4864D8,
    /// <summary><c>game_object:bullet_mark</c>.</summary>
    BulletMark = 0x381232B4,
    /// <summary><c>game_object:bullet_time</c>.</summary>
    BulletTime = 0x390467A4,
    /// <summary><c>game_object:bungee_drop</c>.</summary>
    BungeeDrop = 0x574749A4,
    /// <summary><c>game_object:bungee_hook</c>.</summary>
    BungeeHook = 0x57CFB6F0,
    /// <summary><c>game_object:BusStop</c>.</summary>
    BusStop = 0x8F012778,
    /// <summary><c>game_object:camera_param_asset</c>.</summary>
    CameraParamAsset = 0xE44DCEBA,
    /// <summary><c>game_object:Camera_Tweak</c>.</summary>
    CameraTweak = 0x9092FB14,
    /// <summary><c>game_object:dash_camera_spline</c>.</summary>
    DashCameraSpline = 0x571A5DBC,
    /// <summary><c>game_object:flame_emitter</c>.</summary>
    FlameEmitter = 0xE6120704,
    /// <summary><c>game_object:Flythrough</c>.</summary>
    Flythrough = 0x85BFDF34,
    /// <summary><c>game_object:FreezableObject</c>.</summary>
    FreezableObject = 0x35D19631,
    /// <summary><c>game_object:Grapple</c>.</summary>
    Grapple = 0xE7928821,
    /// <summary><c>game_object:Hangable</c>.</summary>
    Hangable = 0x1D3C54EE,
    /// <summary><c>game_object:IN_Pickup</c>.</summary>
    INPickup = 0x832E4208,
    /// <summary><c>game_object:laser_beam</c>.</summary>
    LaserBeam = 0xBBCB17C1,
    /// <summary><c>game_object:NPCSettings</c>.</summary>
    NPCSettings = 0x8768334A,
    /// <summary><c>game_object:RaceTimer</c>.</summary>
    RaceTimer = 0x844BCF76,
    /// <summary><c>game_object:rband_camera_asset</c>.</summary>
    RbandCameraAsset = 0x945F2E84,
    /// <summary><c>game_object:Ring</c>.</summary>
    Ring = 0x4D81C1EE,
    /// <summary><c>game_object:RingControl</c>.</summary>
    RingControl = 0x18028CA7,
    /// <summary><c>game_object:RubbleGenerator</c>.</summary>
    RubbleGenerator = 0x3D0D5121,
    /// <summary><c>game_object:talk_box</c>.</summary>
    TalkBox = 0x0934B196,
    /// <summary><c>game_object:task_box</c>.</summary>
    TaskBox = 0xE9D2C1BB,
    /// <summary><c>game_object:Taxi</c>.</summary>
    Taxi = 0x4DC449FC,
    /// <summary><c>game_object:Teleport</c>.</summary>
    Teleport = 0x70ADB7F9,
    /// <summary><c>game_object:text_box</c>.</summary>
    TextBox = 0x442E1337,
    /// <summary><c>game_object:train_car</c>.</summary>
    TrainCar = 0xC279D693,
    /// <summary><c>game_object:train_junction</c>.</summary>
    TrainJunction = 0xEA7B28D9,
    /// <summary><c>game_object:Turret</c>.</summary>
    Turret = 0x798A7982,
    /// <summary><c>game_object:Vent</c>.</summary>
    Vent = 0x4E09EC43,
    /// <summary><c>game_object:VentType</c>.</summary>
    VentType = 0x5E5B5165,

    /// <summary><c>hud:image</c>.</summary>
    HUDImage = 0xB8DA553C,
    /// <summary><c>hud:meter:font</c>.</summary>
    HUDMeterFont = 0x8B3E732F,
    /// <summary><c>hud:meter:unit</c>.</summary>
    HUDMeterUnit = 0x8D40B9AC,
    /// <summary><c>hud:model</c>.</summary>
    HUDModel = 0xFF5691D2,
    /// <summary><c>hud:text</c>.</summary>
    HUDText = 0x687ED0B0,
    /// <summary><c>HUD_Compass_Object</c>.</summary>
    HUDCompassObject = 0x50B5E94C,
    /// <summary><c>HUD_Compass_System</c>.</summary>
    HUDCompassSystem = 0xD3BB2158,
    /// <summary><c>Incredibles:Icon</c>.</summary>
    IncrediblesIcon = 0xD6093241,
    /// <summary><c>interaction:Launch</c>.</summary>
    InteractionLaunch = 0x4B03B4F7,
    /// <summary><c>interaction:Lift</c>.</summary>
    InteractionLift = 0x4C1F2B57,
    /// <summary><c>interaction:SwitchLever</c>.</summary>
    InteractionSwitchLever = 0x28478E46,
    /// <summary><c>interaction:Turn</c>.</summary>
    InteractionTurn = 0x4D34C2B9,
    /// <summary><c>Interest_Pointer</c>.</summary>
    InterestPointer = 0x1F662B3C,
    /// <summary><c>JSP Extra Data</c>.</summary>
    JSPExtraData = 0x204D6ADB,
    /// <summary><c>logic:Function Generator</c>.</summary>
    LogicFunctionGenerator = 0x4494F483,
    /// <summary><c>logic:Mission</c>.</summary>
    LogicMission = 0x890EB71C,
    /// <summary><c>logic:reference</c>.</summary>
    LogicReference = 0xF98698FF,
    /// <summary><c>logic:Task</c>.</summary>
    LogicTask = 0x1D40CE5D,
    /// <summary><c>npc:CoverPoint</c>.</summary>
    NPCCoverPoint = 0x48C0D3A6,
    /// <summary><c>npc:group</c>.</summary>
    NPCGroup = 0x2326640A,
    /// <summary><c>npc:NPC_Custom_AV</c>.</summary>
    NPCNPCCustomAV = 0xFF7E4CFC,
    /// <summary><c>pointer</c>.</summary>
    [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Mirrors the game's own name for this kind.")]
    Pointer = 0x2196C135,
    /// <summary><c>Pour Widget</c>.</summary>
    PourWidget = 0x2DDFA8F4,
    /// <summary><c>Twiddler</c>.</summary>
    Twiddler = 0x01A49323,

    /// <summary><c>ui:box</c>.</summary>
    UIBox = 0x8C2D107D,
    /// <summary><c>ui:controller</c>.</summary>
    UIController = 0xE8753BAE,
    /// <summary><c>ui:image</c>.</summary>
    UIImage = 0x337BCB31,
    /// <summary><c>ui:model</c>.</summary>
    UIModel = 0x79F807C7,
    /// <summary><c>ui:text</c>.</summary>
    UIText = 0xBD7646D7,
    /// <summary><c>ui:text:user string</c>.</summary>
    UITextUserString = 0xFB50BACB,

    /// <summary>Unknown. Found in every game from TSSM on.</summary>
    Unknown1 = 0xFABDB3B3,
    /// <summary>Unknown. Found in Incredibles and ROTU.</summary>
    Unknown2 = 0xEBC04E7B,
    /// <summary>Unknown. Found in ROTU.</summary>
    Unknown3 = 0x9F234F8E,
    /// <summary>Unknown. Found in ROTU.</summary>
    Unknown4 = 0xF7E8697A,
    /// <summary>Unknown. Found in ROTU.</summary>
    Unknown5 = 0xDEC6DFF0,
}
