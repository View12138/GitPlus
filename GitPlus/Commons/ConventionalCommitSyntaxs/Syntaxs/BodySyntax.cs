namespace GitPlus.Commons.ConventionalCommitSyntaxs.Syntaxs;

/// <summary>可选的正文段落。</summary>
public sealed class BodySyntax : SyntaxNode
{
    internal BodySyntax(IReadOnlyList<SyntaxToken> textLines) : base(SyntaxKind.Body)
    {
        TextLines = textLines;
    }

    /// <summary>正文文本行（含行内换行）。</summary>
    public IReadOnlyList<SyntaxToken> TextLines { get; }

    public override TextSpan Span => TextSpan.FromBounds(TextLines.First().Span.Start, TextLines.Last().Span.End);
    public override TextSpan FullSpan => TextSpan.FromBounds(TextLines.First().FullSpan.Start, TextLines.Last().FullSpan.End);


    public override IEnumerable<ISyntaxElement> ChildNodesAndTokens()
    {
        foreach (var line in TextLines)
            yield return line;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var line in TextLines)
            sb.Append(line.ToString());
        return sb.ToString();
    }
    public override string ToFullString()
    {
        var sb = new StringBuilder();
        foreach (var line in TextLines)
            sb.Append(line.ToFullString());
        return sb.ToString();
    }
}
