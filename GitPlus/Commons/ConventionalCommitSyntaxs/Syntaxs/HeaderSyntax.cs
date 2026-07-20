namespace GitPlus.Commons.ConventionalCommitSyntaxs.Syntaxs;


/// <summary>提交首行：&lt;type&gt;["("&lt;scope&gt;")"]["!"]":" " " &lt;subject&gt;</summary>
public sealed class HeaderSyntax : SyntaxNode
{
    internal HeaderSyntax(SyntaxToken typeToken, ScopeSyntax? scope, SyntaxToken? bangToken, SyntaxToken colonToken, SyntaxToken subjectToken) : base(SyntaxKind.Header)
    {
        TypeToken = typeToken;
        Scope = scope;
        BangToken = bangToken;
        ColonToken = colonToken;
        SubjectToken = subjectToken;
    }

    /// <summary>type 标识符（如 feat、fix）。</summary>
    public SyntaxToken TypeToken { get; }

    /// <summary>可选 scope 子节点。</summary>
    public ScopeSyntax? Scope { get; }

    /// <summary>可选的破坏性变更 '!' 标记。</summary>
    public SyntaxToken? BangToken { get; }

    /// <summary>':' 冒号。</summary>
    public SyntaxToken ColonToken { get; }

    /// <summary>subject 描述文本。</summary>
    public SyntaxToken SubjectToken { get; }

    public override TextSpan Span => TextSpan.FromBounds(TypeToken.Span.Start, SubjectToken.Span.End);
    public override TextSpan FullSpan => TextSpan.FromBounds(TypeToken.FullSpan.Start, SubjectToken.FullSpan.End);


    public override IEnumerable<ISyntaxElement> ChildNodesAndTokens()
    {
        yield return TypeToken;
        if (Scope != null)
            yield return Scope;
        if (BangToken.HasValue)
            yield return BangToken.Value;
        yield return ColonToken;
        yield return SubjectToken;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(TypeToken.ToString());
        if (Scope != null) sb.Append(Scope.ToString());
        if (BangToken.HasValue) sb.Append(BangToken.Value.ToString());
        sb.Append(ColonToken.ToString());
        sb.Append(SubjectToken.ToString());
        return sb.ToString();
    }

    public override string ToFullString()
    {
        var sb = new StringBuilder();
        sb.Append(TypeToken.ToFullString());
        if (Scope != null) sb.Append(Scope.ToFullString());
        if (BangToken.HasValue) sb.Append(BangToken.Value.ToFullString());
        sb.Append(ColonToken.ToFullString());
        sb.Append(SubjectToken.ToFullString());
        return sb.ToString();
    }
}
