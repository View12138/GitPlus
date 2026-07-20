using System.Linq;
using GitPlus.Commons.ConventionalCommitSyntaxs;
using GitPlus.Commons.ConventionalCommitSyntaxs.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitPlus.Tests.ConventionalCommitSyntaxsTests;

[TestClass]
public sealed class ConventionalCommitSyntaxsSettingsTests
{
    [TestMethod]
    public void Parse_WithAllowedTypes_ValidType_HasNoErrors()
    {
        var settings = new ParserSettings { AllowTypes = ["feat", "fix"] };
        var commit = "feat: add feature";
        var tree = Parser.Parse(commit, settings);

        Assert.IsFalse(tree.HasErrors);
    }

    [TestMethod]
    public void Parse_WithAllowedTypes_InvalidType_HasErrors()
    {
        var settings = new ParserSettings { AllowTypes = ["feat", "fix"] };
        var commit = "chore: update deps";
        var tree = Parser.Parse(commit, settings);

        Assert.IsTrue(tree.HasErrors);
        Assert.HasCount(1, tree.Diagnostics);
        var diagnostic = tree.Diagnostics[0];
        Assert.AreEqual(DiagnosticDescriptor.InvalidCommitType.Id, diagnostic.Id);
        Assert.AreEqual(new TextSpan(0, 5), diagnostic.Span); // "chore" at (0,5)
    }

    [TestMethod]
    public void Parse_WithAllowedScopes_ValidScope_HasNoErrors()
    {
        var settings = new ParserSettings { AllowScopes = ["core", "ui"] };
        var commit = "fix(core): resolve bug";
        var tree = Parser.Parse(commit, settings);

        Assert.IsFalse(tree.HasErrors);
    }

    [TestMethod]
    public void Parse_WithAllowedScopes_InvalidScope_HasErrors()
    {
        var settings = new ParserSettings { AllowScopes = ["core", "ui"] };
        var commit = "fix(api): resolve bug";
        var tree = Parser.Parse(commit, settings);

        Assert.IsTrue(tree.HasErrors);
        Assert.HasCount(1, tree.Diagnostics);
        var diagnostic = tree.Diagnostics[0];
        Assert.AreEqual(DiagnosticDescriptor.InvalidScope.Id, diagnostic.Id);
        Assert.AreEqual(new TextSpan(4, 3), diagnostic.Span); // "api" inside parens at (4,3)
    }

    [TestMethod]
    public void Parse_WithAllowedScopes_MissingScope_HasErrors()
    {
        var settings = new ParserSettings { AllowScopes = ["core", "ui"] };
        var commit = "fix: no scope provided";
        var tree = Parser.Parse(commit, settings);

        Assert.IsTrue(tree.HasErrors);

        // CC007 (MissingOpenParen), CC005 (MissingScope ×2), CC008 (MissingCloseParen)
        Assert.HasCount(4, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingOpenParen.Id, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingScope.Id, tree.Diagnostics);
        Assert.Contains(d => d.Id == DiagnosticDescriptor.MissingCloseParen.Id, tree.Diagnostics);
    }

    [TestMethod]
    public void Parse_WithAllowedFooters_ValidFooter_HasNoErrors()
    {
        var settings = new ParserSettings { AllowFooters = ["Closes", "Refs"] };
        var commit = """
fix: subject

Closes: #123
""";
        var tree = Parser.Parse(commit, settings);

        Assert.IsFalse(tree.HasErrors);
    }

    [TestMethod]
    public void Parse_WithAllowedFooters_InvalidFooter_HasErrors()
    {
        var settings = new ParserSettings { AllowFooters = ["Closes", "Refs"] };
        var commit = """
fix: subject

Signed-off-by: someone
""";
        var tree = Parser.Parse(commit, settings);

        Assert.IsTrue(tree.HasErrors);
        Assert.HasCount(1, tree.Diagnostics);
        var diagnostic = tree.Diagnostics[0];
        Assert.AreEqual(DiagnosticDescriptor.InvalidFooterKey.Id, diagnostic.Id);
        Assert.AreEqual(new TextSpan(12, 13), diagnostic.Span); // "Signed-off-by" key span starts at first newline (12)
    }

    [TestMethod]
    public void Parse_WithAllSettings_AllValid_HasNoErrors()
    {
        var settings = new ParserSettings
        {
            AllowTypes = ["feat", "fix", "docs", "style", "refactor", "perf", "test", "build", "ci", "chore", "revert"],
            AllowScopes = ["api", "core", "ui"],
            AllowFooters = ["Closes", "Refs", "BREAKING CHANGE", "Signed-off-by", "Reviewed-by"],
        };
        var commit = """
fix(api)!: change return type

This is a body description that explains the motivation.

BREAKING CHANGE: old format is no longer supported

Closes: #300
Signed-off-by: dev <dev@example.com>
""";
        var tree = Parser.Parse(commit, settings);

        Assert.IsFalse(tree.HasErrors);
        Assert.AreEqual("fix", tree.Root.Header.TypeToken.Text);
        Assert.AreEqual("api", tree.Root.Header.Scope?.ScopeNameToken.Text);
        Assert.IsNotNull(tree.Root.Header.BangToken);
        Assert.IsNotNull(tree.Root.Body);
        Assert.HasCount(1, tree.Root.Body.TextLines);
        Assert.HasCount(3, tree.Root.Footers);
    }
}
