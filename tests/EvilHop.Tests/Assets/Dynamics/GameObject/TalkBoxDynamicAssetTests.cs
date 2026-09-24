using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class TalkBoxDynamicAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Id = 0x1234;
        header.Type = AssetType.Dynamic;
        header.Debug = debug;

        return (header, debug);
    }

    private static DynamicAsset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), Endianness.Big);
        return (DynamicAsset)AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(DynamicAsset asset, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount, short version = 11) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x00,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
        0x09, 0x34, 0xB1, 0x96, // Kind (game_object:talk_box)
        (byte)(version >> 8), (byte)version,
        0x00, 0x00,             // Handle
    ];

    private static readonly byte[] Body =
    [
        0x00, 0x00, 0x00, 0xA1, // dialog_box
        0x00, 0x00, 0x00, 0xA2, // prompt_box
        0x00, 0x00, 0x00, 0xA3, // quit_box
        0x01,                   // trap
        0x01,                   // pause
        0x01,                   // allow_quit
        0x02,                   // trigger_pads
        0x00,                   // page
        0x00,                   // show
        0x13,                   // hide
        0x01,                   // audio_effect
        0x00, 0x00, 0x00, 0xB1, // teleport
        0x01, 0x01, 0x00, 0x01, // auto_wait.type: time, prompt, sound, event
        0x40, 0xA0, 0x00, 0x00, // auto_wait.delay (5.0)
        0x00, 0x00, 0x00, 0x07, // auto_wait.which_event
        0x00, 0x00, 0x00, 0xC1, // prompt.skip
        0x00, 0x00, 0x00, 0xC2, // prompt.noskip
        0x00, 0x00, 0x00, 0xC3, // prompt.quit
        0x00, 0x00, 0x00, 0xC4, // prompt.noquit
        0x00, 0x00, 0x00, 0xC5, // prompt.yesno
    ];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16), (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        .. new byte[24], // Params, ParamWidgetAssetId, CheckAssetId
    ];

    [Theory]
    [InlineData(GameVersion.BFBB)]
    [InlineData(GameVersion.TSSM)]
    [InlineData(GameVersion.Incredibles)]
    public void Read_SupportedGame_ProducesTalkBoxDynamicAsset(GameVersion game) =>
        Assert.IsType<TalkBoxDynamicAsset>(Read([.. Prefix(0), .. Body], BFBBSerializer.DefaultProfile with { Game = game }));

    [Fact]
    public void Read_UnsupportedVersion_ProducesGenericDynamicAsset() =>
        Assert.IsType<GenericDynamicAsset>(Read([.. Prefix(0, version: 10), .. Body]));

    [Fact]
    public void Read_TalkBox_PopulatesTextBoxIds()
    {
        var asset = (TalkBoxDynamicAsset)Read([.. Prefix(0), .. Body]);

        Assert.Equal(new AssetId(0xA1), asset.DialogBoxId);
        Assert.Equal(new AssetId(0xA2), asset.PromptBoxId);
        Assert.Equal(new AssetId(0xA3), asset.QuitBoxId);
    }

    [Fact]
    public void Read_TalkBox_PopulatesBehavior()
    {
        var asset = (TalkBoxDynamicAsset)Read([.. Prefix(0), .. Body]);

        Assert.True(asset.TrapsPlayer);
        Assert.True(asset.AllowQuit);
        Assert.Equal(TalkBoxDynamicAsset.PadTriggerMode.Active, asset.TriggerPads);
        Assert.Equal(TalkBoxDynamicAsset.AudioEffectKind.FadeMusic, asset.AudioEffect);
        Assert.Equal(new AssetId(0xB1), asset.TeleportTargetId);
    }

    [Fact]
    public void Read_TalkBox_PopulatesPhysicalBytes()
    {
        var asset = (TalkBoxDynamicAsset)Read([.. Prefix(0), .. Body]);

        Assert.Equal(1, asset.Physical.Pause);
        Assert.Equal(0, asset.Physical.Page);
        Assert.Equal(0, asset.Physical.Show);
        Assert.Equal(0x13, asset.Physical.Hide);
    }

    [Fact]
    public void Read_TalkBox_PopulatesAutoWait()
    {
        var wait = ((TalkBoxDynamicAsset)Read([.. Prefix(0), .. Body])).AutoWait;

        Assert.True(wait.WaitsForTime);
        Assert.True(wait.WaitsForPrompt);
        Assert.False(wait.WaitsForSound);
        Assert.True(wait.WaitsForEvent);
        Assert.Equal(5.0f, wait.Delay);
        Assert.Equal(7, wait.EventIndex);
    }

    [Fact]
    public void Read_TalkBox_PopulatesPromptIds()
    {
        var asset = (TalkBoxDynamicAsset)Read([.. Prefix(0), .. Body]);

        Assert.Equal(new AssetId(0xC1), asset.SkipPromptId);
        Assert.Equal(new AssetId(0xC2), asset.NoSkipPromptId);
        Assert.Equal(new AssetId(0xC3), asset.QuitPromptId);
        Assert.Equal(new AssetId(0xC4), asset.NoQuitPromptId);
        Assert.Equal(new AssetId(0xC5), asset.YesNoPromptId);
    }

    [Fact]
    public void Read_TalkBoxWithLinks_ReadsLinksAfterItsFields()
    {
        var asset = (TalkBoxDynamicAsset)Read([.. Prefix(1), .. Body, .. LinkBytes(1, 2, 0xAABBCCDD)]);

        Assert.Equal(new AssetId(0xAABBCCDD), Assert.Single(asset.Links).DestinationAssetId);
        Assert.Empty(asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Read_ThenWrite_TalkBoxWithLinks_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(2), .. Body, .. LinkBytes(1, 2, 0xAABBCCDD), .. LinkBytes(3, 4, 0x11223344)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_TalkBoxWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(1), .. Body, 0xDE, 0xAD, .. LinkBytes(1, 2, 0xAABBCCDD)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Write_NewTalkBox_WritesItsKindAndVersion()
    {
        var asset = new TalkBoxDynamicAsset { Id = new AssetId(0x1234) };

        Assert.Equal(Prefix(0)[8..14], Write(asset)[8..14]);
    }
}
