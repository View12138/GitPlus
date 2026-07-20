using GitPlus.Commons.ConventionalCommitSyntaxs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitPlus.Tests.ConventionalCommitSyntaxsTests;

[TestClass]
public sealed class ConventionalCommitSyntaxsCombinedTests
{
    [TestMethod]
    public void Parse_FullCommit_HeaderBodyFooters()
    {
        var commit = """
feat(api)!: introduce GraphQL endpoint

This replaces the old REST endpoint with a new GraphQL API
that supports batched queries and selective field loading.

BREAKING CHANGE: REST endpoint `/api/v1` is removed
Migration-guide: see docs/migration-v3.md
Closes: #999
""";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);
        Assert.AreEqual("api", tree.Root.Header.Scope?.ScopeNameToken.Text);
        Assert.IsNotNull(tree.Root.Header.BangToken);
        Assert.IsNotNull(tree.Root.Body);
        Assert.IsGreaterThanOrEqualTo(2, tree.Root.Body.TextLines.Count);
        Assert.HasCount(3, tree.Root.Footers);
    }

    [TestMethod]
    public void Parse_TypeBangBody_ParsesCorrectly()
    {
        var commit = """
refactor!: simplify error handling

All custom error types are consolidated into AppException.
""";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("refactor", tree.Root.Header.TypeToken.Text);
        Assert.IsNotNull(tree.Root.Header.BangToken);
        Assert.IsNotNull(tree.Root.Body);
        Assert.HasCount(1, tree.Root.Body.TextLines);
        Assert.IsEmpty(tree.Root.Footers);
    }

    [TestMethod]
    public void Parse_TypeScopeBody_ParsesCorrectly()
    {
        var commit = """
perf(parser): optimize tokenization

Reduced allocations by reusing StringBuilder instances.
""";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("perf", tree.Root.Header.TypeToken.Text);
        Assert.AreEqual("parser", tree.Root.Header.Scope?.ScopeNameToken.Text);
        Assert.IsNotNull(tree.Root.Body);
        Assert.IsEmpty(tree.Root.Footers);
    }
}
