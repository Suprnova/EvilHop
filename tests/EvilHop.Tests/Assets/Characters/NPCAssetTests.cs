using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class NPCAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.NPC;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= N100FSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= N100FSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x02,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EntityPrefix() =>
    [
        0x01, 0x00, 0x00, 0x00,   // EntityFlags, Subtype, PFlags, CollisionFlags
        0x00, 0x00, 0x00, 0x00,   // SurfaceId
        .. new byte[36],          // Angle/Position/Scale
        .. new byte[16],          // ColorMultiplier
        0x43, 0x7F, 0x00, 0x00,   // SeeThroughSpeed = 255
        0x44, 0x44, 0x44, 0x44,   // ModelId
        0x00, 0x00, 0x00, 0x00,   // AnimListId
    ];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] I16(short value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] NPCFields() =>
    [
        .. F32(5.0f),   // ActivateRadius
        .. F32(90.0f),  // ActivateFOV
        .. F32(4.0f),   // DetectHeight
        .. F32(1.0f),   // DetectHeightOffset
        .. F32(2.0f),   // SpeedMovement
        .. F32(6.0f),   // SpeedPursue
        .. F32(3.0f),   // SpeedTurn
        .. F32(20.0f),  // PursuitRange
        .. I16(5),      // DazedDuration
        .. I16(3),      // GloatDuration
        .. I16(7),      // GummedDuration
        .. I16(9),      // BubbleDuration
        4,              // Hitpoints
        1,              // BehaviorState
        0x00, 0x00,     // pad
        .. U32(0x7F000080), // VillFlags
        .. F32(2.0f),   // LobSpeed
        .. F32(1.0f),   // LobDurReload
        .. F32(4.0f),   // LobRange
        .. U32(1u),     // LobSalvo
        .. U32(0x11111111), // ProjectileTypeId
        .. U32(0x22222222), // BullseyeId
        .. F32(-1.0f),  // LobArcness
        .. F32(-1.0f),  // LobHeavy
        .. F32(5.0f),   // ExtenderRange
        .. F32(1.0f),   // ExtenderWidth
        .. F32(10.0f),  // ExtenderDuration
        .. F32(2.5f),   // ExtenderRate
        .. F32(10.0f),  // ExtenderReloadTime
        .. U32(0x33333333), // MovePointId
        .. U32(0xCDCDCDCD), // PathAssetId
        .. U32(0u),     // MinPlayerPowerups
        .. U32(0u),     // MinGameDifficulty
    ];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        (byte)(destinationAssetId >> 24), (byte)(destinationAssetId >> 16), (byte)(destinationAssetId >> 8), (byte)destinationAssetId,
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] Data(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(),
        .. NPCFields(),
    ];

    [Fact]
    public void Read_NPC_ProducesNPCAsset() =>
        Assert.IsType<NPCAsset>(Read(Data()));

    [Fact]
    public void Read_NPC_PopulatesEveryField()
    {
        var asset = (NPCAsset)Read(Data());

        Assert.Equal(5.0f, asset.ActivateRadius);
        Assert.Equal(90.0f, asset.ActivateFOV);
        Assert.Equal(4.0f, asset.DetectHeight);
        Assert.Equal(1.0f, asset.DetectHeightOffset);
        Assert.Equal(2.0f, asset.SpeedMovement);
        Assert.Equal(6.0f, asset.SpeedPursue);
        Assert.Equal(3.0f, asset.SpeedTurn);
        Assert.Equal(20.0f, asset.PursuitRange);
        Assert.Equal(5, asset.DazedDuration);
        Assert.Equal(3, asset.GloatDuration);
        Assert.Equal(7, asset.GummedDuration);
        Assert.Equal(9, asset.BubbleDuration);
        Assert.Equal(4, asset.Hitpoints);
        Assert.Equal(1, asset.BehaviorState);
        Assert.Equal(0x7F000080u, asset.Physical.VillFlags);
        Assert.Equal(2.0f, asset.LobSpeed);
        Assert.Equal(1.0f, asset.LobDurReload);
        Assert.Equal(4.0f, asset.LobRange);
        Assert.Equal(1u, asset.LobSalvo);
        Assert.Equal(new AssetId(0x11111111), asset.ProjectileTypeId);
        Assert.Equal(new AssetId(0x22222222), asset.BullseyeId);
        Assert.Equal(-1.0f, asset.LobArcness);
        Assert.Equal(-1.0f, asset.LobHeavy);
        Assert.Equal(5.0f, asset.ExtenderRange);
        Assert.Equal(1.0f, asset.ExtenderWidth);
        Assert.Equal(10.0f, asset.ExtenderDuration);
        Assert.Equal(2.5f, asset.ExtenderRate);
        Assert.Equal(10.0f, asset.ExtenderReloadTime);
        Assert.Equal(new AssetId(0x33333333), asset.MovePointId);
        Assert.Equal(new AssetId(0xCDCDCDCD), asset.Physical.PathAssetId);
        Assert.Equal(0, asset.MinPlayerPowerups);
        Assert.Equal(0, asset.MinGameDifficulty);
        Assert.Equal(new AssetId(0x44444444), asset.Physical.ModelId);
    }

    [Fact]
    public void Read_ThenWrite_NPC_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_NPCWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_NPC_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = Data();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<NPCAsset>(asset);
    }

    [Fact]
    public void ModelId_SetThroughIHasModel_ProjectsOntoPhysicalModelId()
    {
        var asset = new NPCAsset();

        ((IHasModel)asset).ModelId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.Physical.ModelId);
    }

    [Fact]
    public void VillFlags_SetThroughPhysical_IsStoredIndependently()
    {
        var asset = new NPCAsset();

        asset.Physical.VillFlags = 0x7F000080;

        Assert.Equal(0x7F000080u, asset.Physical.VillFlags);
    }

    [Fact]
    public void PathAssetId_SetThroughPhysical_IsStoredIndependently()
    {
        var asset = new NPCAsset();

        asset.Physical.PathAssetId = new AssetId(0xCDCDCDCD);

        Assert.Equal(new AssetId(0xCDCDCDCD), asset.Physical.PathAssetId);
    }
}
