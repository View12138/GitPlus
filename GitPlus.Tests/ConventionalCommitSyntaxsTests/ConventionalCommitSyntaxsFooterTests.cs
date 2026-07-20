using GitPlus.Commons.ConventionalCommitSyntaxs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitPlus.Tests.ConventionalCommitSyntaxsTests;

[TestClass]
public sealed class ConventionalCommitSyntaxsFooterTests
{
    [TestMethod]
    public void Parse_SingleFooter_ParsesCorrectly()
    {
        var commit = """
fix: patch security vulnerability

CVE: CVE-2024-12345
""";
        var tree = Parser.Parse(commit);

        Assert.HasCount(1, tree.Root.Footers);
        Assert.AreEqual("CVE", tree.Root.Footers[0].KeyToken.Text);
        Assert.AreEqual("CVE-2024-12345", tree.Root.Footers[0].ValueToken.Text);
    }

    [TestMethod]
    public void Parse_MultipleFooters_ParsesAll()
    {
        var commit = """
feat: add export to CSV

Closes: #100
Signed-off-by: Jane Doe <jane@example.com>
Reviewed-by: John Smith
""";
        var tree = Parser.Parse(commit);

        Assert.HasCount(3, tree.Root.Footers);
        Assert.AreEqual("Closes", tree.Root.Footers[0].KeyToken.Text);
        Assert.AreEqual("#100", tree.Root.Footers[0].ValueToken.Text);
        Assert.AreEqual("Signed-off-by", tree.Root.Footers[1].KeyToken.Text);
        Assert.AreEqual("Jane Doe <jane@example.com>", tree.Root.Footers[1].ValueToken.Text);
        Assert.AreEqual("Reviewed-by", tree.Root.Footers[2].KeyToken.Text);
        Assert.AreEqual("John Smith", tree.Root.Footers[2].ValueToken.Text);
    }

    [TestMethod]
    public void Parse_BreakingChangeFooter_AsSingleFooter()
    {
        var commit = """
feat: redesign user profile page

BREAKING CHANGE: the old `/profile` endpoint is removed
""";
        var tree = Parser.Parse(commit);

        Assert.HasCount(1, tree.Root.Footers);
        var footer = tree.Root.Footers[0];
        Assert.AreEqual("BREAKING CHANGE", footer.KeyToken.Text);
        Assert.AreEqual("the old `/profile` endpoint is removed", footer.ValueToken.Text);
    }

    [TestMethod]
    public void Parse_FooterWithUrl_ParsesCorrectly()
    {
        var commit = """
fix: update dependency

See-also: https://github.com/example/repo/pull/42
""";
        var tree = Parser.Parse(commit);

        Assert.HasCount(1, tree.Root.Footers);
        Assert.AreEqual("See-also", tree.Root.Footers[0].KeyToken.Text);
        Assert.AreEqual("https://github.com/example/repo/pull/42", tree.Root.Footers[0].ValueToken.Text);
    }

    [TestMethod]
    public void Parse_FooterWithMultipleRefs_ParsesCorrectly()
    {
        var commit = """
fix: resolve memory leak

Fixes: #42, #43, #44
""";
        var tree = Parser.Parse(commit);

        Assert.HasCount(1, tree.Root.Footers);
        Assert.AreEqual("Fixes", tree.Root.Footers[0].KeyToken.Text);
        Assert.AreEqual("#42, #43, #44", tree.Root.Footers[0].ValueToken.Text);
    }

    [TestMethod]
    public void Parse_FootersOnlyNoBody_ParsesCorrectly()
    {
        var commit = """
feat: add dark mode toggle

Closes: #200
""";
        var tree = Parser.Parse(commit);

        Assert.IsNull(tree.Root.Body);
        Assert.HasCount(1, tree.Root.Footers);
        Assert.AreEqual("Closes", tree.Root.Footers[0].KeyToken.Text);
    }

    [TestMethod]
    public void Parse_ManyFooters_ParsesAll()
    {
        var commit = """
feat: major overhaul of settings UI

BREAKING CHANGE: settings format changed from XML to JSON
Migration-guide: see docs/migration-v3.md
Closes: #500
Signed-off-by: Alice <alice@example.com>
Reviewed-by: Bob
Acked-by: Charlie
""";
        var tree = Parser.Parse(commit);

        Assert.HasCount(6, tree.Root.Footers);
    }
}
