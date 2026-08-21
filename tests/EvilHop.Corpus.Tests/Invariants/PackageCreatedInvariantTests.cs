using EvilHop.Blocks;
using EvilHop.Corpus.Invariants;

namespace EvilHop.Corpus.Tests.Invariants;

public class CreatedDateStringMatchesTimestampInvariantTests
{
    private static ArchiveContext ArchiveOf(params Block[] roots) => new()
    {
        BuildKey = "n100f/release",
        RelativePath = "n100f/release/boot.HIP",
        Roots = roots,
        ArchiveLength = 0
    };

    private static PackageCreated Created(long rawUnixSeconds, string dateString)
    {
        var created = BlockFactory.Create<PackageCreated>();
        created.CreatedDate = DateTimeOffset.FromUnixTimeSeconds(rawUnixSeconds);
        created.CreatedDateString = dateString;
        return created;
    }

    [Fact]
    public void Check_StringMatchesUtcConvertedToPacificDaylightTime_Passes()
    {
        var invariant = new CreatedDateStringMatchesTimestampInvariant();

        invariant.Check(ArchiveOf(Created(1028661674, "Tue Aug 06 12:21:14 2002\n")));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(0, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(1, outcomes["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_StringMatchesUtcConvertedToPacificStandardTime_Passes()
    {
        // 1011124800 is 2002-01-15, before that year's DST cutover - the Pacific offset is -8:00,
        // not the -7:00 that applies during the rest of the corpus. Regression coverage for the
        // fixed-offset bug this invariant exists to catch.
        var invariant = new CreatedDateStringMatchesTimestampInvariant();

        invariant.Check(ArchiveOf(Created(1011124800, "Tue Jan 15 12:00:00 2002\n")));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(0, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(1, outcomes["passing"]!.GetValue<long>());
    }

    [Fact]
    public void Check_StringDoesNotMatchTimestamp_RecordsViolation()
    {
        var invariant = new CreatedDateStringMatchesTimestampInvariant();

        invariant.Check(ArchiveOf(Created(1028661674, "Wed Aug 07 12:21:14 2002\n")));

        var outcomes = invariant.ToJson()["outcomes"]!;
        Assert.Equal(1, outcomes["violated"]!.GetValue<long>());
        Assert.Equal(0, outcomes["passing"]!.GetValue<long>());
    }
}
