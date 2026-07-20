namespace GitPlus.Commons.ConventionalCommitSyntaxs.Syntaxs;

/// <summary>
/// 表示一次完整的约定式提交语法解析结果。
/// 持有语法树根节点与解析器层面的诊断信息。
/// </summary>
public sealed class SyntaxTree
{
    internal SyntaxTree(CommitSyntax root, IReadOnlyList<Diagnostic> diagnostics, string sourceText)
    {
        Root = root;
        Diagnostics = diagnostics;
        SourceText = sourceText;
    }

    /// <summary>语法树根节点。</summary>
    public CommitSyntax Root { get; }

    /// <summary>解析器层面收集的诊断信息。</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>是否存在错误级别诊断。</summary>
    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>原始提交消息文本。</summary>
    public string SourceText { get; }


    public override string ToString() => Root.ToString();
    public string ToFullString() => Root.ToFullString();
}
