namespace GitPlus.Commons.ConventionalCommitSyntaxs.Syntaxs;

/// <summary>脚注：BREAKING CHANGE: &lt;text&gt; 或 &lt;key&gt;: &lt;value&gt;。</summary>
public sealed class FooterSyntax : SyntaxNode
{
    internal FooterSyntax(SyntaxToken keyToken, SyntaxToken colonToken, SyntaxToken valueToken) : base(SyntaxKind.Footer)
    {
        KeyToken = keyToken;
        ColonToken = colonToken;
        ValueToken = valueToken;
    }

    /// <summary>key 文本。</summary>
    public SyntaxToken KeyToken { get; }

    /// <summary>':' 分隔符。</summary>
    public SyntaxToken ColonToken { get; }

    /// <summary>冒号后的内容（含前导空格）。</summary>
    public SyntaxToken ValueToken { get; }

    public override TextSpan Span => TextSpan.FromBounds(KeyToken.Span.Start, ValueToken.Span.End);
    public override TextSpan FullSpan => TextSpan.FromBounds(KeyToken.FullSpan.Start, ValueToken.FullSpan.End);


    public override IEnumerable<ISyntaxElement> ChildNodesAndTokens()
    {
        yield return KeyToken;
        yield return ColonToken;
        yield return ValueToken;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(KeyToken.ToString());
        sb.Append(ColonToken.ToString());
        sb.Append(ValueToken.ToString());
        return sb.ToString();
    }
    public override string ToFullString()
    {
        var sb = new StringBuilder();
        sb.Append(KeyToken.ToFullString());
        sb.Append(ColonToken.ToFullString());
        sb.Append(ValueToken.ToFullString());
        return sb.ToString();
    }
}
