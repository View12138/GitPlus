using System.Linq;
using GitPlus.Commons.ConventionalCommitSyntaxs;
using GitPlus.Commons.ConventionalCommitSyntaxs.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitPlus.Tests.ConventionalCommitSyntaxsTests;

[TestClass]
public sealed class ConventionalCommitSyntaxsErrorTests
{
    [TestMethod]
    public void Parse_EmptyString_HasErrors()
    {
        var commit = "";
        var tree = Parser.Parse(commit);

        Assert.IsTrue(tree.HasErrors);
        Assert.IsNotNull(tree.Root);
        Assert.IsNotNull(tree.Root.Header);

        // Missing type, colon, and subject
        Assert.HasCount(3, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingCommitType.Id, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingColon.Id, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingTextValue.Id, tree.Diagnostics);
        // All missing tokens are at position 0
        foreach (var diagnostic in tree.Diagnostics)
        {
            Assert.AreEqual(new TextSpan(0, 0), diagnostic.Span);
        }
    }

    [TestMethod]
    public void Parse_OnlyTypeNoColonNoSubject_HasErrors()
    {
        var commit = "feat";
        var tree = Parser.Parse(commit);

        Assert.IsTrue(tree.HasErrors);
        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);

        // Missing colon and subject
        Assert.HasCount(2, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingColon.Id, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingTextValue.Id, tree.Diagnostics);
        // Both diagnostics at position 4 (end of "feat")
        foreach (var diagnostic in tree.Diagnostics)
        {
            Assert.AreEqual(new TextSpan(4, 0), diagnostic.Span);
        }
    }

    [TestMethod]
    public void Parse_MissingSubject_HasErrors()
    {
        var commit = "feat: ";
        var tree = Parser.Parse(commit);

        Assert.IsTrue(tree.HasErrors);
        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);

        // Only missing subject text
        Assert.HasCount(1, tree.Diagnostics);
        var diagnostic = tree.Diagnostics[0];
        Assert.AreEqual(DiagnosticDescriptor.MissingTextValue.Id, diagnostic.Id);
        Assert.AreEqual(new TextSpan(5, 0), diagnostic.Span);
    }

    [TestMethod]
    public void Parse_MissingColon_ReportsErrorButParsesRest()
    {
        var commit = "feat add feature without colon";
        var tree = Parser.Parse(commit);

        Assert.IsTrue(tree.HasErrors);
        Assert.AreEqual("feat", tree.Root.Header.TypeToken.Text);
        // Subject contains the remainder after the type
        Assert.IsGreaterThan(0, tree.Root.Header.SubjectToken.Text.Length);

        // Missing colon at the position of "add" token (position 5, length 3)
        Assert.HasCount(1, tree.Diagnostics);
        var diagnostic = tree.Diagnostics[0];
        Assert.AreEqual(DiagnosticDescriptor.MissingColon.Id, diagnostic.Id);
        Assert.AreEqual(new TextSpan(5, 3), diagnostic.Span);
    }

    [TestMethod]
    public void Parse_UnclosedScopeParen_HasErrors()
    {
        var commit = "fix(parser: missing close paren";
        var tree = Parser.Parse(commit);

        Assert.IsTrue(tree.HasErrors);
        Assert.IsNotNull(tree.Root.Header.Scope);

        // Missing ')' at the colon that appears instead (position 10, length 1)
        Assert.HasCount(1, tree.Diagnostics);
        var diagnostic = tree.Diagnostics[0];
        Assert.AreEqual(DiagnosticDescriptor.MissingCloseParen.Id, diagnostic.Id);
        Assert.AreEqual(new TextSpan(10, 1), diagnostic.Span);
    }

    [TestMethod]
    public void Parse_ScopeWithSpace_HasErrors()
    {
        var commit = "feat(my scope): has space in scope";
        var tree = Parser.Parse(commit);

        Assert.IsTrue(tree.HasErrors);

        // CC008 (MissingCloseParen) and CC009 (MissingColon) both at "scope" token (position 8, length 5)
        Assert.HasCount(2, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingCloseParen.Id, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingColon.Id, tree.Diagnostics);
        foreach (var diagnostic in tree.Diagnostics)
        {
            Assert.AreEqual(new TextSpan(8, 5), diagnostic.Span);
        }
    }

    [TestMethod]
    public void Parse_FooterWithMissingValue_HasErrors()
    {
        var commit = """
fix: subject

Signed-off-by:
""";
        var tree = Parser.Parse(commit);

        Assert.IsTrue(tree.HasErrors);
        Assert.HasCount(1, tree.Root.Footers);
        Assert.AreEqual("Signed-off-by", tree.Root.Footers[0].KeyToken.Text);

        // Missing footer value (CC010) and missing space before value (CC003)
        Assert.HasCount(2, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingTextValue.Id, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingSpace.Id, tree.Diagnostics);
    }
}
