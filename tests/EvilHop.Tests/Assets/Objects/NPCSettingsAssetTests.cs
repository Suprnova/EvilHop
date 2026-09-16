using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;

namespace EvilHop.Tests.Serialization;

public class NPCSettingsAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new IncrediblesSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.NPCSettings;
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

    private static byte[] UInt32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];

    private static byte[] Param(uint hashId, string value)
    {
        byte[] valueBytes = System.Text.Encoding.ASCII.GetBytes(value);
        int stringBytesWithNull = valueBytes.Length + 1;
        int totalRegion = ((1 + stringBytesWithNull + 3) / 4) * 4;
        byte wordLength = (byte)(totalRegion / 4 - 1);
        byte[] stringArea = new byte[totalRegion - 1];
        valueBytes.CopyTo(stringArea, 0);
        return [.. UInt32(hashId), wordLength, .. stringArea];
    }

    private static byte[] Data(uint parameterCount, params byte[][] parameters) =>
    [
        .. UInt32(parameterCount),
        .. parameters.SelectMany(p => p),
    ];

    [Fact]
    public void Read_NPCSettings_ProducesNPCSettingsAsset() =>
        Assert.IsType<NPCSettingsAsset>(Read(Data(0)));

    [Fact]
    public void Read_NPCSettings_PopulatesParameters()
    {
        byte[] data = Data(3,
            Param(0x9C15CF3D, "1000"),
            Param(0x40B93FF7, "180"),
            Param(0x7274763D, "1"));

        var asset = (NPCSettingsAsset)Read(data);

        Assert.Equal(3, asset.Parameters.Count);
        Assert.Equal(0x9C15CF3Du, asset.Parameters[0].HashId);
        Assert.Equal("1000", asset.Parameters[0].Value);
        Assert.Equal("180", asset.Parameters[1].Value);
        Assert.Equal("1", asset.Parameters[2].Value);
    }

    [Fact]
    public void Read_NPCSettings_ParameterCountKeepsDerivingAfterParametersAreMutated()
    {
        byte[] data = Data(1, Param(0x9C15CF3D, "1000"));

        var asset = (NPCSettingsAsset)Read(data);
        Assert.Equal(1u, asset.Physical.ParameterCount);

        asset.Parameters.Add(new ModelInfoParameter { HashId = 0x1, Value = "0" });

        Assert.Equal(2u, asset.Physical.ParameterCount);
    }

    [Fact]
    public void Read_ThenWrite_NPCSettingsWithNoParameters_ReproducesInputBytes()
    {
        byte[] data = Data(0);

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_NPCSettingsWithParameters_ReproducesInputBytes()
    {
        byte[] data = Data(2, Param(0x0BCA9F39, "50"), Param(0x12345678, "{ 3.0, 3.0, 3.0 }"));

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_ThenWrite_NPCSettingsUnderROTU_ReproducesInputBytes()
    {
        byte[] data = Data(1, Param(0x0BCA9F39, "50"));
        var profile = ROTUSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_NPCSettingsWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. Data(0), 0xDE, 0xAD, 0xBE, 0xEF];

        Assert.Equal(data, Write(Read(data)));
    }

    [Fact]
    public void Read_NPCSettings_UnderBFBB_DegradesToGenericAsset()
    {
        byte[] data = Data(0);

        var asset = Read(data, BFBBSerializer.DefaultProfile);

        Assert.IsNotType<NPCSettingsAsset>(asset);
    }
}
