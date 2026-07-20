using GitPlus.Commons.ConventionalCommitSyntaxs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitPlus.Tests.ConventionalCommitSyntaxsTests;

[TestClass]
public sealed class ConventionalCommitSyntaxsHeaderTests
{
    [TestMethod]
    public void Parse_AllStandardTypes_ParseCorrectly()
    {
        var types = new[] { "feat", "fix", "docs", "style", "refactor", "perf", "test", "build", "ci", "chore", "revert" };
        foreach (var type in types)
        {
            var commit = $"{type}: subject text";
            var tree = Parser.Parse(commit);
            Assert.IsFalse(tree.HasErrors, $"Type '{type}' should parse without errors");
            Assert.AreEqual(type, tree.Root.Header.TypeToken.Text);
            Assert.AreEqual("subject text", tree.Root.Header.SubjectToken.Text);
        }
    }

    [TestMethod]
    public void Parse_TypeAndScope_ParsesCorrectly()
    {
        var commit = "feat(parser): add new parser";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);
        Assert.IsNotNull(tree.Root.Header.Scope);
        Assert.AreEqual("parser", tree.Root.Header.Scope.ScopeNameToken.Text);
        Assert.IsNull(tree.Root.Header.BangToken);
        Assert.AreEqual("add new parser", tree.Root.Header.SubjectToken.Text);
    }

    [TestMethod]
    public void Parse_TypeAndBang_ParsesCorrectly()
    {
        var commit = "feat!: drop support for legacy API";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);
        Assert.IsNull(tree.Root.Header.Scope);
        Assert.IsNotNull(tree.Root.Header.BangToken);
        Assert.AreEqual("drop support for legacy API", tree.Root.Header.SubjectToken.Text);
    }

    [TestMethod]
    public void Parse_TypeScopeAndBang_ParsesCorrectly()
    {
        var commit = "feat(api)!: change authentication flow";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);
        Assert.IsNotNull(tree.Root.Header.Scope);
        Assert.AreEqual("api", tree.Root.Header.Scope.ScopeNameToken.Text);
        Assert.IsNotNull(tree.Root.Header.BangToken);
        Assert.AreEqual("change authentication flow", tree.Root.Header.SubjectToken.Text);
    }

    [TestMethod]
    public void Parse_ScopeWithDot_ParsesCorrectly()
    {
        var commit = "fix(compiler.core): resolve null reference";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.AreEqual("compiler.core", tree.Root.Header.Scope?.ScopeNameToken.Text);
    }

    [TestMethod]
    public void Parse_ScopeWithHyphen_ParsesCorrectly()
    {
        var commit = "feat(ui-components): add button variant";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.AreEqual("ui-components", tree.Root.Header.Scope?.ScopeNameToken.Text);
    }

    [TestMethod]
    public void Parse_ScopeWithSlash_ParsesCorrectly()
    {
        var commit = "fix(core/parser): handle edge case";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.AreEqual("core/parser", tree.Root.Header.Scope?.ScopeNameToken.Text);
    }

    [TestMethod]
    public void Parse_ScopeWithUnderscore_ParsesCorrectly()
    {
        var commit = "feat(auth_module): add login";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.AreEqual("auth_module", tree.Root.Header.Scope?.ScopeNameToken.Text);
    }

    [TestMethod]
    public void Parse_LongSubject_ParsesCorrectly()
    {
        var commit = "docs: update the installation guide with detailed step-by-step instructions for all supported platforms";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.AreEqual("docs", tree.Root.Header.TypeToken.Text);
        Assert.StartsWith("update the installation guide", tree.Root.Header.SubjectToken.Text);
    }

    [TestMethod]
    public void Parse_SubjectWithUrl_ParsesCorrectly()
    {
        var commit = "docs: see https://example.com/docs for more details";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.Contains("https://example.com/docs", tree.Root.Header.SubjectToken.Text);
    }

    [TestMethod]
    public void Parse_SubjectWithHashRefs_ParsesCorrectly()
    {
        var commit = "fix: resolve issue #42 and #43";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.Contains("#42", tree.Root.Header.SubjectToken.Text);
        Assert.Contains("#43", tree.Root.Header.SubjectToken.Text);
    }

    [TestMethod]
    public void Parse_RevertCommit_ParsesCorrectly()
    {
        var commit = "revert: let us never speak of this again\n\nThis reverts commit abc123.\n";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("revert", tree.Root.Header.TypeToken.Text);
        Assert.IsNotNull(tree.Root.Body);
    }
}
