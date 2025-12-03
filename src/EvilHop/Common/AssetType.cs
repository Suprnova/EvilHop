namespace EvilHop.Common;

/// <summary>
/// Specifies the asset type of supported assets.
/// </summary>
/// <remarks>
/// Backed by a uint field that maps to the second field, <c>type</c>, in <see cref="Blocks.AssetHeader"/> blocks.
/// </remarks>
public enum AssetType : uint
{
    Animation = 0x414E494D,
    AnimationList = 0x414C5354,
    AnimationTable = 0x4154424C,
    BSP = 0x42535020,
    Button = 0x4255544E,
    Camera = 0x43414D20,
    Conditional = 0x434F4E44,
    Counter = 0x434E5452,
    Cutscene = 0x43534E20,
    CutsceneManager = 0x43534E4D,
    CutsceneTable = 0x43544F43,
    DestructibleObject = 0x44535452,
    Dispatcher = 0x44504154,
    ElectricArcGenerator = 0x4547454E,
    Environment = 0x454E5620,
    Fog = 0x464F4720,
    Group = 0x47525550,
    Gust = 0X47555354,
    Hangable = 0x48414E47,
    Light = 0x4C495445,
    LobMaster = 0x4C4F424D,
    Marker = 0x4D524B52,
    Model = 0x4D4F444C,
    ModelInfo = 0x4D494E46,
    MorphTarget = 0x4D504854,
    MovePoint = 0x4D565054,
    NPC = 0x4E504320,
    ParticleEmitter = 0x50415245,
    ParticleSystem = 0x50415253,
    Pendulum = 0x50454E44,
    Pickup = 0x504B5550,
    PickupTable = 0x5049434B,
    Platform = 0x504C4154,
    Player = 0x504C5952,
    Portal = 0x504F5254,
    Projectile = 0x50524A54,
    Script = 0x53435250,
    SoundFX = 0x53465820,
    SimpleObject = 0x53494D50,
    Sound = 0x534E4420,
    SoundInfo = 0x534E4449,
    StreamingSound = 0x534E4453,
    Surface = 0x53555246,
    SurfaceMapper = 0x4D415052,
    Text = 0x54455854,
    Texture = 0x52575458,
    Timer = 0x54494D52,
    Trigger = 0x54524947,
    UI = 0x55492020,
    UIFont = 0x55494654,
    Volume = 0x564F4C55,
    Unknown = 0x00000000
}
