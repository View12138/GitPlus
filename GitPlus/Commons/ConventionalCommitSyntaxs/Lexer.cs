namespace GitPlus.Commons.ConventionalCommitSyntaxs;

/// <summary>词法分析器。</summary>
internal sealed class Lexer
{
    private Lexer(string? text) => this.text = text ?? string.Empty;
    private readonly string text;
    private int position = 0;
    private readonly List<SyntaxToken> tokens = [];
    private readonly List<SyntaxTrivia> leadingTrivia = [];
    private readonly char[] specialChars = ['\r', '\n', ' ', '\t', '(', ')', '!', ':'];

    private List<SyntaxToken> Lex()
    {
        tokens.Clear();
        leadingTrivia.Clear();
        position = 0;

        while (position < text.Length)
        {
            var ch = text[position];

            switch (ch)
            {
                case '\r':
                case '\n':
                    {
                        var start = position;
                        if (position < text.Length - 1 && text[position] == '\r' && text[position + 1] == '\n')
                        { position += 2; }
                        else
                        { position += 1; }
                        var span = new TextSpan(start, position - start);
                        var tokenText = text.Substring(start, span.Length);
                        EmitTokenWithSpan(SyntaxKind.EndOfLineToken, span, tokenText);
                    }
                    break;

                case ' ':
                case '\t':
                    {
                        var start = position;
                        while (position < text.Length && (text[position] == ' ' || text[position] == '\t'))
                        { position++; }
                        var span = new TextSpan(start, position - start);
                        var triviaText = text.Substring(start, span.Length);
                        leadingTrivia.Add(new SyntaxTrivia(SyntaxKind.WhitespaceTrivia, span, triviaText));
                    }
                    break;

                case '(':
                    EmitToken(SyntaxKind.LeftParenToken, 1);
                    break;

                case ')':
                    EmitToken(SyntaxKind.RightParenToken, 1);
                    break;

                case '!':
                    EmitToken(SyntaxKind.BangToken, 1);
                    break;

                case ':':
                    EmitToken(SyntaxKind.ColonToken, 1);
                    break;

                default:
                    {
                        var start = position;
                        while (position < text.Length)
                        {
                            if (specialChars.Contains(text[position]))
                            { break; }
                            position++;
                        }

                        var span = new TextSpan(start, position - start);
                        var tokenText = text.Substring(start, span.Length);
                        EmitTokenWithSpan(SyntaxKind.IdentifierToken, span, tokenText);
                    }
                    break;
            }
        }

        var eofSpan = new TextSpan(text.Length, 0);
        EmitTokenWithSpan(SyntaxKind.EndOfFileToken, eofSpan, string.Empty);
        return tokens;
    }

    public static List<SyntaxToken> Lex(string text) => new Lexer(text).Lex();

    private void EmitToken(SyntaxKind kind, int length)
    {
        var span = new TextSpan(position, length);
        var tokenText = text.Substring(position, length);
        EmitTokenWithSpan(kind, span, tokenText);
        position += length;
    }

    private void EmitTokenWithSpan(SyntaxKind kind, TextSpan span, string text)
    {
        var token = new SyntaxToken(kind, span, text, leadingTrivia.ToImmutableArray(), null);
        leadingTrivia.Clear();
        tokens.Add(token);
    }
}
