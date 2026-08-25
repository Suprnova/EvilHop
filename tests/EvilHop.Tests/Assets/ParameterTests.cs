using EvilHop.Assets;
using EvilHop.Primitives;

namespace EvilHop.Tests.Assets;

public class ParameterTests
{
    [Fact]
    public void RawParameter_WriteTo_RoundTripsBytesUnchanged()
    {
        byte[] bytes = [0xDE, 0xAD, 0xBE, 0xEF];
        var parameter = new RawParameter(bytes);
        var destination = new byte[4];

        parameter.WriteTo(destination);

        Assert.Equal(bytes, destination);
    }

    [Fact]
    public void FloatParameter_WriteTo_WritesBigEndianBytes()
    {
        var parameter = new FloatParameter(1.5f);
        var destination = new byte[4];

        parameter.WriteTo(destination);

        Assert.Equal<byte>([0x3F, 0xC0, 0x00, 0x00], destination);
    }

    [Fact]
    public void IntParameter_WriteTo_WritesBigEndianBytes()
    {
        var parameter = new IntParameter(0x12345678);
        var destination = new byte[4];

        parameter.WriteTo(destination);

        Assert.Equal<byte>([0x12, 0x34, 0x56, 0x78], destination);
    }

    [Fact]
    public void AssetIdParameter_WriteTo_WritesBigEndianBytes()
    {
        var parameter = new AssetIdParameter(new AssetId(0x12345678));
        var destination = new byte[4];

        parameter.WriteTo(destination);

        Assert.Equal<byte>([0x12, 0x34, 0x56, 0x78], destination);
    }
}
