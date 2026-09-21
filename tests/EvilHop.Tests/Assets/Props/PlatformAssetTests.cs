using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class PlatformAssetTests
{
    private readonly PlatformAsset platform;

    public PlatformAssetTests()
    {
        platform = new PlatformAsset { Motion = new ExtendRetractMotion() };
    }

    private static FormatProfile ProfileFor(GameVersion game) => game switch
    {
        GameVersion.N100F => N100FSerializer.DefaultProfile,
        GameVersion.BFBB => BFBBSerializer.DefaultProfile,
        GameVersion.TSSM => TSSMSerializer.DefaultProfile,
        GameVersion.Incredibles => IncrediblesSerializer.DefaultProfile,
        GameVersion.ROTU => ROTUSerializer.DefaultProfile,
        GameVersion.Ratatouille => RatatouilleSerializer.DefaultProfile,
        _ => throw new ArgumentOutOfRangeException(nameof(game)),
    };

    private static Asset Read(GameVersion game, byte[] data)
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();
        header.Type = AssetType.Platform;
        header.Debug = debug;

        var profile = ProfileFor(game);
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(GameVersion game, Asset asset)
    {
        var profile = ProfileFor(game);
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static bool PostBFBB(GameVersion game) => game is not (GameVersion.N100F or GameVersion.BFBB);

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Floats(params float[] values) => [.. values.SelectMany(F32)];
    private static byte[] Vec3(float x, float y, float z) => Floats(x, y, z);

    private static byte[] MotionHeader(byte type, ushort flags, byte useBanking = 0) =>
        [type, useBanking, (byte)(flags >> 8), (byte)flags];

    private static byte[] LinkBytes(uint destinationAssetId) =>
    [
        0x00, 0x0A, 0x00, 0x0B, // SourceEvent, DestinationEvent
        .. U32(destinationAssetId),
        .. new byte[24],        // Params, ParamWidgetAssetId, CheckAssetId
    ];

    private static byte[] Padded(int size, byte[] fields) => [.. fields, .. new byte[size - fields.Length]];

    private static byte[] Platform(GameVersion game, byte type, byte subtype, byte[] typeBlock, byte[] motionBlock, byte linkCount = 0) =>
    [
        0x00, 0x00, 0x12, 0x34,     // BaseId
        0x06,                       // BaseType
        linkCount,
        0x00, 0x1D,                 // BaseFlags
        0x01, subtype, 0x00, 0x02,  // EntityFlags, Subtype, PFlags, CollisionFlags
        .. game is GameVersion.BFBB ? new byte[4] : [],
        .. U32(0xAAAA0001),         // SurfaceId
        .. new byte[36],            // Angle/Position/Scale
        .. new byte[16],            // ColorMultiplier
        .. F32(255f),               // SeeThroughSpeed
        .. U32(0xAAAA0002),         // ModelId
        .. U32(0xAAAA0003),         // AnimListId
        type, 0x00, 0x00, 0x04,     // PlatformType, padding, Flags
        .. Padded(game is GameVersion.N100F ? 0x24 : 0x38, typeBlock),
        .. Padded(PostBFBB(game) ? 0x3C : 0x30, motionBlock),
        .. Enumerable.Range(0, linkCount).SelectMany(i => LinkBytes(0xBBBB0000 + (uint)i)),
    ];

    private static readonly byte[] ExtendRetractBlock =
        [.. MotionHeader(0, 0x0004), .. Vec3(1, 2, 3), .. Vec3(0, 5, 0), .. Floats(1, 2, 3, 4)];

    private static readonly byte[] OrbitBlock = [.. MotionHeader(1, 0), .. Vec3(10, 0, 10), .. Floats(4, 6, 8)];

    private static readonly byte[] EmptyMotionBlock = MotionHeader(6, 0x0004);

    private static byte[] ExtendRetract(GameVersion game) => Platform(game, 0, 0, [], ExtendRetractBlock);

    private static byte[] Orbit(GameVersion game) => Platform(game, 1, 0, [], OrbitBlock);

    private static byte[] Spline(GameVersion game) => Platform(game, 2, 0, [],
        [.. MotionHeader(2, 0x0005), .. U32(0xCCCC0001), .. PostBFBB(game) ? F32(12) : []]);

    private static byte[] MovePoint(GameVersion game) => Platform(game, 3, 0, [],
        [.. MotionHeader(3, 0x0001, useBanking: 1), .. U32(1), .. U32(0xCCCC0002), .. F32(5)]);

    private static byte[] Mechanism(GameVersion game) => Platform(game, 4, 4, [],
    [
        .. MotionHeader(4, 0x0004),
        0x01, 0x01, 0x00, 0x02,     // Movement, MechanismFlags, SlideAxis, RotateAxis
        .. PostBFBB(game) ? new byte[] { 0x06, 0x00, 0x00, 0x00 } : [], // ScaleAxis, padding
        .. Floats(1, 2, 3, 4, 5, 6, 7, 8, 9, 10),
        .. PostBFBB(game) ? Floats(11, 12) : [],
    ]);

    private static byte[] Pendulum(GameVersion game) => Platform(game, 5, 5, [],
        [.. MotionHeader(5, 0), 0x01, 0x02, 0x00, 0x00, .. Floats(3, 0.5f, 2, 1)]);

    private static byte[] ConveyorBelt(GameVersion game) => Platform(game, 6, 6, F32(4), EmptyMotionBlock);

    private static byte[] Falling(GameVersion game) => Platform(game, 7, 7, [.. F32(3), .. U32(0xCCCC0003)], EmptyMotionBlock);

    private static byte[] ForwardReturn(GameVersion game) => Platform(game, 8, 8, Floats(1, 2, 3, 4), EmptyMotionBlock);

    private static byte[] Breakaway(GameVersion game)
    {
        byte[] block = game switch
        {
            GameVersion.N100F => [.. F32(1.5f), .. U32(0xCE7F8131), .. F32(3)],
            GameVersion.BFBB => [.. F32(1.5f), .. U32(0xCE7F8131), .. F32(3), .. U32(1)],
            _ => [.. F32(1.5f), .. F32(3), .. U32(1), .. F32(0.1f)],
        };
        return Platform(game, 9, 9, block, EmptyMotionBlock);
    }

    private static byte[] Springboard(GameVersion game)
    {
        byte[] block = game is GameVersion.N100F
            ? [.. Floats(6, 6, 6), .. U32(0xCCCC0004), .. U32(0xCCCC0005), .. U32(0), .. Vec3(0, 1, 0)]
            : [.. Floats(6, 0, 0), .. F32(2), .. U32(0xCCCC0004), .. U32(0xCCCC0005), .. U32(0), .. Vec3(0, 1, 0), .. U32(5)];
        return Platform(game, 10, 10, block, EmptyMotionBlock);
    }

    private static byte[] TeeterTotter(GameVersion game) => Platform(game, 11, 11,
        [.. Floats(0, 0.5f, 3), .. game is GameVersion.ROTU ? U32(0x69EC5797) : []], EmptyMotionBlock);

    private static byte[] Paddle(GameVersion game) => Platform(game, 12, 12,
        [.. U32(1), .. U32(3), .. F32(360), .. Floats(0, 90, 180, 0, 0, 0), .. U32(0x13), .. Floats(1, 2, 3, 4)],
        EmptyMotionBlock);

    private static byte[] FullyManipulable(GameVersion game) => Platform(game, 13, 0, [], EmptyMotionBlock);

    private static object[] Case(GameVersion game, Func<GameVersion, byte[]> data) => [game, data(game)];

    public static readonly IEnumerable<object[]> RoundTripCases =
    [
        Case(GameVersion.N100F, ExtendRetract),
        Case(GameVersion.N100F, MovePoint),
        Case(GameVersion.N100F, Mechanism),
        Case(GameVersion.N100F, ConveyorBelt),
        Case(GameVersion.N100F, Breakaway),
        Case(GameVersion.N100F, Springboard),
        Case(GameVersion.N100F, TeeterTotter),
        Case(GameVersion.BFBB, ExtendRetract),
        Case(GameVersion.BFBB, Orbit),
        Case(GameVersion.BFBB, Spline),
        Case(GameVersion.BFBB, MovePoint),
        Case(GameVersion.BFBB, Mechanism),
        Case(GameVersion.BFBB, Pendulum),
        Case(GameVersion.BFBB, ConveyorBelt),
        Case(GameVersion.BFBB, Falling),
        Case(GameVersion.BFBB, ForwardReturn),
        Case(GameVersion.BFBB, Breakaway),
        Case(GameVersion.BFBB, Springboard),
        Case(GameVersion.BFBB, TeeterTotter),
        Case(GameVersion.BFBB, Paddle),
        Case(GameVersion.BFBB, FullyManipulable),
        Case(GameVersion.TSSM, Spline),
        Case(GameVersion.TSSM, Mechanism),
        Case(GameVersion.TSSM, Breakaway),
        Case(GameVersion.TSSM, Springboard),
        Case(GameVersion.TSSM, Paddle),
        Case(GameVersion.TSSM, FullyManipulable),
        Case(GameVersion.Incredibles, Orbit),
        Case(GameVersion.Incredibles, MovePoint),
        Case(GameVersion.ROTU, Mechanism),
        Case(GameVersion.ROTU, TeeterTotter),
        Case(GameVersion.Ratatouille, MovePoint),
        Case(GameVersion.Ratatouille, FullyManipulable),
    ];

    public static readonly IEnumerable<object[]> MotionPlatformTypes =
    [
        [new ExtendRetractMotion(), PlatformType.ExtendRetract],
        [new OrbitMotion(), PlatformType.Orbit],
        [new SplineMotion(), PlatformType.Spline],
        [new MovePointMotion(), PlatformType.MovePoint],
        [new MechanismMotion(), PlatformType.Mechanism],
        [new PendulumMotion(), PlatformType.Pendulum],
        [new ConveyorBeltMotion(), PlatformType.ConveyorBelt],
        [new FallingMotion(), PlatformType.Falling],
        [new ForwardReturnMotion(), PlatformType.ForwardReturn],
        [new BreakawayMotion(), PlatformType.Breakaway],
        [new SpringboardMotion(), PlatformType.Springboard],
        [new TeeterTotterMotion(), PlatformType.TeeterTotter],
        [new PaddleMotion(), PlatformType.Paddle],
        [new FullyManipulableMotion(), PlatformType.FullyManipulable],
    ];

    public static readonly IEnumerable<object[]> MotionSubtypes =
    [
        [new ExtendRetractMotion(), (byte)0],
        [new OrbitMotion(), (byte)0],
        [new SplineMotion(), (byte)0],
        [new MovePointMotion(), (byte)0],
        [new MechanismMotion(), (byte)4],
        [new PendulumMotion(), (byte)5],
        [new ConveyorBeltMotion(), (byte)6],
        [new FallingMotion(), (byte)7],
        [new ForwardReturnMotion(), (byte)8],
        [new BreakawayMotion(), (byte)9],
        [new SpringboardMotion(), (byte)10],
        [new TeeterTotterMotion(), (byte)11],
        [new PaddleMotion(), (byte)12],
        [new FullyManipulableMotion(), (byte)0],
    ];

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void Read_ThenWrite_Platform_ReproducesInputBytes(GameVersion game, byte[] data) =>
        Assert.Equal(data, Write(game, Read(game, data)));

    [Fact]
    public void Read_ThenWrite_PlatformWithLinksAndUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Platform(GameVersion.BFBB, 0, 0, [], ExtendRetractBlock, linkCount: 2), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(GameVersion.BFBB, Read(GameVersion.BFBB, data)));
    }

    [Fact]
    public void Read_ThenWrite_PlatformTypeDisagreeingWithMotion_ReproducesInputBytes()
    {
        byte[] data = Platform(GameVersion.BFBB, 0, 0, [], OrbitBlock);

        Assert.Equal(data, Write(GameVersion.BFBB, Read(GameVersion.BFBB, data)));
    }

    [Fact]
    public void Read_ThenWrite_SubtypeDisagreeingWithPlatformType_ReproducesInputBytes()
    {
        byte[] data = Platform(GameVersion.BFBB, 0, 4, [], ExtendRetractBlock);

        Assert.Equal(data, Write(GameVersion.BFBB, Read(GameVersion.BFBB, data)));
    }

    [Fact]
    public void Read_Platform_ProducesPlatformAsset() =>
        Assert.IsType<PlatformAsset>(Read(GameVersion.BFBB, ExtendRetract(GameVersion.BFBB)));

    [Fact]
    public void Read_Platform_PopulatesPlatformFieldsAndTraits()
    {
        var asset = (PlatformAsset)Read(GameVersion.BFBB, ExtendRetract(GameVersion.BFBB));

        Assert.Equal(PlatformFlags.Solid, asset.Flags);
        Assert.Equal(new AssetId(0xAAAA0001), ((IHasSurface)asset).SurfaceId);
        Assert.Equal(new AssetId(0xAAAA0002), ((IHasModel)asset).ModelId);
        Assert.Equal(new AssetId(0xAAAA0003), ((IHasAnimList)asset).AnimListId);
    }

    [Fact]
    public void Read_ExtendRetract_PopulatesMotion()
    {
        var asset = (PlatformAsset)Read(GameVersion.BFBB, ExtendRetract(GameVersion.BFBB));

        var motion = Assert.IsType<ExtendRetractMotion>(asset.Motion);
        Assert.Equal(MotionFlags.Stopped, motion.Flags);
        Assert.Equal(new Vector3(1, 2, 3), motion.RetractPosition);
        Assert.Equal(new Vector3(0, 5, 0), motion.ExtendOffset);
        Assert.Equal(1f, motion.ExtendTime);
        Assert.Equal(2f, motion.ExtendWaitTime);
        Assert.Equal(3f, motion.RetractTime);
        Assert.Equal(4f, motion.RetractWaitTime);
    }

    [Fact]
    public void Read_MovePoint_PopulatesMotion()
    {
        var asset = (PlatformAsset)Read(GameVersion.BFBB, MovePoint(GameVersion.BFBB));

        var motion = Assert.IsType<MovePointMotion>(asset.Motion);
        Assert.Equal(MotionFlags.FaceTravelDirection, motion.Flags);
        Assert.True(motion.UseBanking);
        Assert.Equal(MovePointFlags.StopAtEachPoint, motion.MovePointFlags);
        Assert.Equal(new AssetId(0xCCCC0002), motion.MovePointId);
        Assert.Equal(5f, motion.Speed);
    }

    [Fact]
    public void Read_SplineUnderBFBB_ReadsOnlySplineId()
    {
        var asset = (PlatformAsset)Read(GameVersion.BFBB, Spline(GameVersion.BFBB));

        var motion = Assert.IsType<SplineMotion>(asset.Motion);
        Assert.Equal(new AssetId(0xCCCC0001), motion.SplineId);
        Assert.Equal(0f, motion.Speed);
    }

    [Fact]
    public void Read_SplineUnderTSSM_PopulatesSpeed()
    {
        var asset = (PlatformAsset)Read(GameVersion.TSSM, Spline(GameVersion.TSSM));

        var motion = Assert.IsType<SplineMotion>(asset.Motion);
        Assert.Equal(new AssetId(0xCCCC0001), motion.SplineId);
        Assert.Equal(12f, motion.Speed);
    }

    [Fact]
    public void Read_MechanismUnderBFBB_PopulatesMotion()
    {
        var asset = (PlatformAsset)Read(GameVersion.BFBB, Mechanism(GameVersion.BFBB));

        var motion = Assert.IsType<MechanismMotion>(asset.Motion);
        Assert.Equal(MechanismMovement.Rotate, motion.Movement);
        Assert.Equal(MechanismFlags.ReturnToStart, motion.MechanismFlags);
        Assert.Equal(MotionAxis.X, motion.SlideAxis);
        Assert.Equal(MotionAxis.Z, motion.RotateAxis);
        Assert.Equal(1f, motion.SlideDistance);
        Assert.Equal(10f, motion.PostReturnDelay);
        Assert.Equal(0f, motion.ScaleAmount);
    }

    [Fact]
    public void Read_MechanismUnderTSSM_PopulatesScaleFields()
    {
        var asset = (PlatformAsset)Read(GameVersion.TSSM, Mechanism(GameVersion.TSSM));

        var motion = Assert.IsType<MechanismMotion>(asset.Motion);
        Assert.Equal(6, motion.ScaleAxis);
        Assert.Equal(1f, motion.SlideDistance);
        Assert.Equal(10f, motion.PostReturnDelay);
        Assert.Equal(11f, motion.ScaleAmount);
        Assert.Equal(12f, motion.ScaleDuration);
    }

    [Fact]
    public void Read_ConveyorBelt_TakesFlagsFromEmptyMotionBlock()
    {
        var asset = (PlatformAsset)Read(GameVersion.BFBB, ConveyorBelt(GameVersion.BFBB));

        var motion = Assert.IsType<ConveyorBeltMotion>(asset.Motion);
        Assert.Equal(4f, motion.Speed);
        Assert.Equal(MotionFlags.Stopped, motion.Flags);
    }

    [Fact]
    public void Read_BreakawayUnderN100F_PopulatesMotion()
    {
        var asset = (PlatformAsset)Read(GameVersion.N100F, Breakaway(GameVersion.N100F));

        var motion = Assert.IsType<BreakawayMotion>(asset.Motion);
        Assert.Equal(1.5f, motion.BreakDelay);
        Assert.Equal(new AssetId(0xCE7F8131), motion.BustModelId);
        Assert.Equal(3f, motion.ResetDelay);
        Assert.Equal(BreakawayFlags.None, motion.BreakFlags);
    }

    [Fact]
    public void Read_BreakawayUnderBFBB_PopulatesBreakFlags()
    {
        var asset = (PlatformAsset)Read(GameVersion.BFBB, Breakaway(GameVersion.BFBB));

        var motion = Assert.IsType<BreakawayMotion>(asset.Motion);
        Assert.Equal(new AssetId(0xCE7F8131), motion.BustModelId);
        Assert.Equal(3f, motion.ResetDelay);
        Assert.Equal(BreakawayFlags.AllowSneak, motion.BreakFlags);
    }

    [Fact]
    public void Read_BreakawayUnderTSSM_PopulatesCollisionOffTime()
    {
        var asset = (PlatformAsset)Read(GameVersion.TSSM, Breakaway(GameVersion.TSSM));

        var motion = Assert.IsType<BreakawayMotion>(asset.Motion);
        Assert.Equal(1.5f, motion.BreakDelay);
        Assert.Equal(3f, motion.ResetDelay);
        Assert.Equal(BreakawayFlags.AllowSneak, motion.BreakFlags);
        Assert.Equal(0.1f, motion.CollisionOffTime);
        Assert.Equal(AssetId.None, motion.BustModelId);
    }

    [Fact]
    public void Read_SpringboardUnderN100F_PopulatesMotion()
    {
        var asset = (PlatformAsset)Read(GameVersion.N100F, Springboard(GameVersion.N100F));

        var motion = Assert.IsType<SpringboardMotion>(asset.Motion);
        Assert.Equal([6f, 6f, 6f], motion.JumpHeights);
        Assert.Equal(new AssetId(0xCCCC0004), motion.SpringAnimationId);
        Assert.Equal(new AssetId(0xCCCC0005), motion.IdleAnimationId);
        Assert.Equal(new Vector3(0, 1, 0), motion.JumpDirection);
    }

    [Fact]
    public void Read_SpringboardUnderBFBB_PopulatesBounceAndFlags()
    {
        var asset = (PlatformAsset)Read(GameVersion.BFBB, Springboard(GameVersion.BFBB));

        var motion = Assert.IsType<SpringboardMotion>(asset.Motion);
        Assert.Equal([6f, 0f, 0f], motion.JumpHeights);
        Assert.Equal(2f, motion.BounceHeight);
        Assert.Equal(new Vector3(0, 1, 0), motion.JumpDirection);
        Assert.Equal(SpringboardFlags.LockView | SpringboardFlags.LockMovement, motion.SpringFlags);
    }

    [Fact]
    public void Read_TeeterTotterUnderROTU_PopulatesUnknown()
    {
        var asset = (PlatformAsset)Read(GameVersion.ROTU, TeeterTotter(GameVersion.ROTU));

        var motion = Assert.IsType<TeeterTotterMotion>(asset.Motion);
        Assert.Equal(0.5f, motion.MaxTilt);
        Assert.Equal(3f, motion.InverseMass);
        Assert.Equal(0x69EC5797u, motion.Unknown);
    }

    [Fact]
    public void Read_Paddle_TakesOrientationsFromCount()
    {
        var asset = (PlatformAsset)Read(GameVersion.BFBB, Paddle(GameVersion.BFBB));

        var motion = Assert.IsType<PaddleMotion>(asset.Motion);
        Assert.Equal(1, motion.StartOrientation);
        Assert.Equal([0f, 90f, 180f], motion.Orientations);
        Assert.Equal(360f, motion.OrientationLoop);
        Assert.Equal((PaddleFlags)0x13, motion.PaddleFlags);
        Assert.Equal(4f, motion.HubRadius);
    }

    [Fact]
    public void Read_FullyManipulable_ProducesFullyManipulableMotion() =>
        Assert.IsType<FullyManipulableMotion>(((PlatformAsset)Read(GameVersion.TSSM, FullyManipulable(GameVersion.TSSM))).Motion);

    [Fact]
    public void Read_PlatformTypeDisagreeingWithMotion_PreservesPlatformType()
    {
        var asset = (PlatformAsset)Read(GameVersion.BFBB, Platform(GameVersion.BFBB, 0, 0, [], OrbitBlock));

        Assert.IsType<OrbitMotion>(asset.Motion);
        Assert.Equal(PlatformType.ExtendRetract, asset.Physical.PlatformType);
    }

    [Fact]
    public void Read_UnknownPlatformType_ThrowsInvalidDataException() =>
        Assert.Throws<InvalidDataException>(() => Read(GameVersion.BFBB, Platform(GameVersion.BFBB, 14, 14, [], EmptyMotionBlock)));

    [Fact]
    public void Read_PaddleUnderN100F_ThrowsInvalidDataException() =>
        Assert.Throws<InvalidDataException>(() => Read(GameVersion.N100F, Platform(GameVersion.N100F, 12, 12, [], EmptyMotionBlock)));

    [Fact]
    public void Read_EntityMotionTypeWithEmptyMotionBlock_ThrowsInvalidDataException() =>
        Assert.Throws<InvalidDataException>(() => Read(GameVersion.BFBB, Platform(GameVersion.BFBB, 0, 0, [], EmptyMotionBlock)));

    [Fact]
    public void Read_PlatformMotionTypeWithNonEmptyMotionBlock_ThrowsInvalidDataException() =>
        Assert.Throws<InvalidDataException>(() => Read(GameVersion.BFBB, Platform(GameVersion.BFBB, 6, 6, F32(4), OrbitBlock)));

    [Fact]
    public void Write_NewPlatform_WritesTypeAndSubtypeFromMotion()
    {
        var asset = new PlatformAsset { Type = AssetType.Platform, Motion = new MechanismMotion() };

        byte[] written = Write(GameVersion.BFBB, asset);

        Assert.Equal(4, written[0x09]); // Subtype
        Assert.Equal(4, written[0x54]); // PlatformType
    }

    [Fact]
    public void Write_PaddleUnderN100F_WritesEmptyTypeBlock()
    {
        var asset = new PlatformAsset
        {
            Type = AssetType.Platform,
            Motion = new PaddleMotion { Orientations = [0f, 90f], RotateSpeed = 2f },
        };

        byte[] written = Write(GameVersion.N100F, asset);

        Assert.Equal(0xA8, written.Length);
        Assert.Equal(new byte[0x24], written[0x54..0x78]);
    }

    [Theory]
    [MemberData(nameof(MotionPlatformTypes))]
    public void PlatformType_WithMotion_FollowsMotion(Motion motion, PlatformType expected)
    {
        platform.Motion = motion;

        Assert.Equal(expected, platform.Physical.PlatformType);
    }

    [Theory]
    [MemberData(nameof(MotionSubtypes))]
    public void Subtype_WithMotion_FollowsPlatformType(Motion motion, byte expected)
    {
        platform.Motion = motion;

        Assert.Equal(expected, platform.Physical.Subtype);
    }

    [Fact]
    public void PlatformType_SetToDisagreeingValue_OverridesMotion()
    {
        platform.Physical.PlatformType = PlatformType.Orbit;

        platform.Motion = new MechanismMotion();

        Assert.Equal(PlatformType.Orbit, platform.Physical.PlatformType);
    }

    [Fact]
    public void PlatformType_SetToAgreeingValue_KeepsFollowingMotion()
    {
        platform.Physical.PlatformType = PlatformType.ExtendRetract;

        platform.Motion = new MechanismMotion();

        Assert.Equal(PlatformType.Mechanism, platform.Physical.PlatformType);
    }

    [Fact]
    public void Subtype_WithOverriddenPlatformType_FollowsOverride()
    {
        platform.Physical.PlatformType = PlatformType.Paddle;

        Assert.Equal((byte)12, platform.Physical.Subtype);
    }

    [Fact]
    public void Subtype_SetToDisagreeingValue_OverridesPlatformType()
    {
        platform.Physical.Subtype = 9;

        platform.Motion = new MechanismMotion();

        Assert.Equal((byte)9, platform.Physical.Subtype);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void JumpHeights_SetWithWrongLength_ThrowsArgumentException(int length)
    {
        var motion = new SpringboardMotion();

        Assert.Throws<ArgumentException>(() => motion.JumpHeights = [.. new float[length]]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Orientations_SetWithinMax_StoresValue(int length)
    {
        var motion = new PaddleMotion
        {
            Orientations = [.. new float[length]]
        };

        Assert.Equal(length, motion.Orientations.Length);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(12)]
    public void Orientations_SetBeyondMax_ThrowsArgumentException(int length)
    {
        var motion = new PaddleMotion();

        Assert.Throws<ArgumentException>(() => motion.Orientations = [.. new float[length]]);
    }
}
