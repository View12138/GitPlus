using System.Diagnostics;

namespace GitPlus.Commons.ConventionalCommitSyntaxs.Syntaxs;


/// <summary>表示词法单元前后附着的琐碎内容。</summary>
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
public readonly struct SyntaxTrivia : IEquatable<SyntaxTrivia>
{
    internal SyntaxTrivia(SyntaxKind kind, TextSpan span, string text)
    {
        Kind = kind;
        Span = span;
        Text = text;
    }

    /// <summary>琐碎内容的种类。</summary>
    public SyntaxKind Kind { get; }

    /// <summary>琐碎内容在源码中的区间。</summary>
    public TextSpan Span { get; }

    /// <summary>琐碎内容对应的原始文本。</summary>
    public string Text { get; }

    public override string ToString() => Text;
    public string ToFullString() => Text;
    private string GetDebuggerDisplay() => GetType().Name + " " + Kind + " " + ToString();

    public bool Equals(SyntaxTrivia other) => Kind == other.Kind && Span == other.Span && Text == other.Text;
}
