using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class DuplicatorAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Duplicator;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= IncrediblesSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= IncrediblesSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] U16(ushort value) => [(byte)(value >> 8), (byte)value];

    private static byte[] U32(uint value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] BaseHeader(uint id, byte baseType, byte linkCount) =>
    [
        .. U32(id),
        baseType,
        linkCount,
        0x00, 0x1C, // BaseFlags
    ];

    private static byte[] SpawnFields(ushort initialSpawn, ushort maxInGame, ushort maxToSpawn, float spawnRate) =>
    [
        .. U16(initialSpawn), .. U16(maxInGame), .. U16(maxToSpawn),
        0x00, 0x00, // padding
        .. F32(spawnRate),
    ];

    private static byte[] NpcBlock(uint npcFlags, uint npcModelId, uint movePointId, uint navigationMeshId, uint settingsId) =>
    [
        .. U32(npcFlags), .. U32(npcModelId),
        .. new byte[4],  // NpcSettingsId
        .. U32(movePointId),
        .. new byte[8],  // TaskWidgetPrimeId, TaskWidgetSecondId
        .. U32(navigationMeshId), .. U32(settingsId),
    ];

    private static byte[] EntityPrefix() =>
    [
        0x01, 0x00, 0x00, 0x02,   // EntityFlags, Subtype, PFlags, CollisionFlags
        0x00, 0x00, 0x00, 0x00,   // SurfaceId
        .. new byte[12],          // Angle
        .. F32(1f), .. F32(2f), .. F32(3f), // Position
        .. new byte[12],          // Scale
        .. new byte[16],          // ColorMultiplier
        0x43, 0x7F, 0x00, 0x00,   // SeeThroughSpeed = 255
        0xAA, 0xBB, 0xCC, 0xDD,   // ModelId
        0x00, 0x00, 0x00, 0x00,   // AnimListId
    ];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        .. U32(destinationAssetId),
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] Data(byte linkCount = 0, byte templateLinkCount = 0, uint templateId = 0x12345678) =>
    [
        .. BaseHeader(0x12345678, 0x42, linkCount),
        .. SpawnFields(initialSpawn: 2, maxInGame: 5, maxToSpawn: 10, spawnRate: 0.5f),
        .. NpcBlock(npcFlags: 0, npcModelId: 0, movePointId: 0, navigationMeshId: 0x9FD748C1, settingsId: 0),
        .. BaseHeader(templateId, 0x2B, templateLinkCount),
        .. EntityPrefix(),
        .. NpcBlock(npcFlags: 1, npcModelId: 0xAABBCCDD, movePointId: 0x11223344, navigationMeshId: 0x9FD748C1, settingsId: 0x7DA0E410),
    ];

    [Fact]
    public void Read_Duplicator_ProducesDuplicatorAsset() =>
        Assert.IsType<DuplicatorAsset>(Read(Data()));

    [Fact]
    public void Read_Duplicator_PopulatesSpawnFields()
    {
        var asset = (DuplicatorAsset)Read(Data());

        Assert.Equal(2, asset.InitialSpawn);
        Assert.Equal(5, asset.MaxInGame);
        Assert.Equal(10, asset.MaxToSpawn);
        Assert.Equal(0.5f, asset.SpawnRate);
    }

    [Fact]
    public void Read_Duplicator_PopulatesTemplate()
    {
        var template = ((DuplicatorAsset)Read(Data())).Template;

        Assert.Equal(EntityFlags.Visible, template.EntityFlags);
        Assert.Equal(new Vector3(1f, 2f, 3f), template.Position);
        Assert.Equal(new AssetId(0xAABBCCDD), template.Physical.ModelId);
        Assert.Equal(1, template.NpcFlags);
        Assert.Equal(new AssetId(0xAABBCCDD), template.NpcModelId);
        Assert.Equal(new AssetId(0x11223344), template.MovePointId);
        Assert.Equal(new AssetId(0x9FD748C1), template.NavigationMeshId);
        Assert.Equal(new AssetId(0x7DA0E410), template.SettingsId);
    }

    [Fact]
    public void Read_Duplicator_PopulatesAvatar()
    {
        var avatar = ((DuplicatorAsset)Read(Data())).Physical.Avatar;

        Assert.Equal(AssetId.None, avatar.NpcModelId);
        Assert.Equal(new AssetId(0x9FD748C1), avatar.NavigationMeshId);
    }

    [Fact]
    public void Read_Duplicator_EmbeddedHeaderAgreeingWithOuter_DerivesTemplateHeader()
    {
        var asset = (DuplicatorAsset)Read([.. Data(linkCount: 1, templateLinkCount: 1), .. LinkBytes(0x26F, 0x0B, 0xDED5D8C4)]);

        asset.BaseFlags = BaseAssetFlags.Valid;
        asset.Links.Add(new Link());

        Assert.Equal(asset.Physical.BaseId, asset.Physical.TemplateBaseId);
        Assert.Equal(BaseAssetFlags.Valid, asset.Physical.TemplateBaseFlags);
        Assert.Equal(2, asset.Physical.TemplateLinkCount);
    }

    [Fact]
    public void Read_Duplicator_EmbeddedHeaderDisagreeingWithOuter_KeepsStoredTemplateHeader()
    {
        var asset = (DuplicatorAsset)Read(Data(templateLinkCount: 3, templateId: 0xCAFEF00D));

        Assert.Equal(new AssetId(0xCAFEF00D), asset.Physical.TemplateBaseId);
        Assert.Equal(3, asset.Physical.TemplateLinkCount);
        Assert.Equal(0x2B, asset.Physical.TemplateBaseType);
    }

    [Fact]
    public void Read_ThenWrite_DuplicatorWithLinks_ReproducesInputBytes()
    {
        byte[] data = [.. Data(linkCount: 2, templateLinkCount: 2), .. LinkBytes(0x26F, 0x0B, 0xDED5D8C4), .. LinkBytes(0x272, 0x0C, 0xDED5D8C4)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_DuplicatorWithDisagreeingEmbeddedHeader_ReproducesInputBytes()
    {
        byte[] data = Data(templateLinkCount: 3, templateId: 0xCAFEF00D);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_DuplicatorWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_Duplicator_UnderBFBB_DegradesToGenericAsset()
    {
        var asset = Read(Data(), BFBBSerializer.DefaultProfile);

        Assert.IsNotType<DuplicatorAsset>(asset, exactMatch: false);
    }

    [Fact]
    public void ModelId_SetThroughIHasModelOnTemplate_ProjectsOntoPhysicalModelId()
    {
        var template = new DuplicatorAsset.Villain();

        ((IHasModel)template).ModelId = new AssetId(0xDEADBEEF);

        Assert.Equal(new AssetId(0xDEADBEEF), template.Physical.ModelId);
    }

    [Fact]
    public void IsGrabbable_SetThroughIGrabbableOnTemplate_TogglesOnlyGrabbableBit()
    {
        var template = new DuplicatorAsset.Villain();
        template.Physical.CollisionFlags = CollisionFlags.PreciseCollision;

        ((IGrabbable)template).IsGrabbable = true;

        Assert.Equal(CollisionFlags.PreciseCollision | CollisionFlags.Grabbable, template.Physical.CollisionFlags);
    }

    [Fact]
    public void Read_ThenWrite_RealIncrediblesExemplar_ReproducesInputBytes()
    {
        // incredibles/prototype_2004-07-19/GC/NTSC-U/US/BM/bm01.HIP, AHDR id=0x24453A70
        byte[] data =
        [
            0x24, 0x45, 0x3A, 0x70, 0x42, 0x02, 0x00, 0x1C, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x3F, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x9F, 0xD7, 0x48, 0xC1,
            0x00, 0x00, 0x00, 0x00, 0x24, 0x45, 0x3A, 0x70, 0x2B, 0x02, 0x00, 0x1C, 0x01, 0x00, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x00, 0x40, 0xA1, 0xF7, 0x70, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0xC2, 0x12, 0xD1, 0x1A, 0x40, 0x92, 0x77, 0x9A, 0x41, 0xB7, 0xAF, 0xEC, 0x3F, 0x80, 0x00, 0x00,
            0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00,
            0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00, 0x43, 0x7F, 0x00, 0x00, 0xF7, 0x1F, 0x6F, 0x2E,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0xF7, 0x1F, 0x6F, 0x2E, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x9F, 0xD7, 0x48, 0xC1,
            0x7D, 0xA0, 0xE4, 0x10, 0x02, 0x6F, 0x00, 0x0B, 0xDE, 0xD5, 0xD8, 0xC4, 0x3F, 0x80, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x02, 0x72, 0x00, 0x0C, 0xDE, 0xD5, 0xD8, 0xC4, 0x3F, 0x80, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        ];

        var asset = (DuplicatorAsset)Read(data);

        Assert.Equal(1, asset.InitialSpawn);
        Assert.Equal(1, asset.MaxInGame);
        Assert.Equal(0, asset.MaxToSpawn);
        Assert.Equal(1f, asset.SpawnRate);
        Assert.Equal(new AssetId(0xF71F6F2E), asset.Template.NpcModelId);
        Assert.Equal(new AssetId(0x7DA0E410), asset.Template.SettingsId);
        Assert.Equal(2, asset.Links.Count);
        Assert.Equal(0x26F, asset.Links[0].SourceEvent);
        Assert.Equal(data, Write(asset));
    }
}
