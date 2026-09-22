using EvilHop.Assets;
using EvilHop.Assets.Serialization;
using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Primitives;
using EvilHop.Serialization;
using System.Numerics;

namespace EvilHop.Tests.Serialization;

public class ModelInfoAssetTests
{
    private static (AssetHeader Header, AssetDebug Debug) HeaderFor()
    {
        var serializer = new N100FSerializer();
        var header = serializer.CreateBlock<AssetHeader>();
        var debug = serializer.CreateBlock<AssetDebug>();

        header.Type = AssetType.ModelInfo;
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

    private static byte[] UInt32(uint value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Int16(short value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Single(float value) => [.. BitConverter.GetBytes(value).Reverse()];
    private static byte[] Vector(float x, float y, float z) => [.. Single(x), .. Single(y), .. Single(z)];

    private static byte[] Instance(uint modelId, ushort flags, byte parent, byte bone, Vector3 right, Vector3 up, Vector3 at, Vector3 pos) =>
    [
        .. UInt32(modelId),
        .. Int16((short)flags),
        parent, bone,
        .. Vector(right.X, right.Y, right.Z),
        .. Vector(up.X, up.Y, up.Z),
        .. Vector(at.X, at.Y, at.Z),
        .. Vector(pos.X, pos.Y, pos.Z),
    ];

    private static readonly Vector3 IdentityRight = new(1, 0, 0);
    private static readonly Vector3 IdentityUp = new(0, 1, 0);
    private static readonly Vector3 IdentityAt = new(0, 0, 1);

    private static byte[] RootInstance(uint modelId) =>
        Instance(modelId, 0xCDCD, 0, 0, IdentityRight, IdentityUp, IdentityAt, Vector3.Zero);

    private static byte[] Param(uint hashId, byte wordLength, byte[] stringArea) => [.. UInt32(hashId), wordLength, .. stringArea];

    private static byte[] Param(uint hashId, string value)
    {
        byte[] valueBytes = System.Text.Encoding.ASCII.GetBytes(value);
        int stringBytesWithNull = valueBytes.Length + 1;
        int totalRegion = ((1 + stringBytesWithNull + 3) / 4) * 4;
        byte wordLength = (byte)(totalRegion / 4 - 1);
        byte[] stringArea = new byte[totalRegion - 1];
        valueBytes.CopyTo(stringArea, 0);
        return Param(hashId, wordLength, stringArea);
    }

    private static byte[] N100FHeader(uint numModelInst, uint animTableId) =>
    [
        0x46, 0x4E, 0x49, 0x4D, // Magic ("FNIM" bytes), matching 0x464E494D read under a big-endian profile
        .. UInt32(numModelInst),
        .. UInt32(animTableId),
    ];

    private static byte[] FullHeader(uint numModelInst, uint animTableId, uint combatId, uint brainId) =>
    [
        0x46, 0x4E, 0x49, 0x4D, // Magic ("FNIM" bytes), matching 0x464E494D read under a big-endian profile
        .. UInt32(numModelInst),
        .. UInt32(animTableId),
        .. UInt32(combatId),
        .. UInt32(brainId),
    ];

    private static byte[] N100FSampleData() =>
    [
        .. N100FHeader(1, 0x8E62E340),
        .. RootInstance(0x03605F78),
    ];

    private static byte[] BfbbSampleData() =>
    [
        .. FullHeader(1, 0xFF3D5858, 0, 0),
        .. RootInstance(0xBF8233F9),
        .. Param(0x760DAEFD, "1.0"),
        .. Param(0x1BE96B2B, "50.0"),
        .. Param(0xACBAF0C6, "0.5"),
        .. Param(0x50E7A4A3, "{ 3.0, 3.0, 3.0 }"),
    ];

    [Fact]
    public void Read_ModelInfo_UnderN100F_ProducesModelInfoAsset() =>
        Assert.IsType<ModelInfoAsset>(Read(N100FSampleData(), N100FSerializer.DefaultProfile));

    [Fact]
    public void Read_ModelInfo_UnderN100F_HasNoCombatOrBrainId()
    {
        var asset = (ModelInfoAsset)Read(N100FSampleData(), N100FSerializer.DefaultProfile);

        Assert.Equal(new AssetId(0x8E62E340), asset.AnimTableId);
        Assert.Equal(AssetId.None, asset.CombatId);
        Assert.Equal(AssetId.None, asset.BrainId);
    }

    [Fact]
    public void Read_ModelInfo_UnderBFBB_PopulatesEveryField()
    {
        var asset = (ModelInfoAsset)Read(BfbbSampleData(), BFBBSerializer.DefaultProfile);

        Assert.Equal(0x464E494Du, asset.Physical.Magic);
        Assert.Equal(new AssetId(0xFF3D5858), asset.AnimTableId);
        Assert.Equal(AssetId.None, asset.CombatId);
        Assert.Equal(AssetId.None, asset.BrainId);

        Assert.Single(asset.ModelInstances);
        var instance = asset.ModelInstances[0];
        Assert.Equal(new AssetId(0xBF8233F9), instance.ModelId);
        Assert.Equal(0xCDCD, instance.Flags);
        Assert.Equal(IdentityRight, instance.Right);
        Assert.Equal(IdentityUp, instance.Up);
        Assert.Equal(IdentityAt, instance.At);
        Assert.Equal(Vector3.Zero, instance.Position);

        Assert.Equal(4, asset.Parameters.Count);
        Assert.Equal(0x760DAEFDu, asset.Parameters[0].HashId);
        Assert.Equal("1.0", asset.Parameters[0].Value);
        Assert.Equal("50.0", asset.Parameters[1].Value);
        Assert.Equal("0.5", asset.Parameters[2].Value);
        Assert.Equal("{ 3.0, 3.0, 3.0 }", asset.Parameters[3].Value);
    }

    [Fact]
    public void Read_ModelInfo_ModelInstanceCountKeepsDerivingAfterMutation()
    {
        var asset = (ModelInfoAsset)Read(BfbbSampleData(), BFBBSerializer.DefaultProfile);
        Assert.Equal(1u, asset.Physical.ModelInstanceCount);

        asset.ModelInstances.Add(new ModelInfoAsset.Instance());

        Assert.Equal(2u, asset.Physical.ModelInstanceCount);
    }

    [Fact]
    public void Read_ThenWrite_ModelInfoUnderN100F_ReproducesInputBytes()
    {
        byte[] data = N100FSampleData();
        var profile = N100FSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_ModelInfoUnderBFBB_ReproducesInputBytes()
    {
        byte[] data = BfbbSampleData();
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("abc")]
    [InlineData("abcd")]
    [InlineData("robot_drill_shrapnel_up")]
    public void Read_ThenWrite_ModelInfoParamWithVaryingStringLength_ReproducesInputBytes(string value)
    {
        byte[] data = [.. FullHeader(0, 0, 0, 0), .. Param(0x12345678, value)];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_ModelInfoWithNoModelInstancesOrParams_ReproducesInputBytes()
    {
        byte[] data = FullHeader(0, 0, 0, 0);
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }

    [Fact]
    public void Read_ThenWrite_ModelInfoWithUnparsedTail_ReproducesInputBytes()
    {
        byte[] data = [.. BfbbSampleData(), 0xDE, 0xAD, 0xBE];
        var profile = BFBBSerializer.DefaultProfile;

        Assert.Equal(data, Write(Read(data, profile), profile));
    }
}
