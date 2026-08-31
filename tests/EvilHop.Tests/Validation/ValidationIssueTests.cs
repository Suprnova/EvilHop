using EvilHop.Validation;

namespace EvilHop.Tests.Validation;

public class ValidationIssueTests
{
    [Fact]
    public void Related_NotProvided_DefaultsToEmpty()
    {
        var issue = new ValidationIssue("rule-id", Severity.Warning, new ArchiveSite(), "message");

        Assert.Empty(issue.Related);
    }

    [Fact]
    public void Related_ProvidedExplicitNull_DefaultsToEmpty()
    {
        var issue = new ValidationIssue("rule-id", Severity.Warning, new ArchiveSite(), "message", Related: null);

        Assert.Empty(issue.Related);
    }

    [Fact]
    public void Related_Provided_ReturnsProvidedSites()
    {
        var related = new IssueSite[] { new ArchiveSite() };

        var issue = new ValidationIssue("rule-id", Severity.Warning, new ArchiveSite(), "message", Related: related);

        Assert.Same(related, issue.Related);
    }

    [Fact]
    public void Classification_NotProvided_DefaultsToNull()
    {
        var issue = new ValidationIssue("rule-id", Severity.Warning, new ArchiveSite(), "message");

        Assert.Null(issue.Classification);
    }
}
