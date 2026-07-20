namespace GitPlus.Commons.ConventionalCommitSyntaxs.Syntaxs;

/// <summary>语法树根节点，代表一条完整的约定式提交消息。</summary>
public sealed class CommitSyntax : SyntaxNode
{
    internal CommitSyntax(HeaderSyntax header, BodySyntax? body, IReadOnlyList<FooterSyntax> footers, SyntaxToken eof) : base(SyntaxKind.Commit)
    {
        Header = header;
        Body = body;
        Footers = footers;
        EndOfFileToken = eof;
    }

    /// <summary>提交首行。</summary>
    public HeaderSyntax Header { get; }

    /// <summary>可选正文。</summary>
    public BodySyntax? Body { get; }

    /// <summary>可选脚注。</summary>
    public IReadOnlyList<FooterSyntax> Footers { get; }

    /// <summary>文件结束 Token。</summary>
    public SyntaxToken EndOfFileToken { get; }

    public override TextSpan Span => TextSpan.FromBounds(Header.Span.Start, EndOfFileToken.Span.End);
    public override TextSpan FullSpan => TextSpan.FromBounds(Header.FullSpan.Start, EndOfFileToken.FullSpan.End);

    public override IEnumerable<ISyntaxElement> ChildNodesAndTokens()
    {
        yield return Header;
        if (Body != null)
        { yield return Body; }
        foreach (var footer in Footers)
        { yield return footer; }
        yield return EndOfFileToken;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Header.ToString());
        if (Body != null)
        { sb.Append(Body.ToString()); }
        foreach (var footer in Footers)
        { sb.Append(footer.ToString()); }
        sb.Append(EndOfFileToken.ToString());
        return sb.ToString();
    }
    public override string ToFullString()
    {
        var sb = new StringBuilder();
        sb.Append(Header.ToFullString());
        if (Body != null)
        { sb.Append(Body.ToFullString()); }
        foreach (var footer in Footers)
        { sb.Append(footer.ToFullString()); }
        sb.Append(EndOfFileToken.ToFullString());
        return sb.ToString();
    }
}
