using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class LightKitAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.LightKit;
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

    private static byte[] U32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Vec3(float x, float y, float z) => [.. F32(x), .. F32(y), .. F32(z)];

    private static byte[] Header(uint groupId, uint lightCount) =>
    [
        .. U32(0x54494B4C), // tagID = "TIKL"
        .. U32(groupId),
        .. U32(lightCount),
        .. U32(0), // lightList pointer slot
    ];

    private static readonly byte[] BlendedPlaceholder = [0xCD, 0xCD, 0xCD, 0xCD];

    private static byte[] LightEntry(uint type, float r, float g, float b, float a,
        (float, float, float) right, (float, float, float) up, (float, float, float) at,
        (float, float, float) position, float positionW, float radius, float angle) =>
    [
        .. U32(type),
        .. F32(r), .. F32(g), .. F32(b), .. F32(a),
        .. Vec3(right.Item1, right.Item2, right.Item3), .. F32(0), // right, right.w
        .. Vec3(up.Item1, up.Item2, up.Item3), .. F32(0),          // up, up.w
        .. Vec3(at.Item1, at.Item2, at.Item3), .. F32(0),          // at, at.w
        .. Vec3(position.Item1, position.Item2, position.Item3), .. F32(positionW),
        .. F32(radius),
        .. F32(angle),
        .. U32(0), // platLight pointer slot
    ];

    private static byte[] AmbientLight(float r, float g, float b) =>
        LightEntry(1, r, g, b, 1, (0, 0, 0), (0, 0, 0), (0, 0, 0), (0, 0, 0), 0, 0, 0);

    private static byte[] DirectionalLight(float r, float g, float b, (float, float, float) at) =>
        LightEntry(2, r, g, b, 1, (1, 0, 0), (0, 1, 0), at, (0, 0, 0), 1, 0, 0);

    private static byte[] BfbbData() =>
    [
        .. Header(0, 2),
        .. AmbientLight(0.25f, 0.25f, 0.25f),
        .. DirectionalLight(0.75f, 0.75f, 0.75f, (0.0f, -1.0f, 0.0f)),
    ];

    private static byte[] RotuData() =>
    [
        .. Header(0x11111111, 1),
        .. BlendedPlaceholder,
        .. AmbientLight(0.5f, 0.5f, 0.5f),
    ];

    [Fact]
    public void Read_LightKit_ProducesLightKitAsset() =>
        Assert.IsType<LightKitAsset>(Read(BfbbData(), BFBBSerializer.DefaultProfile));

    [Fact]
    public void Read_LightKit_UnderBfbb_PopulatesEveryField()
    {
        var asset = (LightKitAsset)Read(BfbbData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(default, asset.GroupId);
        Assert.Equal(2, asset.Lights.Count);

        var ambient = asset.Lights[0];
        Assert.Equal(LightKitLightType.Ambient, ambient.Type);
        Assert.Equal(new RgbaColor(0.25f, 0.25f, 0.25f, 1f), ambient.Color);
        Assert.Equal(default, ambient.Right);
        Assert.Equal(default, ambient.Position);
        Assert.Equal(0f, ambient.PositionW);

        var directional = asset.Lights[1];
        Assert.Equal(LightKitLightType.Directional, directional.Type);
        Assert.Equal(new RgbaColor(0.75f, 0.75f, 0.75f, 1f), directional.Color);
        Assert.Equal(new Vector3(1, 0, 0), directional.Right);
        Assert.Equal(new Vector3(0, 1, 0), directional.Up);
        Assert.Equal(new Vector3(0, -1, 0), directional.At);
        Assert.Equal(1f, directional.PositionW);
    }

    [Fact]
    public void Read_LightKit_UnderROTU_PopulatesGroupIdAndIgnoresBlended()
    {
        var asset = (LightKitAsset)Read(RotuData(), ROTUSerializer.DefaultProfile);

        Assert.Equal(new AssetId(0x11111111), asset.GroupId);
        Assert.Single(asset.Lights);
    }

    [Fact]
    public void Read_ThenWrite_LightKitUnderBfbb_ReproducesInputBytes()
    {
        byte[] data = BfbbData();
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LightKitUnderROTU_ReproducesInputBytes()
    {
        byte[] data = RotuData();
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LightKitUnderRatatouille_ReproducesInputBytes()
    {
        byte[] data = RotuData();
        var profile = RatatouilleSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LightKitWithNoLights_ReproducesInputBytes()
    {
        byte[] data = Header(0, 0);
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_LightKitWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbData(), 0xDE, 0xAD, 0xBE, 0xEF];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_LightKit_UnderN100F_DegradesToGenericAsset()
    {
        byte[] data = BfbbData();

        var asset = Read(data, N100FSerializer.DefaultProfile);

        Assert.IsNotType<LightKitAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void LightCount_DisagreeingWithLights_IsStoredIndependently()
    {
        var asset = new LightKitAsset();
        asset.Lights.Add(new LightKitLight());

        asset.Physical.LightCount = 5;

        Assert.Equal(5u, asset.Physical.LightCount);
        Assert.Single(asset.Lights);
    }

    [Fact]
    public void LightCount_MatchingLights_DerivesFromLights()
    {
        var asset = new LightKitAsset();
        asset.Lights.Add(new LightKitLight());
        asset.Lights.Add(new LightKitLight());

        asset.Physical.LightCount = 2;
        asset.Lights.Add(new LightKitLight());

        Assert.Equal(3u, asset.Physical.LightCount);
    }

    [Fact]
    public void TagId_DefaultsToTikl() =>
        Assert.Equal(0x54494B4Cu, new LightKitAsset().Physical.TagId);
}
