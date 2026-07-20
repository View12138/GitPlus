namespace GitPlus.Commons.ConventionalCommitSyntaxs;

public sealed class ParserSettings
{
    public IReadOnlyList<string> AllowTypes { get; init; } = [];
    public IReadOnlyList<string> AllowScopes { get; init; } = [];
    public IReadOnlyList<string> AllowFooters { get; init; } = [];
}