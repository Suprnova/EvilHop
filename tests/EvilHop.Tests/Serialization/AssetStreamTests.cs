using EvilHop.Blocks;
using EvilHop.Primitives;

namespace EvilHop.Tests.Serialization;

public class AssetStreamTests
{
    [Fact]
    public void ReadBlock_Dhdr_ReadsExpectedFields()
    {
        var content = BlockBytes.Content(w => w.WriteEvilInt(0xFFFFFFFF));
        var reader = BlockBytes.Reader("DHDR", content);

        var block = (StreamHeader)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(0xFFFFFFFFu, block.Value);
    }

    [Fact]
    public void ReadBlock_Dpak_ReadsPaddingAndData()
    {
        var content = BlockBytes.Content(w =>
        {
            w.WriteEvilInt(2);
            w.Write([0x33, 0x33]);
            w.Write([0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04]);
        });
        var reader = BlockBytes.Reader("DPAK", content);

        var block = (StreamData)new TestSerializer().ReadBlockPublic(reader);

        Assert.Equal(2u, block.PaddingAmount);
        Assert.Equal([0x33, 0x33], block.Padding);
        Assert.Equal([0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04], block.Data);
    }

    [Fact]
    public void ReadBlock_Dpak_NoAssets_PaddingAmountStaysNull()
    {
        var reader = BlockBytes.Reader("DPAK", []);

        var block = (StreamData)new TestSerializer().ReadBlockPublic(reader);

        Assert.Null(block.PaddingAmount);
        Assert.Empty(block.Padding);
        Assert.Empty(block.Data);
    }
}
