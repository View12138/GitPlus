namespace GitPlus.Commons.ConventionalCommitSyntaxs.Syntaxs;

/// <summary>(scope) 语法。</summary>
public sealed class ScopeSyntax : SyntaxNode
{
    internal ScopeSyntax(SyntaxToken openParen, SyntaxToken scopeName, SyntaxToken closeParen) : base(SyntaxKind.Scope)
    {
        OpenParenToken = openParen;
        ScopeNameToken = scopeName;
        CloseParenToken = closeParen;
    }

    /// <summary>'(' Token。</summary>
    public SyntaxToken OpenParenToken { get; }

    /// <summary>scope 名称。</summary>
    public SyntaxToken ScopeNameToken { get; }

    /// <summary>')' Token。</summary>
    public SyntaxToken CloseParenToken { get; }

    public override TextSpan Span => TextSpan.FromBounds(OpenParenToken.Span.Start, CloseParenToken.Span.End);
    public override TextSpan FullSpan => TextSpan.FromBounds(OpenParenToken.FullSpan.Start, CloseParenToken.FullSpan.End);

    public override IEnumerable<ISyntaxElement> ChildNodesAndTokens()
    {
        yield return OpenParenToken;
        yield return ScopeNameToken;
        yield return CloseParenToken;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(OpenParenToken.ToString());
        sb.Append(ScopeNameToken.ToString());
        sb.Append(CloseParenToken.ToString());
        return sb.ToString();
    }
    public override string ToFullString()
    {
        var sb = new StringBuilder();
        sb.Append(OpenParenToken.ToFullString());
        sb.Append(ScopeNameToken.ToFullString());
        sb.Append(CloseParenToken.ToFullString());
        return sb.ToString();
    }
}
