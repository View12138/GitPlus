namespace GitPlus.Commons.ConventionalCommitSyntaxs;

/// <summary>
/// 语法解析器。
/// </summary>
internal sealed class Parser
{
    private readonly IReadOnlyList<SyntaxToken> tokens;
    private int tokenIndex;
    private readonly string sourceText;
    private readonly List<Diagnostic> diagnostics;
    private readonly ParserSettings settings;

    private Parser(IReadOnlyList<SyntaxToken> tokens, string sourceText, ParserSettings? settings = null)
    {
        this.tokens = tokens;
        this.sourceText = sourceText;
        this.settings = settings ?? new ParserSettings();

        tokenIndex = 0;
        diagnostics = [];
    }

    /// <summary>从原始文本解析语法树（词法分析 + 语法分析）。</summary>
    public static SyntaxTree Parse(string text, ParserSettings? settings = null)
    {
        var tokenList = Lexer.Lex(text);
        var parser = new Parser(tokenList, text, settings);
        var root = parser.ParseCommit();
        return new SyntaxTree(root, parser.diagnostics, text);
    }


    private SyntaxToken Current => tokenIndex < tokens.Count ? tokens[tokenIndex] : tokens[tokens.Count - 1];
    private SyntaxKind CurrentKind => Current.Kind;

    private SyntaxToken Advance()
    {
        var token = Current;
        if (tokenIndex < tokens.Count - 1)
            tokenIndex += 1;
        return token;
    }


    private bool IsAt(SyntaxKind kind) => CurrentKind == kind;

    /// <summary>期望当前 Token 为指定种类；否则创建缺失 Token 并报告错误。</summary>
    private SyntaxToken Expect(SyntaxKind kind, DiagnosticDescriptor diagnostic, bool reportDiagnostic = true)
    {
        if (CurrentKind == kind)
            return Advance();

        var missingToken = new SyntaxToken(kind, new TextSpan(Current.Span.Start, 0), string.Empty, isMissing: true);
        if (reportDiagnostic)
        { diagnostics.Add(Diagnostic.Create(diagnostic, Current.Span)); }
        return missingToken;
    }



    /// <summary>
    /// &lt;commit&gt; ::= &lt;header&gt; [&lt;body&gt;] [&lt;footer&gt;]
    /// </summary>
    private CommitSyntax ParseCommit()
    {
        var header = ParseHeader();

        BodySyntax? body = null;
        if (IsAt(SyntaxKind.EndOfLineToken) && !IsFooter())
        {
            body = ParseBody();
        }
        List<FooterSyntax> footers = ParseFooters();

        List<SyntaxTrivia> trivias = [];
        while (IsAt(SyntaxKind.EndOfLineToken) || IsAt(SyntaxKind.WhitespaceTrivia) || IsAt(SyntaxKind.EndOfLineTrivia))
        {
            var token = Advance();
            var triviaKind = token.Kind == SyntaxKind.EndOfLineToken ? SyntaxKind.EndOfLineTrivia : token.Kind;
            trivias.Add(new SyntaxTrivia(triviaKind, token.Span, token.Text));
        }

        while (!IsAt(SyntaxKind.EndOfFileToken))
        { Advance(); }

        foreach (var trivia in Advance().LeadingTrivia)
        { trivias.Add(trivia); }
        var eof = new SyntaxToken(SyntaxKind.EndOfFileToken, new TextSpan(sourceText.Length, 0), string.Empty, trivias.ToImmutableArray());
        if (eof.LeadingTrivia.Length > 0)
        {
            var span = TextSpan.FromBounds(eof.LeadingTrivia.First().Span.Start, eof.LeadingTrivia.Last().Span.End);
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.UnexpectedExtraWhitespace, span));
        }

        return new CommitSyntax(header, body, footers, eof);
    }

    /// <summary>
    /// 从当前位置向后探查，判断当前行是否匹配 footer 行模式。
    /// <para>Footer 行特征：以 Identifier 开头，且在遇到 NewLine/EOF 之前出现 Colon。</para>
    /// </summary>
    /// <example>"BREAKING CHANGE: ..." 或 "Reviewed-by: ..."</example>
    private bool IsFooter()
    {
        if (!IsAt(SyntaxKind.IdentifierToken) && !IsAt(SyntaxKind.EndOfLineToken))
        { return false; }

        var savedIndex = tokenIndex;
        try
        {
            while (IsAt(SyntaxKind.EndOfLineToken))
            { _ = Advance(); }
            while (!IsAt(SyntaxKind.EndOfFileToken) && !IsAt(SyntaxKind.EndOfLineToken))
            {
                _ = Advance();
                if (IsAt(SyntaxKind.ColonToken))
                {
                    return true;
                }
            }
            return false;
        }
        finally
        { tokenIndex = savedIndex; }
    }

    /// <summary>
    /// &lt;header&gt; ::= &lt;type&gt; ["(" &lt;scope&gt; ")"] ["!"] ":" " " &lt;subject&gt;
    /// </summary>
    private HeaderSyntax ParseHeader()
    {
        // type — 必需
        var typeToken = Expect(SyntaxKind.IdentifierToken, DiagnosticDescriptor.MissingCommitType);
        if (!typeToken.IsMissing && settings.AllowTypes.Count > 0 && !settings.AllowTypes.Contains(typeToken.Text))
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.InvalidCommitType, typeToken.Span));
        }

        // scope - 可选
        ScopeSyntax? scope = null;
        if (IsAt(SyntaxKind.LeftParenToken))
        {
            // <scope> ::= "(" <scope-name> ")"
            var openParen = Expect(SyntaxKind.LeftParenToken, DiagnosticDescriptor.MissingOpenParen);
            var scopeName = Expect(SyntaxKind.IdentifierToken, DiagnosticDescriptor.MissingScope);
            if (!scopeName.IsMissing && settings.AllowScopes.Count > 0 && !settings.AllowScopes.Contains(scopeName.Text))
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.InvalidScope, scopeName.Span));
            }
            var closeParen = Expect(SyntaxKind.RightParenToken, DiagnosticDescriptor.MissingCloseParen);
            scope = new ScopeSyntax(openParen, scopeName, closeParen);
        }
        else if (settings.AllowScopes.Count > 0)
        {
            var openParen = Expect(SyntaxKind.LeftParenToken, DiagnosticDescriptor.MissingOpenParen, reportDiagnostic: false);
            var scopeName = Expect(SyntaxKind.IdentifierToken, DiagnosticDescriptor.MissingScope, reportDiagnostic: false);
            var closeParen = Expect(SyntaxKind.RightParenToken, DiagnosticDescriptor.MissingCloseParen, reportDiagnostic: false);
            scope = new ScopeSyntax(openParen, scopeName, closeParen);
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.MissingScope, scope.Span));
        }

        // ! - 可选
        SyntaxToken? bangToken = null;
        if (IsAt(SyntaxKind.BangToken))
        {
            bangToken = Advance();
        }

        // ':' — 必需
        var colonToken = Expect(SyntaxKind.ColonToken, DiagnosticDescriptor.MissingColon);
        if (!colonToken.IsMissing && colonToken.LeadingTrivia.Length > 0)
        {
            var span = TextSpan.FromBounds(colonToken.LeadingTrivia.First().Span.Start, colonToken.LeadingTrivia.Last().Span.End);
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.UnexpectedExtraSpaces, span));
        }

        // subject — 必需，冒号+空格之后到行尾的所有文本
        var subjectToken = ParseTextToken();
        if (subjectToken.IsMissing)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.MissingTextValue, subjectToken.Span, Assets.Languages.DiagnosticValue_Subject));
        }
        else if (subjectToken.LeadingTrivia.Length == 0)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.MissingSpace, colonToken.Span));
        }
        else if (subjectToken.LeadingTrivia.Length == 1)
        {
            var spaceTrivia = subjectToken.LeadingTrivia[0];
            if (spaceTrivia.Kind != SyntaxKind.WhitespaceTrivia)
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.MissingSpace, spaceTrivia.Span));
            }
            else if (spaceTrivia.Span.Length != 1)
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.UnexpectedExtraSpaces, spaceTrivia.Span));
            }
        }
        else
        {
            var span = TextSpan.FromBounds(subjectToken.LeadingTrivia.First().Span.Start, subjectToken.LeadingTrivia.Last().Span.End);
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.UnexpectedExtraSpaces, span));
        }

        return new HeaderSyntax(typeToken, scope, bangToken, colonToken, subjectToken);
    }

    /// <summary>解析整行文本：从当前 Token 到行尾或 EOF 的所有内容。</summary>
    private SyntaxToken ParseTextToken()
    {
        var subjectParts = new StringBuilder();

        List<SyntaxTrivia> leadingTrivia = [];
        while (IsAt(SyntaxKind.EndOfLineToken))
        {
            var trivia = Advance();
            leadingTrivia.Add(new SyntaxTrivia(SyntaxKind.EndOfLineTrivia, trivia.Span, trivia.Text));
        }
        var startPos = Current.FullSpan.Start;
        while (!IsAt(SyntaxKind.EndOfLineToken) && !IsAt(SyntaxKind.EndOfFileToken))
        {
            var token = Advance();
            foreach (var trivia in token.LeadingTrivia)
                subjectParts.Append(trivia.Text);
            subjectParts.Append(token.Text);
            foreach (var trivia in token.TrailingTrivia)
                subjectParts.Append(trivia.Text);
        }

        var fullSubject = subjectParts.ToString();
        var subject = fullSubject.Trim(' ');
        int start = fullSubject.IndexOf(subject);
        if (start > 0)
        {
            var triviaSpan = new TextSpan(startPos, start);
            leadingTrivia.Add(new SyntaxTrivia(SyntaxKind.WhitespaceTrivia, triviaSpan, fullSubject.Substring(0, start)));
        }
        var subjectSpan = new TextSpan(startPos + start, subject.Length);
        bool isMissing = string.IsNullOrWhiteSpace(subject);
        return new SyntaxToken(SyntaxKind.TextToken, subjectSpan, subject, leadingTrivia.ToImmutableArray(), null, isMissing);

    }

    /// <summary>解析 body 段落（调用方已消耗分隔换行，当前指向 body 第一行首个 Token）。</summary>
    private BodySyntax? ParseBody()
    {
        var textLines = new List<SyntaxToken>();

        while (!IsFooter() && !IsAt(SyntaxKind.EndOfFileToken))
        {
            textLines.Add(ParseTextToken());
        }
        if (textLines.Count == 0)
        {
            return null;
        }
        int blankLineCount = 0;
        for (int index = textLines.Count - 1; index >= 0; index--)
        {
            var textLine = textLines[index];
            if (!textLine.IsMissing)
            { break; }
            blankLineCount += 1;
        }
        tokenIndex -= blankLineCount;
        textLines = [.. textLines.Take(textLines.Count - blankLineCount)];
        if (textLines.Count == 0)
        {
            return null;
        }
        var firstLine = textLines[0];
        if (firstLine.LeadingTrivia.Length == 0)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.MissingNewline, firstLine.Span));
        }
        else if (firstLine.LeadingTrivia.Length == 1)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.MissingNewline, firstLine.LeadingTrivia[0].Span));
        }
        else if (firstLine.LeadingTrivia.Length > 2)
        {
            var span = TextSpan.FromBounds(firstLine.LeadingTrivia.First().Span.Start, firstLine.LeadingTrivia.Last().Span.End);
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.UnexpectedExtraLines, span));
        }

        return new BodySyntax(textLines);
    }

    /// <summary>
    /// &lt;footer-line&gt; ::= "BREAKING CHANGE:" &lt;text&gt; | &lt;key&gt; ":" &lt;value&gt;
    /// </summary>
    private List<FooterSyntax> ParseFooters()
    {
        var footers = new List<FooterSyntax>();
        while (IsFooter() || IsAt(SyntaxKind.EndOfLineToken))
        {
            var savedIndex = tokenIndex;
            List<SyntaxTrivia> leadingTrivia = [];
            while (IsAt(SyntaxKind.EndOfLineToken))
            {
                var token = Advance();
                leadingTrivia.Add(new SyntaxTrivia(SyntaxKind.EndOfLineTrivia, token.Span, token.Text));
            }
            var footKeyParts = new List<string>();
            var startPos = Current.FullSpan.Start;
            while (!IsAt(SyntaxKind.ColonToken) && !IsAt(SyntaxKind.EndOfLineToken) && !IsAt(SyntaxKind.EndOfFileToken))
            {
                var token = Advance();
                foreach (var trivia in token.LeadingTrivia)
                    footKeyParts.Add(trivia.Text);
                footKeyParts.Add(token.Text);
                foreach (var trivia in token.TrailingTrivia)
                    footKeyParts.Add(trivia.Text);
            }
            var fullFootKey = string.Concat(footKeyParts);
            if (string.IsNullOrEmpty(fullFootKey))
            {
                tokenIndex = savedIndex;
                break;
            }
            var footKeySpan = new TextSpan(startPos, fullFootKey.Length);
            var keyToken = new SyntaxToken(SyntaxKind.TextToken, footKeySpan, fullFootKey, leadingTrivia.ToImmutableArray());
            if (settings.AllowFooters.Count > 0 && !settings.AllowFooters.Contains(keyToken.Text))
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.InvalidFooterKey, keyToken.Span));
            }
            var colonToken = Expect(SyntaxKind.ColonToken, DiagnosticDescriptor.MissingColon);
            var valueToken = ParseTextToken();
            if (valueToken.IsMissing)
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.MissingTextValue, valueToken.Span, Assets.Languages.DiagnosticValue_Footer));
            }
            else if (valueToken.LeadingTrivia.Length == 0)
            {
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.MissingSpace, valueToken.Span));
            }
            else if (valueToken.LeadingTrivia.Length == 1)
            {
                var spaceTrivia = valueToken.LeadingTrivia[0];
                if (spaceTrivia.Kind != SyntaxKind.WhitespaceTrivia)
                {
                    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.MissingSpace, spaceTrivia.Span));
                }
                else if (spaceTrivia.Span.Length != 1)
                {
                    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.UnexpectedExtraSpaces, spaceTrivia.Span));
                }
            }
            else
            {
                var span = TextSpan.FromBounds(valueToken.LeadingTrivia.First().Span.Start, valueToken.LeadingTrivia.Last().Span.End);
                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.UnexpectedExtraSpaces, span));
            }
            footers.Add(new FooterSyntax(keyToken, colonToken, valueToken));
        }
        for (int index = 0; index < footers.Count; index++)
        {
            var leadingTrivias = footers[index].KeyToken.LeadingTrivia;
            if (index == 0)
            {
                if (leadingTrivias.Length == 0)
                {
                    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.MissingNewline, footers[1].KeyToken.Span));
                }
                else if (leadingTrivias.Length == 1)
                {
                    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.MissingNewline, leadingTrivias[0].Span));
                }
                else if (leadingTrivias.Length > 2)
                {
                    var span = TextSpan.FromBounds(leadingTrivias[2].Span.Start, leadingTrivias.Last().Span.End);
                    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.UnexpectedExtraLines, span));
                }
            }
            else
            {
                if (leadingTrivias.Length > 1)
                {
                    var span = TextSpan.FromBounds(leadingTrivias.First().Span.Start, leadingTrivias.Last().Span.End);
                    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptor.UnexpectedExtraLines, span));
                }
            }
        }
        return footers;
    }

}
