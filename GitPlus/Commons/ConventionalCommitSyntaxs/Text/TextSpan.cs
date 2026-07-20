namespace GitPlus.Commons.ConventionalCommitSyntaxs.Text;

/// <summary>表示源代码中的一个不可变区间。</summary>
public readonly struct TextSpan(int start, int length) : IEquatable<TextSpan>
{
    /// <summary>区间起始位置。</summary>
    public int Start { get; } = start;

    /// <summary>区间结束位置。</summary>
    public readonly int End => Start + Length;

    /// <summary>区间长度。</summary>
    public int Length { get; } = length;

    /// <summary>从起止位置创建（end 不含）。</summary>
    public static TextSpan FromBounds(int start, int end) => new(start, end - start);

    public override string ToString() => $"[{Start}..{End})";


    public bool Equals(TextSpan other) => Start == other.Start && Length == other.Length;
    public override bool Equals(object obj) => obj is TextSpan textSpan && Equals(textSpan);
    public override int GetHashCode() => Hash.Combine(Start, Length);
    public static bool operator ==(TextSpan left, TextSpan right) => left.Equals(right);
    public static bool operator !=(TextSpan left, TextSpan right) => !left.Equals(right);
}
