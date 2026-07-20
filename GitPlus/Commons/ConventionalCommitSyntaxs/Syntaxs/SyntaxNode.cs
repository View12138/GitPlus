namespace GitPlus.Commons.ConventionalCommitSyntaxs.Syntaxs;

/// <summary>
/// 约定式提交语法树中所有节点的抽象基类。
/// 每个节点是不可变的，持有子节点引用与源码区间。
/// </summary>
public abstract class SyntaxNode : ISyntaxElement
{
    internal SyntaxNode(SyntaxKind kind)
    {
        Kind = kind;
    }

    public SyntaxKind Kind { get; }

    public string Text => ToFullString();

    public abstract TextSpan Span { get; }

    public abstract TextSpan FullSpan { get; }

    /// <summary>子节点集合。</summary>
    public abstract IEnumerable<ISyntaxElement> ChildNodesAndTokens();

    /// <summary>所有后代节点（深度优先）。</summary>
    //public IEnumerable<ISyntaxElement> DescendantNodesAndTokens(bool includeSelf = false)
    //{
    //    if (includeSelf)
    //        yield return this;
    //    foreach (var child in ChildNodesAndTokens())
    //    {
    //        yield return child;
    //        foreach (var descendant in child.DescendantNodesAndTokens())
    //            yield return descendant;
    //    }
    //}

    /// <summary>将节点子树写回为原始文本（含 trivia）。</summary>
    public abstract string ToFullString();
}
