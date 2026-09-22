using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;
using static EvilHop.Assets.LightAsset;

namespace EvilHop.Tests.Serialization;

public class LightAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Light;
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
        0x25,                   // BaseType
        linkCount,
        0x00, 0x1D,             // BaseFlags
    ];

    private static byte[] UInt32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Single(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Vector(float x, float y, float z) => [.. Single(x), .. Single(y), .. Single(z)];

    private static byte[] LinkBytes(short sourceEvent, short destinationEvent, uint destinationAssetId) =>
    [
        (byte)(sourceEvent >> 8), (byte)sourceEvent,
        (byte)(destinationEvent >> 8), (byte)destinationEvent,
        .. UInt32(destinationAssetId),
        .. new byte[16], // Params
        .. new byte[4],  // ParamWidgetAssetId
        .. new byte[4],  // CheckAssetId
    ];

    private static byte[] Body(
        byte lightType, byte lightEffect, uint flags, float r, float g, float b, float a,
        Vector3 direction, float coneAngle, Vector3 position, float radius, uint attachedEntityId) =>
    [
        lightType, lightEffect, 0x00, 0x00, // lightType/lightEffect/padding
        .. UInt32(flags),
        .. Single(r), .. Single(g), .. Single(b), .. Single(a),
        .. Vector(direction.X, direction.Y, direction.Z),
        .. Single(coneAngle),
        .. Vector(position.X, position.Y, position.Z),
        .. Single(radius),
        .. UInt32(attachedEntityId),
    ];

    private static byte[] SampleData() =>
    [
        .. Prefix(0),
        .. Body(3, 4, 0x28, 0.98f, 0.69f, 0.016f, 1.0f, new Vector3(0.0f, 0.0f, 1.0f), 45.0f, new Vector3(3.4f, 2.6f, -0.8f), 6.0f, 0),
    ];

    [Fact]
    public void Read_Light_UnderN100F_ProducesLightAsset() =>
        Assert.IsType<LightAsset>(Read(SampleData(), N100FSerializer.DefaultProfile));

    [Fact]
    public void Read_Light_PopulatesEveryField()
    {
        var asset = (LightAsset)Read(SampleData(), N100FSerializer.DefaultProfile);

        Assert.Equal(Shape.Point3, asset.Kind);
        Assert.Equal(Pattern.FlickerErratic, asset.Effect);
        Assert.Equal(Behavior.On | Behavior.Environment, asset.Flags);
        Assert.Equal(new Rgba(0.98f, 0.69f, 0.016f, 1.0f), asset.Color);
        Assert.Equal(new Vector3(0.0f, 0.0f, 1.0f), asset.Direction);
        Assert.Equal(45.0f, asset.ConeAngle);
        Assert.Equal(new Vector3(3.4f, 2.6f, -0.8f), asset.Position);
        Assert.Equal(6.0f, asset.Radius);
        Assert.Equal(AssetId.None, asset.AttachedEntityId);
    }

    [Fact]
    public void Read_Light_LinkCountKeepsDerivingAfterLinksAreMutated()
    {
        byte[] data =
        [
            .. Prefix(1),
            .. Body(0, 0, 0, 0, 0, 0, 0, Vector3.Zero, 0, Vector3.Zero, 0, 0),
            .. LinkBytes(0, 0, 0),
        ];

        var asset = (LightAsset)Read(data, N100FSerializer.DefaultProfile);
        Assert.Equal(1, asset.Physical.LinkCount);

        asset.Links.Add(new Link());

        Assert.Equal(2, asset.Physical.LinkCount);
    }

    [Fact]
    public void Read_ThenWrite_Light_ReproducesInputBytes()
    {
        byte[] data = SampleData();
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LightWithAttachedEntityAndLinks_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(1),
            .. Body(1, 17, 0x28, 1.0f, 1.0f, 1.0f, 1.0f, new Vector3(0.0f, 1.0f, 0.0f), 90.0f, Vector3.Zero, 4.0f, 0xAABBCCDD),
            .. LinkBytes(1, 2, 0x11223344),
        ];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LightWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. SampleData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_Light_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = SampleData();

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<LightAsset>(asset);
        Assert.Equal(data[8..], asset.GetUnparsedTail().ToArray());
    }
}
