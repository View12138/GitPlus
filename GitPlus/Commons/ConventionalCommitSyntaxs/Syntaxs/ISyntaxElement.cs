namespace GitPlus.Commons.ConventionalCommitSyntaxs.Syntaxs;

public interface ISyntaxElement
{
    /// <summary>类型</summary>
    public SyntaxKind Kind { get; }

    /// <summary>在源码中的核心区间（不含 trivia）。</summary>
    public TextSpan Span { get; }

    /// <summary>在源码中的完整区间（含前导 trivia + 核心文本 + 尾随 trivia）。</summary>
    public TextSpan FullSpan { get; }

    /// <summary>原始文本。</summary>
    public string Text { get; }

    /// <summary>完整原始代码</summary>
    public string ToFullString();
}
