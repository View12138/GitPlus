using System.Drawing.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace GitPlus.Configurations;

/// <summary>Options page visible in Tools → Options → Git +. Wraps <see cref="GitPlusOption"/>.</summary>
[ComVisible(true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class GitPlusOptionPage : DialogPage
{
    #region General
    [LocalizedCategory(nameof(Assets.Languages.OptionCategory_General))]
    [LocalizedDisplayName(nameof(Assets.Languages.Option_TimeoutSeconds_DisplayName))]
    [LocalizedDescription(nameof(Assets.Languages.Option_TimeoutSeconds_Description))]
    public int TimeoutSeconds { get; set; } = 30;

    [LocalizedCategory(nameof(Assets.Languages.OptionCategory_General))]
    [LocalizedDisplayName(nameof(Assets.Languages.Option_GitFileName_DisplayName))]
    [LocalizedDescription(nameof(Assets.Languages.Option_GitFileName_Description))]
    [Editor(typeof(FileBrowserEditor), typeof(UITypeEditor))]
    public string GitFileName { get; set; } = string.Empty;
    #endregion

    #region Auto_Fetch
    [LocalizedCategory(nameof(Assets.Languages.OptionCategory_Auto_Fetch))]
    [LocalizedDisplayName(nameof(Assets.Languages.Option_UseAutoFetch_DisplayName))]
    [LocalizedDescription(nameof(Assets.Languages.Option_UseAutoFetch_Description))]
    public bool UseAutoFetch { get; set; } = true;

    [LocalizedCategory(nameof(Assets.Languages.OptionCategory_Auto_Fetch))]
    [LocalizedDisplayName(nameof(Assets.Languages.Option_AutoFetchIntervalMinutes_DisplayName))]
    [LocalizedDescription(nameof(Assets.Languages.Option_AutoFetchIntervalMinutes_Description))]
    public int AutoFetchIntervalMinutes { get; set; } = 5;
    #endregion

    #region Pull
    [LocalizedCategory(nameof(Assets.Languages.OptionCategory_Pull))]
    [LocalizedDisplayName(nameof(Assets.Languages.Option_UseRebase_DisplayName))]
    [LocalizedDescription(nameof(Assets.Languages.Option_UseRebase_Description))]
    public bool UseRebase { get; set; } = true;

    [LocalizedCategory(nameof(Assets.Languages.OptionCategory_Pull))]
    [LocalizedDisplayName(nameof(Assets.Languages.Option_AutoPullVisible_DisplayName))]
    [LocalizedDescription(nameof(Assets.Languages.Option_AutoPullVisible_Description))]
    public bool AutoPullVisible { get; set; } = true;
    #endregion

    #region Commit
    [LocalizedCategory(nameof(Assets.Languages.OptionCategory_Commit))]
    [LocalizedDisplayName(nameof(Assets.Languages.Option_ConventionalCommitsVisible_DisplayName))]
    [LocalizedDescription(nameof(Assets.Languages.Option_ConventionalCommitsVisible_Description))]
    public bool ConventionalCommitsVisible { get; set; } = true;

    [LocalizedCategory(nameof(Assets.Languages.OptionCategory_Commit))]
    [LocalizedDisplayName(nameof(Assets.Languages.Option_ConventionalCommitOptionFileName_DisplayName))]
    [LocalizedDescription(nameof(Assets.Languages.Option_ConventionalCommitOptionFileName_Description))]
    public string ConventionalCommitOptionFileName { get; set; } = "ConventionalCommitOption.json";
    #endregion

    #region Logging
    [LocalizedCategory(nameof(Assets.Languages.OptionCategory_Logging))]
    [LocalizedDisplayName(nameof(Assets.Languages.Option_LogLevel_DisplayName))]
    [LocalizedDescription(nameof(Assets.Languages.Option_LogLevel_Description))]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    #endregion

    /// <summary>Copy current values into a POCO for consumption by services.</summary>
    public GitPlusOption ToOption() => new(
        new(TimeoutSeconds,GitFileName),
        new(UseAutoFetch, AutoFetchIntervalMinutes),
        new(UseRebase, AutoPullVisible),
        new(ConventionalCommitsVisible, ConventionalCommitOptionFileName),
        new(LogLevel));
}


/// <summary>Lightweight POCO snapshot of options — DI-friendly, no WPF dependency.</summary>
/// <param name="TimeoutSeconds"></param>
/// <param name="UseAutoFetch"></param>
/// <param name="AutoFetchIntervalMinutes"></param>
/// <param name="UseRebase"></param>
/// <param name="LogLevel"></param>
public sealed record GitPlusOption(
    GitPlusGeneralOption General,
    GitPlusAutoFetchOption AutoFetch,
    GitPlusPullOption Pull,
    GitPlusCommitOption Commit,
    GitPlusLoggingOption Logging);

public sealed record GitPlusGeneralOption(int TimeoutSeconds, string GitFileName);
public sealed record GitPlusAutoFetchOption(bool UseAutoFetch, int AutoFetchIntervalMinutes);
public sealed record GitPlusPullOption(bool UseRebase, bool AutoPullVisible);
public sealed record GitPlusCommitOption(bool ConventionalCommitsVisible, string ConventionalCommitOptionFileName);
public sealed record GitPlusLoggingOption(LogLevel LogLevel);