using EvilHop.Blocks;
using EvilHop.Serialization;
using EvilHop.Validation;

namespace EvilHop.Tests;

public class ArchiveTests
{
    private static FileStream OpenMinimalFixture() =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "n100f", "minimal.hip"));

    private sealed class RecordingBlock : Block
    {
        protected internal override string Tag => "RECD";

        public List<ValidationIssue> OwnIssues { get; } = [];
        public ValidationContext? ReceivedContext { get; private set; }

        public override IEnumerable<ValidationIssue> Validate(ValidationContext context)
        {
            ReceivedContext = context;
            return OwnIssues.Concat(base.Validate(context));
        }
    }

    private static ValidationIssue MakeIssue(string ruleId) =>
        new(ruleId, Severity.Warning, new ArchiveSite(), "test issue");

    [Fact]
    public void Load_ThenSave_MinimalFixture_ProducesIdenticalBytes()
    {
        using var fixture = OpenMinimalFixture();
        using var fixtureCopy = new MemoryStream();
        fixture.CopyTo(fixtureCopy);
        byte[] originalBytes = fixtureCopy.ToArray();

        var archive = Archive.Load(new MemoryStream(originalBytes), new N100FSerializer());

        using var rewritten = new MemoryStream();
        archive.Save(rewritten);

        Assert.Equal(originalBytes, rewritten.ToArray());
    }

    [Fact]
    public void Validate_NoRoots_ReturnsEmpty()
    {
        var archive = new Archive(new N100FSerializer(), []);

        var issues = archive.Validate(new ValidationContext(N100FSerializer.DefaultProfile));

        Assert.Empty(issues);
    }

    [Fact]
    public void Validate_RootsWithIssues_ReturnsEveryRootsIssue()
    {
        var firstIssue = MakeIssue("first-rule");
        var secondIssue = MakeIssue("second-rule");
        var first = new RecordingBlock();
        first.OwnIssues.Add(firstIssue);
        var second = new RecordingBlock();
        second.OwnIssues.Add(secondIssue);
        var archive = new Archive(new N100FSerializer(), [first, second]);

        var issues = archive.Validate(new ValidationContext(N100FSerializer.DefaultProfile));

        Assert.Equal([firstIssue, secondIssue], issues);
    }

    [Fact]
    public void Validate_Parameterless_BuildsContextFromSerializerProfile()
    {
        var root = new RecordingBlock();
        var archive = new Archive(new N100FSerializer(), [root]);

        archive.Validate().ToList();

        Assert.Equal(N100FSerializer.DefaultProfile, root.ReceivedContext?.Profile);
    }

    [Fact]
    public void Validate_Parameterless_LeavesOriginAndRoleUnknown()
    {
        var root = new RecordingBlock();
        var archive = new Archive(new N100FSerializer(), [root]);

        archive.Validate().ToList();

        Assert.Equal(ArchiveOrigin.Unknown, root.ReceivedContext?.Origin);
        Assert.Equal(ArchiveRole.Unknown, root.ReceivedContext?.Role);
    }
}
