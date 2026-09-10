using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class ReactiveAnimationAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new TSSMSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.ReactiveAnimation;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= TSSMSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= TSSMSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] Prefix(byte baseType = 0) =>
    [
        0x00, 0x00, 0x12, 0x34, // BaseId
        baseType,
        0x00,                   // LinkCount
        0x00, 0x00,             // BaseFlags
    ];

    private static byte[] Row(uint staticModel, uint boundModel, float lodDist, uint idleAnim, uint moveThroughAnim,
        uint hitAnim, uint idleSound, uint moveThroughSound, uint hitSound, uint burntModel, uint burnAnim,
        float burnFuel, float burnFlameSize, float burnEmitScale, float moveThroughRadius) =>
    [
        .. BitConverter.GetBytes(staticModel).Reverse(),
        .. BitConverter.GetBytes(boundModel).Reverse(),
        .. BitConverter.GetBytes(lodDist).Reverse(),
        .. BitConverter.GetBytes(idleAnim).Reverse(),
        .. BitConverter.GetBytes(moveThroughAnim).Reverse(),
        .. BitConverter.GetBytes(hitAnim).Reverse(),
        .. BitConverter.GetBytes(idleSound).Reverse(),
        .. BitConverter.GetBytes(moveThroughSound).Reverse(),
        .. BitConverter.GetBytes(hitSound).Reverse(),
        .. BitConverter.GetBytes(burntModel).Reverse(),
        .. BitConverter.GetBytes(burnAnim).Reverse(),
        .. BitConverter.GetBytes(burnFuel).Reverse(),
        .. BitConverter.GetBytes(burnFlameSize).Reverse(),
        .. BitConverter.GetBytes(burnEmitScale).Reverse(),
        .. BitConverter.GetBytes(moveThroughRadius).Reverse(),
    ];

    private static byte[] VersionAndRowCount(int version, int rowCount) =>
    [
        .. BitConverter.GetBytes(version).Reverse(),
        .. BitConverter.GetBytes(rowCount).Reverse(),
    ];

    [Fact]
    public void Read_ReactiveAnimation_ProducesReactiveAnimationAsset() =>
        Assert.IsType<ReactiveAnimationAsset>(Read([.. Prefix(), .. VersionAndRowCount(5, 0)]));

    [Fact]
    public void Read_ReactiveAnimation_PopulatesVersionAndRows()
    {
        byte[] data =
        [
            .. Prefix(),
            .. VersionAndRowCount(5, 1),
            .. Row(0x11111111, 0x22222222, 10.0f, 0x33333333, 0x44444444, 0x55555555, 0, 0, 0, 0, 0, 10.0f, 0.3f, 1.0f, 1.5f),
        ];

        var asset = (ReactiveAnimationAsset)Read(data);

        Assert.Equal(5, asset.Version);
        Assert.Single(asset.Rows);

        var row = asset.Rows[0];
        Assert.Equal(new AssetId(0x11111111), row.StaticModelId);
        Assert.Equal(new AssetId(0x22222222), row.BoundModelId);
        Assert.Equal(10.0f, row.LodDistance);
        Assert.Equal(new AssetId(0x33333333), row.IdleAnimationId);
        Assert.Equal(new AssetId(0x44444444), row.MoveThroughAnimationId);
        Assert.Equal(new AssetId(0x55555555), row.HitAnimationId);
        Assert.Equal(10.0f, row.BurnFuel);
        Assert.Equal(0.3f, row.BurnFlameSize);
        Assert.Equal(1.0f, row.BurnEmitScale);
        Assert.Equal(1.5f, row.MoveThroughRadius);
    }

    [Fact]
    public void Read_ReactiveAnimation_RowCountKeepsDerivingAfterRowsAreMutated()
    {
        byte[] data =
        [
            .. Prefix(),
            .. VersionAndRowCount(5, 1),
            .. Row(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        ];

        var asset = (ReactiveAnimationAsset)Read(data);
        Assert.Equal(1, asset.Physical.RowCount);

        asset.Rows.Add(new ReactiveAnimationRow());

        Assert.Equal(2, asset.Physical.RowCount);
    }

    [Fact]
    public void Read_ThenWrite_ReactiveAnimationWithNoRows_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(), .. VersionAndRowCount(5, 0)];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ReactiveAnimationWithRows_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(),
            .. VersionAndRowCount(5, 2),
            .. Row(0x11111111, 0x22222222, 10.0f, 0x33333333, 0x44444444, 0x55555555, 0, 0, 0, 0, 0, 10.0f, 0.3f, 1.0f, 1.5f),
            .. Row(0x66666666, 0x77777777, 20.0f, 0x88888888, 0x99999999, 0xAAAAAAAA, 0xBBBBBBBB, 0xCCCCCCCC, 0xDDDDDDDD, 0xEEEEEEEE, 0xFFFFFFFF, 5.0f, 0.5f, 2.0f, 3.0f),
        ];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_ReactiveAnimationWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Prefix(), .. VersionAndRowCount(5, 0), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ReactiveAnimation_UnderBFBB_DegradesToGenericBaseAsset()
    {
        byte[] data = [.. Prefix(), .. VersionAndRowCount(5, 0)];

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<ReactiveAnimationAsset>(asset);
    }

    [Fact]
    public void Read_ThenWrite_ReactiveAnimationUnderIncredibles_ReproducesInputBytes()
    {
        byte[] data =
        [
            .. Prefix(),
            .. VersionAndRowCount(5, 1),
            .. Row(0x11111111, 0x22222222, 10.0f, 0x33333333, 0x44444444, 0x55555555, 0, 0, 0, 0, 0, 10.0f, 0.3f, 1.0f, 1.5f),
        ];
        var profile = IncrediblesSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }
}
