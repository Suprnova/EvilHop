using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using static EvilHop.Assets.UIMotionCommand;

namespace EvilHop.Tests.Serialization;

public class UIMotionAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new TSSMSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.UIMotion;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile profile)
    {
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile profile)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Prefix(byte commandCount, byte inFlag, uint commandsSize, float totalTime, float loopTime) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x53,                   // BaseType
        0x00,                   // LinkCount
        0x00, 0x1D,             // BaseFlags
        commandCount,
        inFlag,
        0x00, 0x00,             // padding
        (byte)(commandsSize >> 24), (byte)(commandsSize >> 16), (byte)(commandsSize >> 8), (byte)commandsSize,
        .. F32(totalTime),
        .. F32(loopTime),
    ];

    private static byte[] CommandHeader(uint type, float startTime, float endTime, float accelTime, float decelTime, bool enabled) =>
    [
        (byte)(type >> 24), (byte)(type >> 16), (byte)(type >> 8), (byte)type,
        .. F32(startTime), .. F32(endTime), .. F32(accelTime), .. F32(decelTime),
        (byte)(enabled ? 1 : 0),
        0x00, 0x00, 0x00, // padding
    ];

    private static byte[] MoveCommandBytes(bool enabled, float distX, float distY) =>
    [
        .. CommandHeader(0, 0.0f, 1.0f, 0.0f, 0.0f, enabled),
        .. F32(distX), .. F32(distY),
    ];

    private static byte[] ColorCommandBytes(bool enabled, byte sr, byte sg, byte sb, byte er, byte eg, byte eb) =>
    [
        .. CommandHeader(6, 0.0f, 1.0f, 0.0f, 0.0f, enabled),
        sr, sg, sb, er, eg, eb, 0x00, 0x00,
    ];

    private static byte[] OneMoveCommandData(byte commandCount = 1) =>
    [
        .. Prefix(commandCount, inFlag: 1, commandsSize: 0x20, totalTime: 1.0f, loopTime: 0.0f),
        .. MoveCommandBytes(enabled: true, distX: 1.5f, distY: -2.5f),
    ];

    [Fact]
    public void Read_UIMotion_ProducesUIMotionAsset() =>
        Assert.IsType<UIMotionAsset>(Read(OneMoveCommandData(), TSSMSerializer.DefaultProfile));

    [Fact]
    public void Read_UIMotion_PopulatesHeaderFields()
    {
        var asset = (UIMotionAsset)Read(OneMoveCommandData(), TSSMSerializer.DefaultProfile);

        Assert.Equal(1.0f, asset.TotalTime);
        Assert.Equal(0.0f, asset.LoopTime);
        Assert.Equal((byte)1, ((Physical.IUIMotionAsset)asset).InFlag);
        Assert.Single(asset.Commands);
    }

    [Fact]
    public void Read_UIMotion_PopulatesMoveCommand()
    {
        var asset = (UIMotionAsset)Read(OneMoveCommandData(), TSSMSerializer.DefaultProfile);
        var command = Assert.IsType<Move>(asset.Commands[0]);

        Assert.Equal(Command.Move, command.Type);
        Assert.Equal(0.0f, command.StartTime);
        Assert.Equal(1.0f, command.EndTime);
        Assert.True(command.Enabled);
        Assert.Equal(1.5f, command.DistanceX);
        Assert.Equal(-2.5f, command.DistanceY);
    }

    [Fact]
    public void Read_UIMotion_PopulatesColorCommand()
    {
        byte[] data =
        [
            .. Prefix(1, inFlag: 1, commandsSize: 0x20, totalTime: 1.0f, loopTime: 0.0f),
            .. ColorCommandBytes(enabled: true, sr: 0xFF, sg: 0x80, sb: 0x00, er: 0x00, eg: 0x80, eb: 0xFF),
        ];

        var asset = (UIMotionAsset)Read(data, TSSMSerializer.DefaultProfile);
        var command = Assert.IsType<Color>(asset.Commands[0]);

        Assert.Equal(new Rgb(0xFF / 255f, 0x80 / 255f, 0x00 / 255f), command.StartColor);
        Assert.Equal(new Rgb(0x00 / 255f, 0x80 / 255f, 0xFF / 255f), command.EndColor);
    }

    [Fact]
    public void Read_ThenWrite_UIMotionWithOneCommand_ReproducesInputBytes()
    {
        byte[] data = OneMoveCommandData();
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_UIMotionWithEveryCommandType_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(8, inFlag: 1, commandsSize: 8 * 24 + (8 + 20 + 12 + 4 + 20 + 4 + 8 + 8), totalTime: 2.0f, loopTime: 0.5f),
            .. MoveCommandBytes(true, 1.0f, 2.0f),
            .. (byte[])
            [
                .. CommandHeader(1, 0.0f, 1.0f, 0.0f, 0.0f, true), // Scale
                .. F32(2.0f), .. F32(2.0f), 0x01, 0x00, 0x00, 0x00, .. F32(0.0f), .. F32(0.0f),
            ],
            .. (byte[])
            [
                .. CommandHeader(2, 0.0f, 1.0f, 0.0f, 0.0f, true), // Rotate
                .. F32(90.0f), .. F32(0.0f), .. F32(0.0f),
            ],
            .. (byte[])
            [
                .. CommandHeader(3, 0.0f, 1.0f, 0.0f, 0.0f, true), // Opacity
                0xFF, 0x00, 0x00, 0x00,
            ],
            .. (byte[])
            [
                .. CommandHeader(4, 0.0f, 1.0f, 0.0f, 0.0f, true), // AbsoluteScale
                .. F32(1.0f), .. F32(1.0f), .. F32(2.0f), .. F32(2.0f), 0x00, 0x05, 0x00, 0x00,
            ],
            .. (byte[])
            [
                .. CommandHeader(5, 0.0f, 1.0f, 0.0f, 0.0f, true), // Brightness
                0x10, 0x20, 0x00, 0x00,
            ],
            .. ColorCommandBytes(true, 0xFF, 0x80, 0x00, 0x00, 0x80, 0xFF),
            .. (byte[])
            [
                .. CommandHeader(7, 0.0f, 1.0f, 0.0f, 0.0f, false), // UVScroll
                .. F32(0.5f), .. F32(-0.5f),
            ],
        ];
        var profile = TSSMSerializer.DefaultProfile;

        var asset = (UIMotionAsset)Read(data, profile);
        Assert.Equal(8, asset.Commands.Count);
        Assert.IsType<Move>(asset.Commands[0]);
        Assert.IsType<Scale>(asset.Commands[1]);
        Assert.IsType<Rotate>(asset.Commands[2]);
        Assert.IsType<Opacity>(asset.Commands[3]);
        Assert.IsType<AbsoluteScale>(asset.Commands[4]);
        Assert.IsType<Brightness>(asset.Commands[5]);
        Assert.IsType<Color>(asset.Commands[6]);
        Assert.IsType<UVScroll>(asset.Commands[7]);
        Assert.False(((UVScroll)asset.Commands[7]).Enabled);

        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThenWrite_UIMotionWithNoCommands_ReproducesInputBytes()
    {
        byte[] data = Prefix(0, inFlag: 1, commandsSize: 0, totalTime: 0.0f, loopTime: 0.0f);
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_UIMotionWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. OneMoveCommandData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_UIMotionUnderIncredibles_ReproducesInputBytes()
    {
        byte[] data = OneMoveCommandData();
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_UIMotionUnderROTU_ReproducesInputBytes()
    {
        byte[] data = OneMoveCommandData();
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_UIMotionUnderRatatouille_ReproducesInputBytes()
    {
        byte[] data = OneMoveCommandData();
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_UIMotion_UnderBFBB_DegradesToGenericBaseAsset()
    {
        byte[] data = OneMoveCommandData();
        var profile = BFBBSerializer.DefaultProfile;

        var asset = Read(data, profile);

        Assert.IsNotType<UIMotionAsset>(asset);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void CommandCount_AfterRead_DerivesFromCommandsCollection()
    {
        var asset = (UIMotionAsset)Read(OneMoveCommandData(), TSSMSerializer.DefaultProfile);

        asset.Commands.Add(new Move());

        Assert.Equal((byte)2, ((Physical.IUIMotionAsset)asset).CommandCount);
    }

    [Fact]
    public void CommandsSize_AfterRead_DerivesFromCommandsCollection()
    {
        var asset = (UIMotionAsset)Read(OneMoveCommandData(), TSSMSerializer.DefaultProfile);

        asset.Commands.Add(new Scale());

        Assert.Equal((uint)(32 + 44), ((Physical.IUIMotionAsset)asset).CommandsSize);
    }
}
