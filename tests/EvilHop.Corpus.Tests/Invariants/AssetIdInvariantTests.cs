using EvilHop.Blocks;
using EvilHop.Common;
using EvilHop.Corpus.Invariants;

namespace EvilHop.Corpus.Tests.Invariants;

public class AssetIdMatchesNameHashInvariantTests
{
    private static ArchiveContext ArchiveOf(params Block[] roots) => new()
    {
        BuildKey = "n100f/release",
        RelativePath = "n100f/release/boot.HIP",
        Roots = roots,
        ArchiveLength = 0
    };

    private static AssetHeader Header(string name, AssetType type, uint id)
    {
        var header = BlockFactory.CreateAssetHeader(id, name);
        header.Type = type;
        return header;
    }

    [Fact]
    public void Check_NameHashesDirectly_ClassifiesAsDirect()
    {
        var header = Header("ice", AssetType.Texture, BKDRHash.Calculate("ice"));
        var invariant = new AssetIdMatchesNameHashInvariant();

        invariant.Check(ArchiveOf(header));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["direct"]!.GetValue<long>());
    }

    [Fact]
    public void Check_AnimationNameWithExtension_ClassifiesAsAnimSuffix()
    {
        uint id = BKDRHash.Calculate("run.anm");
        var header = Header("run.mp", AssetType.Animation, id);
        var invariant = new AssetIdMatchesNameHashInvariant();

        invariant.Check(ArchiveOf(header));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["anim-suffix"]!.GetValue<long>());
    }

    [Fact]
    public void Check_AnimationNameWithNoExtension_ClassifiesAsAnimSuffix()
    {
        // The .anm candidate applies unconditionally to every Animation-typed name, not just ones
        // ending in a specific source extension - real archives hash plenty with none at all.
        uint id = BKDRHash.Calculate("destruct_crate_idle.anm");
        var header = Header("destruct_crate_idle", AssetType.Animation, id);
        var invariant = new AssetIdMatchesNameHashInvariant();

        invariant.Check(ArchiveOf(header));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["anim-suffix"]!.GetValue<long>());
    }

    [Fact]
    public void Check_MorphTargetNameWithExtensionReplaced_ClassifiesAsMphtReplace()
    {
        uint id = BKDRHash.Calculate("face.mph");
        var header = Header("face.mdl", AssetType.MorphTarget, id);
        var invariant = new AssetIdMatchesNameHashInvariant();

        invariant.Check(ArchiveOf(header));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["mpht-replace"]!.GetValue<long>());
    }

    [Fact]
    public void Check_MorphTargetNameWithExtensionAppended_ClassifiesAsMphtAppend()
    {
        uint id = BKDRHash.Calculate("face.mdl.mph");
        var header = Header("face.mdl", AssetType.MorphTarget, id);
        var invariant = new AssetIdMatchesNameHashInvariant();

        invariant.Check(ArchiveOf(header));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["mpht-append"]!.GetValue<long>());
    }

    [Fact]
    public void ToJson_AlwaysOmitsMatchedSamples()
    {
        var header = Header("ice", AssetType.Texture, BKDRHash.Calculate("ice"));
        var invariant = new AssetIdMatchesNameHashInvariant();

        invariant.Check(ArchiveOf(header));

        Assert.Null(invariant.ToJson()["matched"]);
    }

    [Fact]
    public void Check_ThirtyOneCharacterNameWithNoMatch_ClassifiesAsTruncated()
    {
        string name = new('a', 31);
        var header = Header(name, AssetType.Texture, 0xDEADBEEF);
        var invariant = new AssetIdMatchesNameHashInvariant();

        invariant.Check(ArchiveOf(header));

        Assert.Equal(1, invariant.ToJson()["outcomes"]!["truncated"]!.GetValue<long>());
    }

    [Fact]
    public void Check_ShortNameWithNoMatch_ClassifiesAsUnexplained()
    {
        var header = Header("mystery", AssetType.Texture, 0xDEADBEEF);
        var invariant = new AssetIdMatchesNameHashInvariant();

        invariant.Check(ArchiveOf(header));

        var json = invariant.ToJson();
        Assert.Equal(1, json["outcomes"]!["unexplained"]!.GetValue<long>());
        var sample = json["unexplained"]![0]!;
        Assert.Equal("mystery", (string)sample["name"]!);
        Assert.Equal("0xDEADBEEF", (string)sample["expected"]!);
        Assert.Equal($"0x{BKDRHash.Calculate("mystery"):X8}", (string)sample["actual"]!);
    }
}

public class AssetIdsUniqueInvariantTests
{
    private static ArchiveContext ArchiveOf(params Block[] roots) => new()
    {
        BuildKey = "n100f/release",
        RelativePath = "n100f/release/boot.HIP",
        Roots = roots,
        ArchiveLength = 0
    };

    [Fact]
    public void Check_DistinctIds_AllPass()
    {
        var invariant = new AssetIdsUniqueInvariant();

        invariant.Check(ArchiveOf(BlockFactory.CreateAssetHeader(1, "a"), BlockFactory.CreateAssetHeader(2, "b")));

        var json = invariant.ToJson();
        Assert.Equal(0, json["outcomes"]!["violated"]!.GetValue<long>());
        Assert.Equal(2, json["outcomes"]!["passing"]!.GetValue<long>());
        Assert.Null(json["passing"]);
    }

    [Fact]
    public void Check_DuplicateId_RecordsOneViolation()
    {
        var invariant = new AssetIdsUniqueInvariant();

        invariant.Check(ArchiveOf(BlockFactory.CreateAssetHeader(1, "a"), BlockFactory.CreateAssetHeader(1, "b")));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(1, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(1, outcomes["passing"]!.GetValue<long>());
    }
}
