using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;
using static EvilHop.Assets.FlyAsset;

namespace EvilHop.Tests.Serialization;

public class FlyAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor(Serializer serializer)
    {
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Fly;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null, Serializer? serializer = null)
    {
        profile ??= BFBBSerializer.DefaultProfile;
        serializer ??= new BFBBSerializer();
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

    // FlyKey entries are always little-endian, even under GameCube's big-endian profile - see
    // FlyAsset.LittleEndianReader/LittleEndianWriter. Every fixture below is built little-endian
    // regardless of which profile a test reads it under.
    private static byte[] Vec3(Vector3 v) =>
    [
        .. BitConverter.GetBytes(v.X),
        .. BitConverter.GetBytes(v.Y),
        .. BitConverter.GetBytes(v.Z),
    ];

    private static byte[] KeyBytes(int frame, Vector3 right, Vector3 up, Vector3 at, Vector3 position, Vector2 aperture, float focalLength) =>
    [
        .. BitConverter.GetBytes(frame),
        .. Vec3(right),
        .. Vec3(up),
        .. Vec3(at),
        .. Vec3(position),
        .. BitConverter.GetBytes(aperture.X),
        .. BitConverter.GetBytes(aperture.Y),
        .. BitConverter.GetBytes(focalLength),
    ];

    private static byte[] Key1 { get; } = KeyBytes(
        frame: 1,
        right: new Vector3(1, 0, 0),
        up: new Vector3(0, 1, 0),
        at: new Vector3(0, 0, 1),
        position: new Vector3(12.5f, -6.25f, 200.0f),
        aperture: new Vector2(0.975f, 0.7305f),
        focalLength: 26.75f);

    private static byte[] Key2 { get; } = KeyBytes(
        frame: 2,
        right: new Vector3(0.996f, 0, -0.087f),
        up: new Vector3(0, 1, 0),
        at: new Vector3(0.087f, 0, 0.996f),
        position: new Vector3(13.0f, -6.25f, 199.5f),
        aperture: new Vector2(0.975f, 0.7305f),
        focalLength: 26.75f);

    [Fact]
    public void Read_Fly_ProducesFlyAsset() =>
        Assert.IsType<FlyAsset>(Read(Key1));

    [Fact]
    public void Read_Fly_UnderGameCubeProfile_PopulatesKeyFields()
    {
        var asset = (FlyAsset)Read(Key1);

        var key = Assert.Single(asset.Keys);
        Assert.Equal(1, key.Frame);
        Assert.Equal(new Vector3(1, 0, 0), key.Right);
        Assert.Equal(new Vector3(0, 1, 0), key.Up);
        Assert.Equal(new Vector3(0, 0, 1), key.At);
        Assert.Equal(new Vector3(12.5f, -6.25f, 200.0f), key.Position);
        Assert.Equal(new Vector2(0.975f, 0.7305f), key.Aperture);
        Assert.Equal(26.75f, key.FocalLength);
    }

    [Fact]
    public void Read_Fly_PopulatesEveryKey()
    {
        var asset = (FlyAsset)Read([.. Key1, .. Key2]);

        Assert.Equal(2, asset.Keys.Count);
        Assert.Equal(1, asset.Keys[0].Frame);
        Assert.Equal(2, asset.Keys[1].Frame);
    }

    [Fact]
    public void Read_ThenWrite_Fly_ReproducesInputBytes()
    {
        byte[] data = [.. Key1, .. Key2];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_FlyWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Key1, 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_Fly_UnderROTU_DegradesToGenericAsset()
    {
        var asset = Read(Key1, ROTUSerializer.DefaultProfile, new ROTUSerializer());

        Assert.IsNotType<FlyAsset>(asset);
        Assert.Equal(Key1, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Write_FlyAsset_UnderROTU_StillWritesItsOwnFields()
    {
        var asset = new FlyAsset { Type = AssetType.Fly };
        asset.Keys.Add(new Key
        {
            Frame = 1,
            Right = new Vector3(1, 0, 0),
            Up = new Vector3(0, 1, 0),
            At = new Vector3(0, 0, 1),
            Position = new Vector3(12.5f, -6.25f, 200.0f),
            Aperture = new Vector2(0.975f, 0.7305f),
            FocalLength = 26.75f,
        });

        Assert.Equal(Key1, Write(asset, ROTUSerializer.DefaultProfile));
    }
}
