using System.Linq;
using GitPlus.Commons.ConventionalCommitSyntaxs;
using GitPlus.Commons.ConventionalCommitSyntaxs.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitPlus.Tests.ConventionalCommitSyntaxsTests;

[TestClass]
public sealed class ConventionalCommitSyntaxsRealWorldCommitTests
{
    [TestMethod]
    public void Parse_RealWorldCommit_FixWithScopeAndBody()
    {
        var commit = """
fix: prevent racing of requests

Introduce a request id and a reference to latest request. Dismiss
incoming responses other than from latest request.

Remove timeouts which were used to mitigate the racing issue but are
obsolete now.

Reviewed-by: Z
Refs: #123
""";
        var tree = Parser.Parse(commit);

        // Complex real-world commits may have diagnostic warnings about blank lines,
        // but the structure should still parse correctly
        Assert.AreEqual("fix", tree.Root.Header.TypeToken.Text);
        Assert.IsNotNull(tree.Root.Body);
        Assert.HasCount(2, tree.Root.Footers);
        Assert.AreEqual("Reviewed-by", tree.Root.Footers[0].KeyToken.Text);
        Assert.AreEqual("Z", tree.Root.Footers[0].ValueToken.Text);
        Assert.AreEqual("Refs", tree.Root.Footers[1].KeyToken.Text);
        Assert.AreEqual("#123", tree.Root.Footers[1].ValueToken.Text);
    }

    [TestMethod]
    public void Parse_RealWorldCommit_BreakingChangeWithBody()
    {
        var commit = """
fix(scope)!: this is subject

this is description

BREAKING CHANGE: this is breaking change
""";
        var tree = Parser.Parse(commit);

        Assert.IsNotNull(tree.Root.Header.BangToken);
        Assert.IsNotNull(tree.Root.Body);
        Assert.HasCount(1, tree.Root.Footers);
        Assert.AreEqual("BREAKING CHANGE", tree.Root.Footers[0].KeyToken.Text);
        Assert.AreEqual("this is breaking change", tree.Root.Footers[0].ValueToken.Text);
    }

    [TestMethod]
    public void Parse_RealWorldCommit_EmptyScopeWithBang()
    {
        var commit = "fix()!: this is subject";
        var tree = Parser.Parse(commit);

        Assert.IsNotNull(tree.Root.Header.Scope);
        Assert.IsNotNull(tree.Root.Header.BangToken);
        Assert.IsTrue(tree.HasErrors);

        // Missing scope name (CC005) at the ')' position (4,1)
        Assert.HasCount(1, tree.Diagnostics);
        var diagnostic = tree.Diagnostics[0];
        Assert.AreEqual(DiagnosticDescriptor.MissingScope.Id, diagnostic.Id);
        Assert.AreEqual(new TextSpan(4, 1), diagnostic.Span);
    }

    [TestMethod]
    public void Parse_MultipleBodyParagraphs_AllPreserved()
    {
        var commit = """
docs: update readme

First paragraph of the description.

Second paragraph with more details.

Third paragraph.
""";
        var tree = Parser.Parse(commit);
        Assert.IsNotNull(tree.Root.Body);
        Assert.HasCount(3, tree.Root.Body.TextLines);
    }

    [TestMethod]
    public void Parse_FooterKeyWithSpace_BreakingChangePattern()
    {
        var commit = """
fix: subject

BREAKING CHANGE: description here
""";
        var tree = Parser.Parse(commit);
        var footer = tree.Root.Footers[0];
        Assert.AreEqual("BREAKING CHANGE", footer.KeyToken.Text);
        Assert.AreEqual("description here", footer.ValueToken.Text);
    }

    [TestMethod]
    public void Parse_AngularStyleCommit_ParsesCorrectly()
    {
        var commit = """
feat($compile): add unit testing guide

Angular style commit with a dollar sign in scope.

Closes: #123
""";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);
        Assert.IsNotNull(tree.Root.Header.Scope);
        Assert.AreEqual("$compile", tree.Root.Header.Scope.ScopeNameToken.Text);
        Assert.IsNotNull(tree.Root.Body);
        Assert.HasCount(1, tree.Root.Footers);
        Assert.AreEqual("Closes", tree.Root.Footers[0].KeyToken.Text);
    }

    [TestMethod]
    public void Parse_CommitWithMultipleFootersAndBody_ParsesAll()
    {
        var commit = """
feat: add new authentication system

This adds OAuth2 support with several providers.
See the documentation for more details.

BREAKING CHANGE: removed legacy password auth
Migration-guide: see docs/migration-v2.md
Fixes: #42, #43
""";
        var tree = Parser.Parse(commit);

        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);
        Assert.IsNotNull(tree.Root.Body);
        Assert.Contains(t => t.Text.Contains("OAuth2"), tree.Root.Body.TextLines);
        Assert.HasCount(3, tree.Root.Footers);
        Assert.AreEqual("BREAKING CHANGE", tree.Root.Footers[0].KeyToken.Text);
        Assert.AreEqual("Migration-guide", tree.Root.Footers[1].KeyToken.Text);
        Assert.AreEqual("Fixes", tree.Root.Footers[2].KeyToken.Text);
    }

    [TestMethod]
    public void Parse_SimpleCommitWithNoScopeNoBang_IsClean()
    {
        var commit = "chore: bump version to 2.0.0";
        var tree = Parser.Parse(commit);

        Assert.IsFalse(tree.HasErrors);
        Assert.AreEqual("chore", tree.Root.Header.TypeToken.Text);
        Assert.IsNull(tree.Root.Header.Scope);
        Assert.IsNull(tree.Root.Header.BangToken);
        Assert.AreEqual("bump version to 2.0.0", tree.Root.Header.SubjectToken.Text);
        Assert.IsNull(tree.Root.Body);
        Assert.IsEmpty(tree.Root.Footers);
    }
}
