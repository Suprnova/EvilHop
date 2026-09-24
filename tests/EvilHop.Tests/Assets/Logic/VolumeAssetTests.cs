using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;
using static EvilHop.Assets.VolumeAsset;

namespace EvilHop.Tests.Serialization;

public class VolumeAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new ROTUSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Volume;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= ROTUSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= ROTUSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] F32(float value) => [.. BitConverter.GetBytes(value).Reverse()];

    // rotu/prototype_2005-09-15/GC/NTSC-J/JP/DD/dd01.HIP, AHDR id=0x5F89B91A
    private static readonly byte[] ROTUExemplar =
    [
        0x5F, 0x89, 0xB9, 0x1A, 0x1D, 0x00, 0x00, 0x1D, 0x00, 0x00, 0x00, 0x00, .. new byte[32],
        0x02, 0x00, 0x00, 0x00, 0xC1, 0xBD, 0x84, 0xB8, 0x41, 0x85, 0x9F, 0x48, 0xC2, 0x24, 0x17, 0xD8,
        0xC1, 0xB5, 0x84, 0xB8, 0x41, 0x8D, 0x9F, 0x48, 0xC2, 0x20, 0x17, 0xD8, 0xC1, 0xC5, 0x84, 0xB8,
        0x41, 0x7B, 0x3E, 0x90, 0xC2, 0x28, 0x17, 0xD8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xC1, 0xBD, 0x84, 0xB8, 0xC2, 0x24, 0x17, 0xD8,
    ];

    private static byte[] ShapeData(byte kind, params float[] union) =>
    [
        0x12, 0x34, 0x56, 0x78, 0x1D, 0x00, 0x00, 0x1D, 0x00, 0x00, 0x00, 0x00, .. new byte[32],
        kind, 0x00, 0x00, 0x00,
        .. union.SelectMany(F32), .. new byte[36 - 4 * union.Length],
        .. new byte[4],                     // mat
        .. F32(0.5f), .. F32(10f), .. F32(-20f), // Rotation, PivotX, PivotZ
    ];

    [Fact]
    public void Read_Volume_ProducesVolumeAsset() =>
        Assert.IsType<VolumeAsset>(Read(ROTUExemplar));

    [Fact]
    public void Read_Volume_PopulatesFields()
    {
        var asset = (VolumeAsset)Read(ROTUExemplar);

        var box = Assert.IsType<Bound.Box>(asset.Shape);
        Assert.Equal(-23.689804f, box.Center.X);
        Assert.Equal(-40.023285f, box.Upper.Z);
        Assert.Equal(-42.023285f, box.Lower.Z);
        Assert.Equal(0f, asset.Rotation);
        Assert.Equal(-23.689804f, asset.PivotX);
        Assert.Equal(-41.023285f, asset.PivotZ);
    }

    [Fact]
    public void Read_ThenWrite_RealROTUExemplar_ReproducesInputBytes() =>
        Assert.Equal(ROTUExemplar, Write(Read(ROTUExemplar)));

    [Fact]
    public void Read_ThenWrite_RealN100FExemplar_ReproducesInputBytes()
    {
        // n100f/prototype_2003-07-08/XBOX/NTSC-U/US/c0/c001.HIP, AHDR id=0xDD42A1D1
        byte[] data =
        [
            0xD1, 0xA1, 0x42, 0xDD, 0x1D, 0x00, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
            0x30, 0x7B, 0x6B, 0xC1, 0x84, 0x16, 0xAC, 0x40, 0x68, 0x42, 0xAE, 0x41, 0x68, 0x42, 0xD2, 0x41,
            0xA1, 0x05, 0xF7, 0x41, 0x34, 0x21, 0x59, 0x42, 0xCC, 0xDE, 0x5E, 0xC2, 0x5F, 0xFA, 0xA0, 0xC1,
            0x30, 0x7B, 0x2B, 0xC1, .. new byte[16], 0x00, 0x00, 0x00, 0x00, 0x9A, 0x90, 0x80, 0x42,
            0x9A, 0x90, 0x80, 0x42,
        ];
        var profile = N100FSerializer.DefaultProfile with { Platform = Platform.Xbox };

        var asset = (VolumeAsset)Read(data, profile);

        Assert.IsType<Bound.Box>(asset.Shape);
        Assert.Equal(64.282425f, asset.PivotX);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Read_ThenWrite_SphereVolume_ReproducesInputBytes()
    {
        byte[] data = ShapeData(0x01, 1f, 2f, 3f, 4.5f);

        var asset = (VolumeAsset)Read(data);

        var sphere = Assert.IsType<Bound.Sphere>(asset.Shape);
        Assert.Equal(new Vector3(1f, 2f, 3f), sphere.Center);
        Assert.Equal(4.5f, sphere.Radius);
        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_ThenWrite_CylinderVolume_ReproducesInputBytes()
    {
        byte[] data = ShapeData(0x03, 1f, 2f, 3f, 4.5f, 6f);

        var asset = (VolumeAsset)Read(data);

        var cylinder = Assert.IsType<Bound.Cylinder>(asset.Shape);
        Assert.Equal(4.5f, cylinder.Radius);
        Assert.Equal(6f, cylinder.Height);
        Assert.Equal(data, Write(asset));
    }

    [Fact]
    public void Read_ThenWrite_VolumeWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. ROTUExemplar, 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_Volume_UnknownShapeKind_ThrowsInvalidDataException() =>
        Assert.Throws<InvalidDataException>(() => Read(ShapeData(0x07, 1f, 2f, 3f)));

    [Fact]
    public void Read_Volume_UnderBFBB_DegradesToGenericAsset() =>
        Assert.IsNotType<VolumeAsset>(Read(ROTUExemplar, BFBBSerializer.DefaultProfile), exactMatch: false);
}
