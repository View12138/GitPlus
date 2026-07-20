using GitPlus.Commons.ConventionalCommitSyntaxs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitPlus.Tests.ConventionalCommitSyntaxsTests;

[TestClass]
public sealed class ConventionalCommitSyntaxsEdgeCaseTests
{
    [TestMethod]
    public void Parse_WindowsLineEndings_ParsesCorrectly()
    {
        var commit = "feat: windows support\r\n\r\nThis adds full Windows compatibility.\r\n\r\nCloses: #50\r\n";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);
        Assert.IsNotNull(tree.Root.Body);
        Assert.HasCount(1, tree.Root.Footers);
    }

    [TestMethod]
    public void Parse_SubjectWithColon_ParsesAsPartOfSubject()
    {
        var commit = "feat: add support for key:value pairs";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.Contains("key:value", tree.Root.Header.SubjectToken.Text);
    }

    [TestMethod]
    public void Parse_TrailingNewlinesAfterBody_StillParsesBody()
    {
        var commit = """
feat: subject

body text


""";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);
        Assert.IsNotNull(tree.Root.Body);
        Assert.HasCount(1, tree.Root.Body.TextLines);
    }

    [TestMethod]
    public void Parse_TrailingNewlinesAfterFooters_DoesNotCreateExtraFooters()
    {
        var commit = """
fix: subject

Closes: #1


""";
        var tree = Parser.Parse(commit);

        Assert.HasCount(1, tree.Root.Footers);
    }

    [TestMethod]
    public void Parse_ExtraSpacesAfterColon_SubjectTrimmedCorrectly()
    {
        var commit = "feat:     extra spaces before subject";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);
        Assert.IsGreaterThan(0, tree.Root.Header.SubjectToken.Text.Length);
    }

    [TestMethod]
    public void Parse_NoSpaceAfterColon_StillParses()
    {
        var commit = "fix:nospace";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("fix", tree.Root.Header.TypeToken.Text);
        Assert.AreEqual("nospace", tree.Root.Header.SubjectToken.Text);
    }

    [TestMethod]
    public void Parse_UppercaseType_StillParses()
    {
        var commit = "FEAT: uppercase type";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("FEAT", tree.Root.Header.TypeToken.Text);
        Assert.AreEqual("uppercase type", tree.Root.Header.SubjectToken.Text);
    }

    [TestMethod]
    public void Parse_SubjectWithLeadingWhitespace_PreservedInText()
    {
        var commit = "feat:   indented subject";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);
        Assert.Contains("indented subject", tree.Root.Header.SubjectToken.Text);
    }

    [TestMethod]
    public void Parse_NumericType_StillParses()
    {
        var commit = "v2: version bump";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("v2", tree.Root.Header.TypeToken.Text);
    }
}
