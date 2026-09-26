using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class SplineAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Spline;
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

    // incredibles/prototype_2004-07-19/GC/NTSC-U/US/CI/ci03.HOP, AHDR id=0x14595AD0
    private static readonly byte[] IncrediblesExemplar =
    [
        0xD0, 0x5A, 0x59, 0x14, 0x49, 0x00, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x04,
        0x00, 0x00, 0x00, 0x02, 0x38, 0x2C, 0xA8, 0x23, 0x14, 0x2C, 0xA8, 0x23, 0xC2, 0x83, 0xFC, 0x38,
        0x40, 0xEA, 0x73, 0x75, 0xC0, 0xBB, 0xAC, 0xCF, 0xC2, 0x83, 0xFC, 0x38, 0x40, 0xCF, 0x0B, 0xB5,
        0xC0, 0xBB, 0xAC, 0xCF, 0xC2, 0x83, 0xFA, 0x96, 0x40, 0xC1, 0xBE, 0x32, 0xC0, 0x98, 0x02, 0x2A,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x02, 0xCB, 0x5E, 0x40, 0x44, 0x31, 0x0D,
        0x40, 0x44, 0x31, 0x0D,
    ];

    // rotu/prototype_2005-09-15/GC/NTSC-J/JP/A1/a101.HIP, AHDR id=0x2EBD35CB
    private static byte[] ROTUExemplar(byte linkCount = 0) =>
    [
        0x2E, 0xBD, 0x35, 0xCB, 0x49, linkCount, 0x00, 0x1D, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x07,
        0x00, 0x00, 0x00, 0x03, 0xEC, 0x8A, 0x4B, 0x0E, 0xBC, 0x8A, 0x4B, 0x0E, 0xC2, 0x18, 0xF3, 0xAB,
        0x40, 0xE0, 0x00, 0x40, 0xC2, 0xFF, 0x9D, 0x1C, 0xC2, 0x9F, 0xEC, 0x23, 0x40, 0xDF, 0xFF, 0x7A,
        0xC2, 0xAC, 0x0D, 0x50, 0xC3, 0x02, 0x8F, 0xB5, 0x40, 0xE0, 0x00, 0xA8, 0xC2, 0x4C, 0x22, 0xA8,
        0xC3, 0x0D, 0x05, 0x42, 0x40, 0xE0, 0x00, 0x00, 0x41, 0x70, 0x24, 0x8E, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00,
        0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00, 0x3F, 0x80, 0x00, 0x00,
    ];

    [Fact]
    public void Read_Spline_ProducesSplineAsset() =>
        Assert.IsType<SplineAsset>(Read(ROTUExemplar(), ROTUSerializer.DefaultProfile));

    [Fact]
    public void Read_Spline_PopulatesFields()
    {
        var asset = (SplineAsset)Read(ROTUExemplar(), ROTUSerializer.DefaultProfile);

        Assert.Equal(3, asset.Degree);
        Assert.Equal(4, asset.ControlPoints.Count);
        Assert.Equal(7f, asset.ControlPoints[3].Y);
        Assert.Equal([0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f], asset.Knots);
        Assert.Equal(0xEC8A4B0Eu, asset.Physical.KnotsPointer);
    }

    [Fact]
    public void Read_ThenWrite_RealROTUExemplar_ReproducesInputBytes() =>
        Assert.Equal(ROTUExemplar(), Write(Read(ROTUExemplar(), ROTUSerializer.DefaultProfile), ROTUSerializer.DefaultProfile));

    [Fact]
    public void Read_SplineUnderIncredibles_ReadsLittleEndianHeader()
    {
        var asset = (SplineAsset)Read(IncrediblesExemplar, IncrediblesSerializer.DefaultProfile);

        Assert.Equal(new AssetId(0x14595AD0), asset.Physical.BaseId);
        Assert.Equal(BaseAssetFlags.Valid, asset.Physical.BaseFlags & BaseAssetFlags.Valid);
        Assert.Equal(1, asset.Degree);
        Assert.Equal(3, asset.ControlPoints.Count);
        Assert.Equal(5, asset.Knots.Count);
    }

    [Fact]
    public void Read_ThenWrite_RealIncrediblesExemplar_ReproducesInputBytes() =>
        Assert.Equal(IncrediblesExemplar, Write(Read(IncrediblesExemplar, IncrediblesSerializer.DefaultProfile), IncrediblesSerializer.DefaultProfile));

    [Fact]
    public void Read_ThenWrite_SplineUnderLittleEndianIncredibles_ReproducesInputBytes()
    {
        var profile = IncrediblesSerializer.DefaultProfile with { Platform = Platform.PlayStation2 };
        var gameCube = (SplineAsset)Read(IncrediblesExemplar, IncrediblesSerializer.DefaultProfile);
        byte[] data = Write(gameCube, profile);

        var asset = (SplineAsset)Read(data, profile);

        Assert.Equal(new AssetId(0x14595AD0), asset.Physical.BaseId);
        Assert.Equal([0xD0, 0x5A, 0x59, 0x14], data[..4]);
        Assert.Equal([0x01, 0x00, 0x00, 0x00], data[8..12]);
        Assert.Equal(data, Write(asset, profile));
    }

    [Fact]
    public void Write_SplineUnderTSSM_WritesLittleEndianHeader()
    {
        var asset = (SplineAsset)Read(ROTUExemplar(), ROTUSerializer.DefaultProfile);

        byte[] data = Write(asset, TSSMSerializer.DefaultProfile);

        Assert.Equal([0xCB, 0x35, 0xBD, 0x2E, 0x49, 0x00, 0x1D, 0x00], data[..8]);
        Assert.Equal([0x00, 0x00, 0x00, 0x03], data[8..12]);
    }

    [Fact]
    public void Read_ThenWrite_SplineWithLinkCountButNoLinks_ReproducesInputBytes()
    {
        byte[] data = ROTUExemplar(linkCount: 1);

        var asset = (SplineAsset)Read(data, ROTUSerializer.DefaultProfile);

        Assert.Empty(asset.Links);
        Assert.Equal(1, asset.Physical.LinkCount);
        Assert.Equal(data, Write(asset, ROTUSerializer.DefaultProfile));
    }

    [Fact]
    public void Read_ThenWrite_SplineWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. ROTUExemplar(), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data, ROTUSerializer.DefaultProfile), ROTUSerializer.DefaultProfile));
    }

    [Fact]
    public void KnotMaxIndex_AfterAddingKnot_FollowsKnots()
    {
        var asset = (SplineAsset)Read(ROTUExemplar(), ROTUSerializer.DefaultProfile);

        asset.Knots.Add(1f);

        Assert.Equal(8, asset.Physical.KnotMaxIndex);
    }

    [Fact]
    public void Read_Spline_UnderBFBB_DegradesToGenericAsset() =>
        Assert.IsNotType<SplineAsset>(Read(ROTUExemplar(), BFBBSerializer.DefaultProfile), exactMatch: false);
}
