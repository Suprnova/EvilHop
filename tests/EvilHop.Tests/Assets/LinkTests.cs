using EvilHop.Assets;
using EvilHop.Primitives;

namespace EvilHop.Tests.Assets;

public class LinkTests
{
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

        var bytes = new byte[16];
        for (int i = 0; i < 4; i++)
            link.Params[i].WriteTo(bytes.AsSpan(i * 4, 4));

        Assert.Equal<byte>(
        [
            0x00, 0x01, 0x02, 0x03,
            0x00, 0x00, 0x00, 0x04,
            0x3F, 0x80, 0x00, 0x00,
            0xDE, 0xAD, 0xBE, 0xEF,
        ], bytes);
    }
}
