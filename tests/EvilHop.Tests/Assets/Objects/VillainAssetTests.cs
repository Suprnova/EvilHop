using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class VillainAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(Serializer serializer)
    {
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Villain;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        var serializer = new BFBBSerializer(profile);
        var (header, debug) = HeaderFor(serializer);
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

    // BaseAssetPrefix: BaseId(4), BaseType(1), LinkCount(1), BaseFlags(2)
    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x2B,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EntityPrefix(bool hasPadding, bool hasAnimListId = true) =>
    [
        0x01, 0x00, 0x00, 0x00,   // EntityFlags, Subtype, PFlags, CollisionFlags
        .. hasPadding ? new byte[4] : [],
        0x00, 0x00, 0x00, 0x00,   // SurfaceId
        .. new byte[36],          // Angle/Position/Scale
        .. new byte[16],          // ColorMultiplier
        0x43, 0x7F, 0x00, 0x00,   // SeeThroughSpeed = 255
        0xAA, 0xBB, 0xCC, 0xDD,   // ModelId
        .. hasAnimListId ? new byte[4] : [], // AnimListId
    ];

    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] I32(int value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] VilFields() =>
    [
        .. I32(0x00000001),     // NpcFlags
        .. U32(0x11111111),     // NpcModelId
        .. U32(0x22222222),     // NpcSettingsId
        .. U32(0x33333333),     // MovePointId
        .. U32(0x44444444),     // TaskWidgetPrimeId
        .. U32(0x55555555),     // TaskWidgetSecondId
    ];

    private static byte[] IncrediblesOnlyFields() =>
    [
        .. U32(0x66666666),     // NavigationMeshId
        .. U32(0x77777777),     // SettingsId
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

    private static byte[] BfbbData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: true),
        .. VilFields(),
    ];

    private static byte[] IncrediblesData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: false),
        .. VilFields(),
        .. IncrediblesOnlyFields(),
    ];

    [Fact]
    public void Read_Villain_ProducesVillainAsset() =>
        Assert.IsType<VillainAsset>(Read(BfbbData()));

    [Fact]
    public void DefaultValues_HaveCorrectType()
    {
        var asset = new VillainAsset();

        Assert.Equal(AssetType.Villain, asset.Type);
        Assert.Equal(0x2B, asset.Physical.BaseType);
    }

    [Fact]
    public void Read_Villain_UnderBfbb_PopulatesAllFields()
    {
        var asset = (VillainAsset)Read(BfbbData());

        Assert.Equal(0x00000001, asset.NpcFlags);
        Assert.Equal(new AssetId(0x11111111), asset.NpcModelId);
        Assert.Equal(new AssetId(0x22222222), asset.NpcSettingsId);
        Assert.Equal(new AssetId(0x33333333), asset.MovePointId);
        Assert.Equal(new AssetId(0x44444444), asset.TaskWidgetPrimeId);
        Assert.Equal(new AssetId(0x55555555), asset.TaskWidgetSecondId);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Physical.ModelId);
    }

    [Fact]
    public void Read_Villain_UnderBfbb_LeavesIncrediblesFieldsAtDefault()
    {
        var asset = (VillainAsset)Read(BfbbData());

        Assert.Equal(default, asset.NavigationMeshId);
        Assert.Equal(default, asset.SettingsId);
    }

    [Fact]
    public void Read_Villain_UnderIncredibles_PopulatesIncrediblesFields()
    {
        var profile = IncrediblesSerializer.DefaultProfile;
        var asset = (VillainAsset)Read(IncrediblesData(), profile);

        Assert.Equal(0x00000001, asset.NpcFlags);
        Assert.Equal(new AssetId(0x11111111), asset.NpcModelId);
        Assert.Equal(new AssetId(0x22222222), asset.NpcSettingsId);
        Assert.Equal(new AssetId(0x33333333), asset.MovePointId);
        Assert.Equal(new AssetId(0x44444444), asset.TaskWidgetPrimeId);
        Assert.Equal(new AssetId(0x55555555), asset.TaskWidgetSecondId);
        Assert.Equal(new AssetId(0x66666666), asset.NavigationMeshId);
        Assert.Equal(new AssetId(0x77777777), asset.SettingsId);
    }

    [Fact]
    public void Read_ThenWrite_VillainUnderBfbb_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. BfbbData(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_VillainUnderIncredibles_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. IncrediblesData(linkCount: 1),
            .. LinkBytes(5, 6, 0xDEADBEEF),
        ];
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_VillainWithoutAnimListIdOrTaskWidgetSecondId_ReproducesInputBytes()
    {
        // BFBB's leftover gl/Working and gl/New Folder archives predate AnimListId and
        // TaskWidgetSecondId - see BuildProfiles.json's "bfbb/**/gl/Working/**"/
        // "bfbb/**/gl/New Folder/**" entries.
        byte[] data =
        [
            .. Prefix(linkCount: 0),
            .. EntityPrefix(hasPadding: true, hasAnimListId: false),
            .. I32(0x00000001),     // NpcFlags
            .. U32(0x11111111),     // NpcModelId
            .. U32(0x22222222),     // NpcSettingsId
            .. U32(0x33333333),     // MovePointId
            .. U32(0x44444444),     // TaskWidgetPrimeId
        ];
        var profile = BFBBSerializer.DefaultProfile with { EntityHasAnimListId = false, VillainHasTaskWidgetSecondId = false };

        var asset = (VillainAsset)Read(data, profile);

        Assert.Equal(new AssetId(0x44444444), asset.TaskWidgetPrimeId);
        Assert.Equal(default, asset.TaskWidgetSecondId);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThenWrite_VillainWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_Villain_UnderTSSM_DegradesToGenericEntityAsset()
    {
        byte[] data = BfbbData();
        var profile = TSSMSerializer.DefaultProfile;

        var asset = Read(data, profile);

        Assert.IsNotType<VillainAsset>(asset);
        Assert.IsType<EntityAsset>(asset, exactMatch: false);
    }

    [Fact]
    public void Read_Villain_UnderN100F_DegradesToGenericEntityAsset()
    {
        byte[] data = BfbbData();
        var profile = N100FSerializer.DefaultProfile;

        var asset = Read(data, profile);

        Assert.IsNotType<VillainAsset>(asset);
        Assert.IsType<EntityAsset>(asset, exactMatch: false);
    }

    [Fact]
    public void Read_ThenWrite_VillainUnderXbox_LittleEndian_ReproducesInputBytes()
    {
        var profile = BFBBSerializer.DefaultProfile with { Platform = Platform.Xbox };
        // Re-encode the data in little-endian for Xbox
        byte[] data =
        [
            0x34, 0x12, 0x00, 0x00, // BaseId (little-endian)
            0x2B,                   // BaseType
            0x00,                   // LinkCount
            0x1D, 0x00,             // BaseFlags (little-endian)
            0x01, 0x00, 0x00, 0x00, // EntityFlags, Subtype, PFlags, CollisionFlags
            .. new byte[4],         // BFBB padding
            0x00, 0x00, 0x00, 0x00, // SurfaceId
            .. new byte[36],        // Angle/Position/Scale
            .. new byte[16],        // ColorMultiplier
            0x00, 0x00, 0x7F, 0x43, // SeeThroughSpeed = 255 (little-endian float)
            0xDD, 0xCC, 0xBB, 0xAA, // ModelId (little-endian)
            0x00, 0x00, 0x00, 0x00, // AnimListId
            0x01, 0x00, 0x00, 0x00, // NpcFlags (little-endian)
            0x11, 0x11, 0x11, 0x11, // NpcModelId
            0x22, 0x22, 0x22, 0x22, // NpcSettingsId
            0x33, 0x33, 0x33, 0x33, // MovePointId
            0x44, 0x44, 0x44, 0x44, // TaskWidgetPrimeId
            0x55, 0x55, 0x55, 0x55, // TaskWidgetSecondId
        ];

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void ModelId_SetThroughIHasModel_ProjectsOntoPhysicalModelId()
    {
        var asset = new VillainAsset();

        ((IHasModel)asset).ModelId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), asset.Physical.ModelId);
    }

    [Fact]
    public void IsGrabbable_SetThroughIGrabbable_ReflectsInCollisionFlags()
    {
        var asset = new VillainAsset();

        ((IGrabbable)asset).IsGrabbable = true;

        Assert.True(((IGrabbable)asset).IsGrabbable);
        Assert.True(asset.Physical.CollisionFlags.HasFlag(CollisionFlags.Grabbable));
    }

    [Fact]
    public void IsGrabbable_ClearThroughIGrabbable_DoesNotAffectOtherCollisionFlags()
    {
        var asset = new VillainAsset();
        asset.Physical.CollisionFlags = CollisionFlags.Grabbable | CollisionFlags.Hittable;

        ((IGrabbable)asset).IsGrabbable = false;

        Assert.False(((IGrabbable)asset).IsGrabbable);
        Assert.True(asset.Physical.CollisionFlags.HasFlag(CollisionFlags.Hittable));
    }

    [Fact]
    public void Write_NonVillainAssetWithVillainType_FallsBackToRuntimeShape()
    {
        // A GenericEntityAsset carrying AssetType.Villain should write via its own shape's handler,
        // not the typed VillainAsset writer (which would throw on the cast).
        var generic = new GenericEntityAsset(AssetType.Villain);
        generic.SetUnparsedTail([0xDE, 0xAD, 0xBE, 0xEF]);

        // GenericEntityAsset writes: BaseAssetPrefix + EntityAssetPrefix + unparsed tail
        // Just assert it doesn't throw and that the tail is included.
        var written = Write(generic);
        Assert.True(written.AsSpan().IndexOf(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }) >= 0);
    }

    [Fact]
    public void Read_Villain_LinksArePreserved()
    {
        byte[] data =
        [
            .. BfbbData(linkCount: 2),
            .. LinkBytes(10, 20, 0xAABBCCDD),
            .. LinkBytes(30, 40, 0x11223344),
        ];
        var profile = BFBBSerializer.DefaultProfile;

        var asset = (VillainAsset)Read(data, profile);

        Assert.Equal(2, asset.Links.Count);
        Assert.Equal(10, asset.Links[0].SourceEvent);
        Assert.Equal(20, asset.Links[0].DestinationEvent);
        Assert.Equal(new AssetId(0xAABBCCDD), asset.Links[0].DestinationAssetId);
        Assert.Equal(30, asset.Links[1].SourceEvent);
        Assert.Equal(40, asset.Links[1].DestinationEvent);
        Assert.Equal(new AssetId(0x11223344), asset.Links[1].DestinationAssetId);
    }
}
