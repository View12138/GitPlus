namespace GitPlus.Commons.ConventionalCommitSyntaxs;

public enum DiagnosticSeverity { Info, Warning, Error, }

public class DiagnosticDescriptor
{
    private DiagnosticDescriptor(string id, DiagnosticSeverity severity, string message)
    {
        Id = id;
        Severity = severity;
        MessageTemplate = message;
    }
    public string Id { get; }
    public DiagnosticSeverity Severity { get; }
    public string MessageTemplate { get; }
    public override string ToString() => $"[{Severity}] {Id}: {MessageTemplate}";

    public static readonly DiagnosticDescriptor MissingCommitType = new("CC001", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_MissingCommitType);
    public static readonly DiagnosticDescriptor InvalidCommitType = new("CC002", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_InvalidCommitType);
    public static readonly DiagnosticDescriptor MissingSpace = new("CC003", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_MissingSpace);
    public static readonly DiagnosticDescriptor UnexpectedExtraSpaces = new("CC004", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_UnexpectedExtraSpaces);
    public static readonly DiagnosticDescriptor MissingScope = new("CC005", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_MissingScope);
    public static readonly DiagnosticDescriptor InvalidScope = new("CC006", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_InvalidScope);
    public static readonly DiagnosticDescriptor MissingOpenParen = new("CC007", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_MissingOpenParen);
    public static readonly DiagnosticDescriptor MissingCloseParen = new("CC008", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_MissingCloseParen);
    public static readonly DiagnosticDescriptor MissingColon = new("CC009", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_MissingColon);
    public static readonly DiagnosticDescriptor MissingTextValue = new("CC010", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_MissingTextValue);
    public static readonly DiagnosticDescriptor MissingNewline = new("CC011", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_MissingNewline);
    public static readonly DiagnosticDescriptor UnexpectedExtraLines = new("CC012", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_UnexpectedExtraLines);
    public static readonly DiagnosticDescriptor InvalidFooterKey = new("CC013", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_InvalidFooterKey);
    public static readonly DiagnosticDescriptor UnexpectedExtraWhitespace = new("CC014", DiagnosticSeverity.Error, Assets.Languages.Diagnostic_UnexpectedExtraWhitespace);
}

public sealed class Diagnostic
{
    private Diagnostic(DiagnosticDescriptor descriptor, TextSpan span, params object[] args)
    {
        Id = descriptor.Id;
        Severity = descriptor.Severity;
        Message = args.Length == 0 ? descriptor.MessageTemplate : string.Format(descriptor.MessageTemplate, args);
        Span = span;
    }

    public string Id { get; }
    public DiagnosticSeverity Severity { get; }
    public string Message { get; }
    public TextSpan Span { get; }

    public override string ToString() => $"[{Severity}] {Id}: {Message} at {Span}";

    public static Diagnostic Create(DiagnosticDescriptor descriptor, TextSpan span, params object[] args) => new(descriptor, span, args);
}
