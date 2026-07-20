using System.Diagnostics;

namespace GitPlus.Commons.ConventionalCommitSyntaxs.Syntaxs;

/// <summary>词法单元</summary>
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
public readonly struct SyntaxToken : ISyntaxElement, IEquatable<SyntaxToken>
{
    internal SyntaxToken(SyntaxKind kind, TextSpan span, string text,
        ImmutableArray<SyntaxTrivia>? leadingTrivia = null, ImmutableArray<SyntaxTrivia>? trailingTrivia = null,
        bool isMissing = false)
    {
        Kind = kind;
        Span = span;
        Text = text;
        LeadingTrivia = leadingTrivia ?? ImmutableArray.Create<SyntaxTrivia>();
        TrailingTrivia = trailingTrivia ?? ImmutableArray.Create<SyntaxTrivia>();
        IsMissing = isMissing;
    }

    public SyntaxKind Kind { get; }

    /// <summary>本 Token 在源码中的核心区间（不含 trivia）。</summary>
    public TextSpan Span { get; }

    /// <summary>本 Token 在源码中的完整区间（含前导 trivia + 核心文本 + 尾随 trivia）。</summary>
    public TextSpan FullSpan => TextSpan.FromBounds(LeadingTrivia.Any() ? LeadingTrivia.First().Span.Start : Span.Start, TrailingTrivia.Any() ? TrailingTrivia.Last().Span.End : Span.End);

    /// <summary>Token 对应的原始文本。</summary>
    public string Text { get; }

    /// <summary>前导琐碎内容。</summary>
    public ImmutableArray<SyntaxTrivia> LeadingTrivia { get; }

    /// <summary>尾随琐碎内容。</summary>
    public ImmutableArray<SyntaxTrivia> TrailingTrivia { get; }

    /// <summary>是否为缺失的 Token（由错误恢复插入的占位 Token）。</summary>
    public bool IsMissing { get; }

    public override string ToString() => Text;
    public string ToFullString() => $"{string.Join(string.Empty, LeadingTrivia.Select(t => t.Text))}{Text}{string.Join(string.Empty, TrailingTrivia.Select(t => t.Text))}";
    private string GetDebuggerDisplay() => GetType().Name + " " + Kind + " " + ToString();

    public bool Equals(SyntaxToken other) => Kind == other.Kind && Span == other.Span && Text == other.Text;
}
