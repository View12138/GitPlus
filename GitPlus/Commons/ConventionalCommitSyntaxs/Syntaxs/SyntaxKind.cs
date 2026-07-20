namespace GitPlus.Commons.ConventionalCommitSyntaxs.Syntaxs;

/// <summary>所有词法单元（Token）、语法节点（Node）、琐碎内容（Trivia）的种类。</summary>
public enum SyntaxKind : ushort
{
    /// <summary>未初始化 / 未知。</summary>
    None,

    // Token 词法单元
    /// <summary>标识符（type 名、scope 名、key 名等）。</summary>
    IdentifierToken,
    /// <summary>'(' 左括号。</summary>
    LeftParenToken,
    /// <summary>')' 右括号。</summary>
    RightParenToken,
    /// <summary>'!' 破坏性变更标记。</summary>
    BangToken,
    /// <summary>':' 冒号（header 与 footer 共用，由解析器区分语义）。</summary>
    ColonToken,
    /// <summary>普通文本（body / footer 中的内容行）。</summary>
    TextToken,
    /// <summary>行尾换行标记。</summary>
    EndOfLineToken,
    /// <summary>输入结束标记。</summary>
    EndOfFileToken,

    // Trivia
    /// <summary>空白字符序列（空格、制表符等）。</summary>
    WhitespaceTrivia,
    /// <summary>行尾换行标记。</summary>
    EndOfLineTrivia,

    // Syntax 语法节点
    /// <summary>整条提交消息的根节点。</summary>
    Commit,
    /// <summary>提交首行：type(scope)!: subject。</summary>
    Header,
    /// <summary>可选的 (scope) 部分。</summary>
    Scope,
    /// <summary>可选的正文段落。</summary>
    Body,
    /// <summary>可选的脚注区块。</summary>
    Footer,
}
