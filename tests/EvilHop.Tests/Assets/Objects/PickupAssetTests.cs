using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class PickupAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new BFBBSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Pickup;
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
        0x04,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] EntityPrefix(byte subtype, bool hasPadding, bool hasAnimListId = true) =>
    [
        0x01, subtype, 0x00, 0x02, // EntityFlags, Subtype, PFlags, CollisionFlags
        .. hasPadding ? new byte[4] : [],
        0x00, 0x00, 0x00, 0x00,   // SurfaceId
        .. new byte[36],          // Angle/Position/Scale
        .. new byte[16],          // ColorMultiplier
        0x43, 0x7F, 0x00, 0x00,   // SeeThroughSpeed = 255
        0x94, 0xE2, 0x54, 0x63,   // ModelId = pickups.MINF
        .. hasAnimListId ? new byte[4] : [], // AnimListId
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

    private static byte[] Data(byte subtype, uint pickupHash, short flags, short pickupValue, byte linkCount = 0, bool hasPadding = true, bool hasAnimListId = true) =>
    [
        .. Prefix(linkCount),
        .. EntityPrefix(subtype, hasPadding, hasAnimListId),
        (byte)(pickupHash >> 24), (byte)(pickupHash >> 16), (byte)(pickupHash >> 8), (byte)pickupHash,
        (byte)(flags >> 8), (byte)flags,
        (byte)(pickupValue >> 8), (byte)pickupValue,
    ];

    [Fact]
    public void Read_Pickup_ProducesPickupAsset() =>
        Assert.IsType<PickupAsset>(Read(Data(0x13, 0, 0, 4)));

    [Fact]
    public void Read_Pickup_PopulatesEveryField()
    {
        var asset = (PickupAsset)Read(Data(0x13, 0x28F55613, 2, 4));

        Assert.Equal(PickupKind.Underwear, asset.Kind);
        Assert.Equal(0x28F55613u, asset.PickupHash);
        Assert.Equal(PickupFlags.EnabledOnStart, asset.Flags);
        Assert.Equal(4, asset.PickupValue);
        Assert.Equal(new AssetId(0x94E25463), asset.Physical.ModelId);
    }

    [Fact]
    public void Read_ThenWrite_Pickup_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(0x13, 0x28F55613, 2, 4, linkCount: 1),
            .. LinkBytes(1, 2, 0x55667788),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_PickupWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(0x13, 0, 0, 4), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_PickupUnderTSSM_NoEntityPadding_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Data(0xB7, 0x60F808B7, 0, 4, linkCount: 1, hasPadding: false),
            .. LinkBytes(0x23, 8, 0x09B80F4A),
        ];
        var profile = TSSMSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_PickupWithoutAnimListId_ReproducesInputBytes()
    {
        // BFBB's leftover gl/Working and gl/New Folder archives predate AnimListId - see
        // BuildProfiles.json's "bfbb/**/gl/Working/**"/"bfbb/**/gl/New Folder/**" entries.
        byte[] data =
        [
            .. Data(0x13, 0x28F55613, 2, 4, linkCount: 1, hasAnimListId: false),
            .. LinkBytes(1, 2, 0x55667788),
        ];
        var profile = BFBBSerializer.DefaultProfile with { EntityHasAnimListId = false };

        var asset = (PickupAsset)Read(data, profile);

        Assert.Equal(0x28F55613u, asset.PickupHash);
        Assert.Equal(PickupFlags.EnabledOnStart, asset.Flags);
        Assert.Equal(4, asset.PickupValue);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_Pickup_UnderIncredibles_DegradesToGenericEntityAsset()
    {
        byte[] data = Data(0x13, 0, 0, 4, hasPadding: false);

        var asset = Read(data, IncrediblesSerializer.DefaultProfile);

        Assert.IsNotType<PickupAsset>(asset);
    }

    [Fact]
    public void Kind_SetThroughSubtype_RoundTripsUnnamedValues()
    {
        // Real TSSM data carries a Subtype (0xB7) not in the wiki's known-kind table.
        var asset = (PickupAsset)Read(Data(0xB7, 0, 0, 4, hasPadding: false), TSSMSerializer.DefaultProfile);

        Assert.Equal((PickupKind)0xB7, asset.Kind);
        Assert.Equal(0xB7, asset.Physical.Subtype);
    }

    [Fact]
    public void Read_ThenWrite_RealBFBBExemplar_ReproducesInputBytes()
    {
        // bfbb/prototype_2003-10-01/GC/NTSC-U/US/b1/b101.HIP, AHDR id=0x9426EFC0
        byte[] data =
        [
            0x94, 0x26, 0xEF, 0xC0, 0x04, 0x00, 0x00, 0x1D,
            0x01, 0x13, 0x00, 0x02,
            .. new byte[4], // BFBB entity padding
            0x00, 0x00, 0x00, 0x00, // SurfaceId
            .. new byte[12],        // Angle
            0xC1, 0x01, 0x52, 0x35, 0x3E, 0xA5, 0xB6, 0xC3, 0xC1, 0x0C, 0x89, 0x03, // Position
            0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00, // Scale
            0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00, // ColorMultiplier
            0x43, 0x7F, 0x00, 0x00, // SeeThroughSpeed = 255
            0x94, 0xE2, 0x54, 0x63, // ModelId = pickups.MINF
            0x00, 0x00, 0x00, 0x00, // AnimListId
            0x28, 0xF5, 0x56, 0x13, // pickupHash
            0x00, 0x02,             // pickupFlags = EnabledOnStart
            0x00, 0x04,             // pickupValue
        ];

        var asset = (PickupAsset)Read(data);

        Assert.Equal(new AssetId(0x9426EFC0), asset.Physical.BaseId);
        Assert.Equal(0x04, asset.Physical.BaseType);
        Assert.Equal(PickupKind.Underwear, asset.Kind);
        Assert.Equal(new AssetId(0x94E25463), asset.Physical.ModelId);
        Assert.Equal(0x28F55613u, asset.PickupHash);
        Assert.Equal(PickupFlags.EnabledOnStart, asset.Flags);
        Assert.Equal(4, asset.PickupValue);

        Assert.Equal(data, Write(asset));
    }
}
