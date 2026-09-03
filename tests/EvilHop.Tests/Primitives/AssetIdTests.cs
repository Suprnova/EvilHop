using EvilHop.Common;
using EvilHop.Primitives;

namespace EvilHop.Tests.Primitives;

public class AssetIdTests
{
    [Fact]
    public void Equality_SameValue_AreEqual() =>
        Assert.Equal(new AssetId(1234), new AssetId(1234));

    [Fact]
    public void Equality_DifferentValue_AreNotEqual() =>
        Assert.NotEqual(new AssetId(1234), new AssetId(5678));

    [Fact]
    public void ToString_FormatsAsUppercaseHex() =>
        Assert.Equal("0x000004D2", new AssetId(1234).ToString());

    [Fact]
    public void FromName_HashesTheNameDirectly() =>
        Assert.Equal(BKDRHash.Calculate("foo"), AssetId.FromName("foo").Value);

    [Fact]
    public void FromName_Animation_ChangesExtensionToAnm() =>
        Assert.Equal(BKDRHash.Calculate("foo.anm"), AssetId.FromName("foo.dff", AssetType.Animation).Value);

    [Fact]
    public void FromName_DestructibleAsset_AppendsDffDestructExtension() =>
        Assert.Equal(BKDRHash.Calculate("foo.dff.dff_destruct"), AssetId.FromName("foo.dff", AssetType.DestructibleAsset).Value);

    [Fact]
    public void FromName_MorphTarget_ChangesExtensionToMph() =>
        Assert.Equal(BKDRHash.Calculate("foo.mph"), AssetId.FromName("foo.dff", AssetType.MorphTarget).Value);

    [Fact]
    public void FromName_MorphTarget_ExtensionlessName_MatchesNaiveAppend()
    {
        // ChangeExtension appends when the name has no existing extension, so replace and append
        // agree here - the case every real MorphTarget name in the corpus falls into.
        Assert.Equal(AssetId.FromName("foo.mph"), AssetId.FromName("foo", AssetType.MorphTarget));
    }

    [Fact]
    public void FromName_MorphTarget_DottedName_DiffersFromNaiveAppend()
    {
        // ChangeExtension replaces an existing extension, where a naive append would keep it - the
        // two diverge here, a case nothing in the corpus exercises.
        Assert.NotEqual(AssetId.FromName("foo.bar.mph"), AssetId.FromName("foo.bar", AssetType.MorphTarget));
    }

    [Fact]
    public void FromName_UnrecognizedType_PassesNameThroughUnchanged() =>
        Assert.Equal(AssetId.FromName("foo"), AssetId.FromName("foo", AssetType.Trigger));

    [Fact]
    public void None_HasZeroValue() =>
        Assert.Equal(0u, AssetId.None.Value);
}
