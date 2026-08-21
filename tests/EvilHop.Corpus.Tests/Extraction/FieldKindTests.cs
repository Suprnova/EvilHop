using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Corpus.Extraction;

namespace EvilHop.Corpus.Tests.Extraction;

public class FieldKindTests
{
    [Fact]
    public void FormatKey_PrintableFourCcEnum_RendersAsAsciiString()
    {
        // Animation = 0x414E494D, which reads as the ASCII FourCC "ANIM".
        string key = FieldKind.Numeric.FormatKey(AssetType.Animation);

        Assert.Equal("ANIM", key);
    }

    [Fact]
    public void FormatKey_AllZeroEnum_FallsBackToHex()
    {
        string key = FieldKind.Numeric.FormatKey(AssetType.Unknown);

        Assert.Equal("0x00000000", key);
    }

    [Fact]
    public void FormatKey_NonPrintableFlagsCombination_FallsBackToHex()
    {
        string key = FieldKind.Numeric.FormatKey(PackFlags.Default);

        Assert.Equal("0x0000002E", key);
    }

    [Fact]
    public void ToJsonNode_PrintableFourCcEnum_RendersAsAsciiJsonString()
    {
        var node = FieldKind.Numeric.ToJsonNode(AssetType.Animation);

        Assert.Equal("ANIM", node!.GetValue<string>());
    }

    [Fact]
    public void Classify_NullableNumericType_ClassifiesAsUnderlyingKind()
    {
        var kind = FieldKindClassifier.Classify(typeof(uint?));

        Assert.Same(FieldKind.Numeric, kind);
    }

    [Fact]
    public void Classify_ByteArray_ClassifiesAsBytes()
    {
        var kind = FieldKindClassifier.Classify(typeof(byte[]));

        Assert.Same(FieldKind.Bytes, kind);
    }

    [Fact]
    public void Classify_NonBlockCollection_ClassifiesAsCollection()
    {
        var kind = FieldKindClassifier.Classify(typeof(List<uint>));

        Assert.Same(FieldKind.Collection, kind);
    }
}
