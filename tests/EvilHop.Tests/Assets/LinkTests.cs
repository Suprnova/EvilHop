using EvilHop.Assets;
using EvilHop.Primitives;
using System.Collections.Immutable;

namespace EvilHop.Tests.Assets;

public class LinkTests
{
    [Fact]
    public void DefaultConstructor_ParamsHasFourZeroedSlots()
    {
        var link = new Link();

        Assert.Equal(4, link.Params.Length);
        Assert.All(link.Params, p => Assert.IsType<RawParameter>(p));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void Params_SetToWrongLength_ThrowsArgumentException(int length)
    {
        var link = new Link();
        var wrongLength = new Parameter[length].ToImmutableArray();

        Assert.Throws<ArgumentException>(() => link.Params = wrongLength);
    }

    [Fact]
    public void Params_AllFourPopulated_WriteToSixteenBytesInOrder()
    {
        var link = new Link
        {
            Params =
            [
                new RawParameter([0x00, 0x01, 0x02, 0x03]),
                new IntParameter(4),
                new FloatParameter(1.0f),
                new AssetIdParameter(new AssetId(0xDEADBEEF)),
            ]
        };

        using var stream = new MemoryStream();
        using (var writer = new EndianWriter(stream, Endianness.Big, leaveOpen: true))
            foreach (var parameter in link.Params)
                parameter.WriteTo(writer);

        Assert.Equal<byte>(
        [
            0x00, 0x01, 0x02, 0x03,
            0x00, 0x00, 0x00, 0x04,
            0x3F, 0x80, 0x00, 0x00,
            0xDE, 0xAD, 0xBE, 0xEF,
        ], stream.ToArray());
    }
}
