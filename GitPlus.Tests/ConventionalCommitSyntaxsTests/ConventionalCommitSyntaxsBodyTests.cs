using GitPlus.Commons.ConventionalCommitSyntaxs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitPlus.Tests.ConventionalCommitSyntaxsTests;

[TestClass]
public sealed class ConventionalCommitSyntaxsBodyTests
{
    [TestMethod]
    public void Parse_SingleLineBody_ParsesCorrectly()
    {
        var commit = """
fix: correct typo in error message

The error message previously said "occured" instead of "occurred".
""";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.IsNotNull(tree.Root.Body);
        Assert.HasCount(1, tree.Root.Body.TextLines);
        Assert.Contains("occured", tree.Root.Body.TextLines[0].Text);
    }

    [TestMethod]
    public void Parse_MultiParagraphBody_ParsesAllParagraphs()
    {
        var commit = """
feat: introduce caching layer

This change adds a Redis-backed cache for frequently accessed data.

The cache is configured with a default TTL of 5 minutes and supports
invalidation by key prefix.

Future work will include distributed cache support.
""";
        var tree = Parser.Parse(commit);

        Assert.IsNotNull(tree.Root.Body);
        Assert.HasCount(4, tree.Root.Body.TextLines);
        Assert.Contains("Redis-backed cache", tree.Root.Body.TextLines[0].Text);
        Assert.Contains("default TTL of 5 minutes", tree.Root.Body.TextLines[1].Text);
        Assert.Contains("invalidation by key prefix", tree.Root.Body.TextLines[2].Text);
        Assert.Contains("distributed cache support", tree.Root.Body.TextLines[3].Text);
    }

    [TestMethod]
    public void Parse_BodyWithMarkdownStyle_ParsesCorrectly()
    {
        var commit = """
docs: add API reference

### New Endpoints

- `GET /api/v2/users` — list all users
- `POST /api/v2/users` — create a user

See also: #42
""";
        var tree = Parser.Parse(commit);

        Assert.IsNotNull(tree.Root.Body);
        Assert.IsGreaterThanOrEqualTo(3, tree.Root.Body.TextLines.Count);
        Assert.Contains("New Endpoints", tree.Root.Body.TextLines[0].Text);
    }

    [TestMethod]
    public void Parse_HeaderOnlyNoBody_IsClean()
    {
        var commit = "style: format code with dotnet-format";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.IsNull(tree.Root.Body);
        Assert.IsEmpty(tree.Root.Footers);
    }
}
