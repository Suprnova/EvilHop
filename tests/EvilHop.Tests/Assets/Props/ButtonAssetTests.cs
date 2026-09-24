using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class ButtonAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Button;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x18,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EntityPrefix(bool hasPadding) =>
    [
        0x01, 0x00, 0x00, 0x02,   // EntityFlags, Subtype, PFlags, CollisionFlags
        .. hasPadding ? new byte[4] : [],
        0x11, 0x22, 0x33, 0x44,   // SurfaceId
        .. new byte[36],          // Angle/Position/Scale
        .. new byte[16],          // ColorMultiplier
        0x43, 0x7F, 0x00, 0x00,   // SeeThroughSpeed = 255
        0xAA, 0xBB, 0xCC, 0xDD,   // ModelId
        0x00, 0x00, 0x00, 0x00,   // AnimListId
    ];

    private static byte[] U32(uint value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] ButtonFields(uint kind, uint autoReset, float resetDelay, uint activatedBy) =>
    [
        0xCE, 0x7F, 0x81, 0x31,   // PressedModelId
        .. U32(kind),
        0x00, 0x00, 0x00, 0x00,   // InitialState
        .. U32(autoReset),
        .. F32(resetDelay),
        .. U32(activatedBy),
    ];

    private static byte[] MechanismBytes(int blockSize)
    {
        byte[] fields =
        [
            0x04, 0x00, 0x00, 0x04,  // type = Mechanism, use_banking, flags = Stopped
            0x02, 0x01, 0x01, 0x00,  // Movement, Loop, SlideAxis, RotateAxis
            .. blockSize == 0x3C ? new byte[4] : [], // ScaleAxis + padding
            .. F32(-0.2f), .. F32(0.15f),
        ];
        return [.. fields, .. new byte[blockSize - fields.Length]];
    }

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        .. U32(destinationAssetId),
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] Data(uint kind = 0, uint autoReset = 1, float resetDelay = 1.5f, uint activatedBy = 0x19,
        byte linkCount = 0, bool hasPadding = true, int blockSize = 0x30) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding),
        .. ButtonFields(kind, autoReset, resetDelay, activatedBy),
        .. MechanismBytes(blockSize),
    ];

    [Fact]
    public void Read_Button_ProducesButtonAsset() =>
        Assert.IsType<ButtonAsset>(Read(Data()));

    [Fact]
    public void Read_LatchingButton_PopulatesFields()
    {
        var asset = (ButtonAsset)Read(Data(kind: 0, autoReset: 1, resetDelay: 10f, activatedBy: 0x19));

        Assert.Equal(new AssetId(0xCE7F8131), asset.PressedModelId);
        Assert.Equal(ButtonAsset.ButtonKind.Latching, asset.Kind);
        Assert.Equal(0, asset.Physical.InitialState);
        Assert.True(asset.AutoReset);
        Assert.Equal(10f, asset.ResetDelay);
        Assert.Equal(
            ButtonAsset.Activators.BubbleSpin | ButtonAsset.Activators.Boulder | ButtonAsset.Activators.CruiseBubble,
            asset.ActivatedBy);
        Assert.Equal(EntityMotion.Mechanism.Sequence.SlideAndRotate, asset.Motion.Movement);
        Assert.Equal(-0.2f, asset.Motion.SlideDistance);
        Assert.Equal(0.15f, asset.Motion.SlideTime);
    }

    [Fact]
    public void Read_PressurePlate_PopulatesFields()
    {
        var asset = (ButtonAsset)Read(Data(kind: 1, autoReset: 1, resetDelay: 5f, activatedBy: 0x400));

        Assert.Equal(ButtonAsset.ButtonKind.PressurePlate, asset.Kind);
        Assert.True(asset.AutoReset);
        Assert.Equal(5f, asset.ResetDelay);
        Assert.Equal(ButtonAsset.Activators.PlayerStanding, asset.ActivatedBy);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(2u)]
    public void Read_ThenWrite_Button_ReproducesInputBytes(uint kind)
    {
        byte[] data =
        [
            .. Data(kind: kind, linkCount: 1),
            .. LinkBytes(0x37, 0x12, 0x55667788),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ButtonWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ButtonUnderTSSM_DropsPaddingAndUsesLargerMotionBlock_ReproducesInputBytes()
    {
        byte[] data = Data(kind: 1, hasPadding: false, blockSize: 0x3C);
        var profile = TSSMSerializer.DefaultProfile;

        var asset = (ButtonAsset)Read(data, profile);

        Assert.Equal(new AssetId(0xCE7F8131), asset.PressedModelId);
        Assert.Equal(-0.2f, asset.Motion.SlideDistance);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_Button_UnderN100F_DegradesToGenericAsset()
    {
        byte[] data = Data(hasPadding: false);

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<ButtonAsset>(asset, exactMatch: false);
    }

    [Fact]
    public void AutoReset_SetOnLatchingButton_WritesStoredInt()
    {
        var asset = (ButtonAsset)Read(Data(kind: 0, autoReset: 0));

        asset.AutoReset = true;

        Assert.Equal(Data(kind: 0, autoReset: 1), Write(asset));
    }

    [Fact]
    public void ModelId_SetThroughIHasModel_ProjectsOntoPhysicalModelId()
    {
        var asset = new ButtonAsset();

        ((IHasModel)asset).ModelId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.Physical.ModelId);
    }

    [Fact]
    public void SurfaceId_SetThroughIHasSurface_ProjectsOntoPhysicalSurfaceId()
    {
        var asset = new ButtonAsset();

        ((IHasSurface)asset).SurfaceId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.Physical.SurfaceId);
    }

    [Fact]
    public void Read_ThenWrite_RealTSSMExemplar_ReproducesInputBytes()
    {
        // tssm/release/GC/NTSC-U/US-r1/BB/bb02.HIP, AHDR id=0x5D3995A5
        byte[] data =
        [
            0x5D, 0x39, 0x95, 0xA5, 0x18, 0x02, 0x00, 0x1D, 0x01, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00,
            0x3F, 0xC9, 0x0F, 0xDB, 0x40, 0x96, 0xCB, 0xE4, 0x00, 0x00, 0x00, 0x00, 0x42, 0xF2, 0x5E, 0x91,
            0x3F, 0x29, 0xC0, 0xEC, 0xC2, 0x0C, 0xAE, 0x14, 0x40, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00,
            0x40, 0x00, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00,
            0x3F, 0x80, 0x00, 0x00, 0x43, 0x7F, 0x00, 0x00, 0xB7, 0x80, 0x9B, 0x16, 0x00, 0x00, 0x00, 0x00,
            0x6B, 0xDE, 0x44, 0x3E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x3F, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x19, 0x04, 0x00, 0x00, 0x04, 0x02, 0x01, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x00, 0xBF, 0x00, 0x00, 0x00, 0x3E, 0x19, 0x99, 0x9A,
            .. new byte[40], // unused union padding, up to the 0x3C block size
            0x00, 0x37, 0x00, 0x12, 0x14, 0x6E, 0xDF, 0x34, .. new byte[24],
            0x00, 0x37, 0x00, 0x12, 0xC5, 0x21, 0x1D, 0x30, .. new byte[24],
        ];
        var profile = TSSMSerializer.DefaultProfile;

        var asset = (ButtonAsset)Read(data, profile);

        Assert.Equal(new AssetId(0x6BDE443E), asset.PressedModelId);
        Assert.False(asset.AutoReset);
        Assert.Equal(1.5f, asset.ResetDelay);
        Assert.Equal(
            ButtonAsset.Activators.BubbleSpin | ButtonAsset.Activators.Boulder | ButtonAsset.Activators.CruiseBubble,
            asset.ActivatedBy);
        Assert.Equal(-0.5f, asset.Motion.SlideDistance);
        Assert.Equal(2, asset.Links.Count);
        Assert.Equal(new AssetId(0xC5211D30), asset.Links[1].DestinationAssetId);

        Assert.Equal(data, Write(asset, profile));
    }
}
