using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class TriggerAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Trigger;
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

    private static byte[] Prefix(byte linkCount) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        0x01,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EntityPrefix(bool hasPadding, byte subtype, bool hasAnimListId = true) =>
    [
        0x01, subtype, 0x00, 0x00,   // EntityFlags, Subtype, PFlags, CollisionFlags
        .. hasPadding ? new byte[4] : [],
        0x00, 0x00, 0x00, 0x00,      // SurfaceId
        .. new byte[36],             // Angle/Position/Scale
        .. new byte[16],             // ColorMultiplier
        0x00, 0x00, 0x00, 0x00,      // SeeThroughSpeed
        0x00, 0x00, 0x00, 0x00,      // ModelId
        .. hasAnimListId ? new byte[4] : [], // AnimListId
    ];

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Vec3(float x, float y, float z) => [.. F32(x), .. F32(y), .. F32(z)];
    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] TrigFields(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 direction, uint flags) =>
    [
        .. Vec3(p0.X, p0.Y, p0.Z),
        .. Vec3(p1.X, p1.Y, p1.Z),
        .. Vec3(p2.X, p2.Y, p2.Z),
        .. Vec3(p3.X, p3.Y, p3.Z),
        .. Vec3(direction.X, direction.Y, direction.Z),
        .. U32(flags),
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

    private static readonly Vector3 UsualDirection = new(0f, -0f, 1f);

    private static byte[] BoxData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: true, subtype: 0),
        .. TrigFields(new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9), new Vector3(10, 11, 12), UsualDirection, flags: 0),
    ];

    private static byte[] SphereData(byte linkCount = 0) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(hasPadding: true, subtype: 1),
        .. TrigFields(new Vector3(1, 2, 3), new Vector3(5, 0, 0), Vector3.Zero, Vector3.Zero, UsualDirection, flags: 1),
    ];

    [Fact]
    public void Read_Trigger_ProducesTriggerAsset() =>
        Assert.IsType<TriggerAsset>(Read(BoxData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_Trigger_Box_PopulatesEveryField()
    {
        var asset = (TriggerAsset)Read(BoxData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(TriggerShape.Box, asset.Shape);
        Assert.Equal(new Vector3(1, 2, 3), asset.TriggerPosition0);
        Assert.Equal(new Vector3(4, 5, 6), asset.TriggerPosition1);
        Assert.Equal(new Vector3(7, 8, 9), asset.Physical.TriggerPosition2);
        Assert.Equal(new Vector3(10, 11, 12), asset.Physical.TriggerPosition3);
        Assert.Equal(UsualDirection, asset.Direction);
        Assert.Equal(TriggerFlags.None, asset.Flags);
    }

    [Fact]
    public void Read_Trigger_Sphere_PopulatesCenterRadiusAndFlags()
    {
        var asset = (TriggerAsset)Read(SphereData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(TriggerShape.Sphere, asset.Shape);
        Assert.Equal(new Vector3(1, 2, 3), asset.TriggerPosition0);
        Assert.Equal(5f, asset.TriggerPosition1.X);
        Assert.Equal(Vector3.Zero, asset.Physical.TriggerPosition2);
        Assert.Equal(Vector3.Zero, asset.Physical.TriggerPosition3);
        Assert.Equal(TriggerFlags.DirectionGate, asset.Flags);
    }

    [Fact]
    public void Shape_SetDirectly_ProjectsOntoPhysicalSubtype()
    {
        var asset = new TriggerAsset { Shape = TriggerShape.Sphere };

        Assert.Equal(1, asset.Physical.Subtype);
    }

    [Fact]
    public void Read_ThenWrite_TriggerBoxUnderBfbb_ReproducesInputBytes()
    {
        byte[] data = BoxData();
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_TriggerSphereUnderN100F_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(0),
            .. EntityPrefix(hasPadding: false, subtype: 1),
            .. TrigFields(new Vector3(1, 2, 3), new Vector3(5, 0, 0), Vector3.Zero, Vector3.Zero, UsualDirection, flags: 0),
        ];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_RealN100FPrototypeExemplar_RoundTripsExactly()
    {
        // From FOO2.HIP: AHDR id=0xB0F021A9 "TRIG", size=124, n100f/prototype_2001-06-11/PS2. This
        // build's triggers are just the four positions (no Direction/Flags), and its links are 24
        // bytes (no ParamWidgetAssetId/CheckAssetId).
        byte[] data =
        [
            0xA9, 0x21, 0xF0, 0xB0, 0x01, 0x01, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Angle
            0x47, 0x46, 0x47, 0x42, 0x0D, 0x35, 0x33, 0x42, 0x68, 0x01, 0xA6, 0x42, // Position
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Scale
            0xD9, 0x01, 0xBB, 0x15, // ModelId
            0xE2, 0x0C, 0x43, 0x42, 0x45, 0x74, 0x30, 0x42, 0x51, 0xE3, 0xA0, 0x42, // TriggerPosition0
            0x31, 0x10, 0x4C, 0x42, 0x5A, 0x1D, 0x41, 0x42, 0xBD, 0x46, 0xD9, 0x42, // TriggerPosition1
            0xCB, 0xCD, 0xCD, 0xCD, 0xCC, 0xCD, 0xCD, 0xCD, 0xCA, 0xCD, 0xCD, 0xCD, // TriggerPosition2
            0xCB, 0xCD, 0xCD, 0xCD, 0xCC, 0xCD, 0xCD, 0xCD, 0xCA, 0xCD, 0xCD, 0xCD, // TriggerPosition3
            0x06, 0x00, 0x01, 0x00, 0x84, 0x9C, 0xC0, 0x85, // Link: SourceEvent, DestEvent, DestAssetId
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x40, // Params
        ];

        var profile = N100FSerializer.DefaultProfile with
        {
            Platform = Platform.PlayStation2,
            StreamDataHasPaddingField = false,
            EntityHasExtendedFields = false,
            LinkHasExtendedFields = false,
            TriggerHasDirectionAndFlags = false,
        };

        var asset = (TriggerAsset)Read(data, profile);

        Assert.Equal(new Vector3(0f, 0f, 0f), asset.Direction);
        Assert.Equal(TriggerFlags.None, asset.Flags);
        Assert.Single(asset.Links);
        Assert.Equal(6, asset.Links[0].SourceEvent);
        Assert.Equal(1, asset.Links[0].DestinationEvent);
        Assert.Equal(new AssetId(0x85C09C84), asset.Links[0].DestinationAssetId);
        Assert.Equal(new AssetId(0), asset.Links[0].ParamWidgetAssetId);
        Assert.Equal(new AssetId(0), asset.Links[0].CheckAssetId);

        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThenWrite_TriggerWithoutAnimListId_ReproducesInputBytes()
    {
        // BFBB's leftover gl/Working and gl/New Folder archives predate AnimListId. See
        // BuildProfiles.json's "bfbb/**/gl/Working/**"/"bfbb/**/gl/New Folder/**" entries.
        byte[] data =
        [
            .. Prefix(linkCount: 1),
            .. EntityPrefix(hasPadding: true, subtype: 0, hasAnimListId: false),
            .. TrigFields(new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9), new Vector3(10, 11, 12), UsualDirection, flags: 0),
            .. LinkBytes(1, 2, 0xAABBCCDD),
        ];
        var profile = BFBBSerializer.DefaultProfile with { EntityHasAnimListId = false };

        var asset = (TriggerAsset)Read(data, profile);

        Assert.Equal(UsualDirection, asset.Direction);
        Assert.Equal(TriggerFlags.None, asset.Flags);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThenWrite_TriggerWithLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. BoxData(linkCount: 2),
            .. LinkBytes(1, 2, 0xAABBCCDD),
            .. LinkBytes(3, 4, 0x11223344),
        ];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_TriggerWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BoxData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }
}
