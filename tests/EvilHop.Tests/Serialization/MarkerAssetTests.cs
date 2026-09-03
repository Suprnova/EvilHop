using EvilHop.Assets;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class MarkerAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.Marker;
        header.Debug = debug;

        return (header, debug);
    }

    private static Asset Read(byte[] data, FormatProfile? profile = null)
    {
        profile ??= N100FSerializer.DefaultProfile;
        var (header, debug) = HeaderFor();
        using var reader = new EndianReader(new MemoryStream(data), profile.Endianness);
        return AssetCodecs.Read(reader, header, debug, profile);
    }

    private static byte[] Write(Asset asset, FormatProfile? profile = null)
    {
        profile ??= N100FSerializer.DefaultProfile;
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, profile.Endianness, leaveOpen: true))
            AssetCodecs.Write(asset, writer, profile);
        return stream.ToArray();
    }

    private static byte[] PositionBytes(float x, float y, float z) =>
    [
        .. BitConverter.GetBytes(x).Reverse(),
        .. BitConverter.GetBytes(y).Reverse(),
        .. BitConverter.GetBytes(z).Reverse(),
    ];

    [Fact]
    public void Read_Marker_ProducesMarkerAsset() =>
        Assert.IsType<MarkerAsset>(Read(PositionBytes(1, 2, 3)));

    [Fact]
    public void Read_Marker_PopulatesPosition()
    {
        var asset = (MarkerAsset)Read(PositionBytes(1.5f, -2.5f, 3.5f));

        Assert.Equal(new Vector3(1.5f, -2.5f, 3.5f), asset.Position);
    }

    [Fact]
    public void Read_ThenWrite_Marker_ReproducesInputBytes()
    {
        byte[] data = PositionBytes(1, 2, 3);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_MarkerWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. PositionBytes(1, 2, 3), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_Marker_UnderRatatouille_DegradesToGenericAsset()
    {
        byte[] data = PositionBytes(1, 2, 3);

        var asset = Read(data, RatatouilleSerializer.DefaultProfile);

        Assert.IsNotType<MarkerAsset>(asset);
        Assert.Equal(data, asset.GetUnparsedTail().ToArray());
    }

    [Fact]
    public void Write_MarkerAsset_UnderRatatouille_StillWritesItsOwnFields()
    {
        var asset = new MarkerAsset { Type = AssetType.Marker, Position = new Vector3(1, 2, 3) };

        Assert.Equal(PositionBytes(1, 2, 3), Write(asset, RatatouilleSerializer.DefaultProfile));
    }
}
