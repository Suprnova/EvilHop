using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Corpus.Extraction;

namespace EvilHop.Corpus.Tests.Extraction;

public class ValueFormatterTests
{
    [Fact]
    public void FormatKey_PrintableFourCcEnum_RendersAsAsciiString()
    {
        // Animation = 0x414E494D, which reads as the ASCII FourCC "ANIM".
        string key = ValueFormatter.FormatKey(AssetType.Animation, ValueKind.Numeric);

        Assert.Equal("ANIM", key);
    }

    [Fact]
    public void FormatKey_AllZeroEnum_FallsBackToHex()
    {
        string key = ValueFormatter.FormatKey(AssetType.Unknown, ValueKind.Numeric);

        Assert.Equal("0x00000000", key);
    }

    [Fact]
    public void FormatKey_NonPrintableFlagsCombination_FallsBackToHex()
    {
        string key = ValueFormatter.FormatKey(PackFlags.Default, ValueKind.Numeric);

        Assert.Equal("0x0000002E", key);
    }

    [Fact]
    public void ToJsonNode_PrintableFourCcEnum_RendersAsAsciiJsonString()
    {
        var node = ValueFormatter.ToJsonNode(AssetType.Animation, ValueKind.Numeric);

        Assert.Equal("ANIM", node!.GetValue<string>());
    }
}
