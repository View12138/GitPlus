namespace GitPlus.Configurations;

public class ConventionalCommits
{
    public List<ConventionalCommitRule> Rules { get; set; } = [];

}

public class ConventionalCommitRule
{
    public string Type { get; set; }
    public string Emoji { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
}

public class ConventionalCommitOption
{
    public bool EnableStrictValidation { get; set; }

    public List<string> Scopes { get; set; } = [];
    public List<ConventionalCommitRule> CustomRules { get; set; } = [];
    public List<string> CustomFooters { get; set; } = [];
}