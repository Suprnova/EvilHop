using EvilHop.Assets;
using EvilHop.Primitives;

namespace EvilHop.Tests.Assets;

public class ParameterTests
{
    private static byte[] WrittenBytes(Parameter parameter)
    {
        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true))
            parameter.WriteTo(writer);
        return stream.ToArray();
    }

    [Fact]
    public void RawParameter_WriteTo_RoundTripsBytesUnchanged()
    {
        byte[] bytes = [0xDE, 0xAD, 0xBE, 0xEF];
        var parameter = new RawParameter(bytes);

        Assert.Equal(bytes, WrittenBytes(parameter));
    }

    [Fact]
    public void FloatParameter_WriteTo_WritesBigEndianBytes()
    {
        var parameter = new FloatParameter(1.5f);

        Assert.Equal<byte>([0x3F, 0xC0, 0x00, 0x00], WrittenBytes(parameter));
    }

    [Fact]
    public void IntParameter_WriteTo_WritesBigEndianBytes()
    {
        var parameter = new IntParameter(0x12345678);

        Assert.Equal<byte>([0x12, 0x34, 0x56, 0x78], WrittenBytes(parameter));
    }

    [Fact]
    public void AssetIdParameter_WriteTo_WritesBigEndianBytes()
    {
        var parameter = new AssetIdParameter(new AssetId(0x12345678));

        Assert.Equal<byte>([0x12, 0x34, 0x56, 0x78], WrittenBytes(parameter));
    }
}
