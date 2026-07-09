using System.Drawing.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;

namespace GitPlus.Configurations;

/// <summary>Options page visible in Tools → Options → Git +. Wraps <see cref="GitPlusOption"/>.</summary>
[ComVisible(true)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class GitPlusOptionPage : DialogPage
{
    [LocalizedCategory(nameof(Properties.Languages.OptionCategory_General))]
    [LocalizedDisplayName(nameof(Properties.Languages.Option_TimeoutSeconds_DisplayName))]
    [LocalizedDescription(nameof(Properties.Languages.Option_TimeoutSeconds_Description))]
    public int TimeoutSeconds { get; set; } = 30;

    [LocalizedCategory(nameof(Properties.Languages.OptionCategory_General))]
    [LocalizedDisplayName(nameof(Properties.Languages.Option_GitFilePath_DisplayName))]
    [LocalizedDescription(nameof(Properties.Languages.Option_GitFilePath_Description))]
    [Editor(typeof(FileBrowserEditor), typeof(UITypeEditor))]
    public string GitFilePath { get; set; } = string.Empty;

    [LocalizedCategory(nameof(Properties.Languages.OptionCategory_Auto_Fetch))]
    [LocalizedDisplayName(nameof(Properties.Languages.Option_AutoFetchEnabled_DisplayName))]
    [LocalizedDescription(nameof(Properties.Languages.Option_AutoFetchEnabled_Description))]
    public bool AutoFetchEnabled { get; set; } = true;

    [LocalizedCategory(nameof(Properties.Languages.OptionCategory_Auto_Fetch))]
    [LocalizedDisplayName(nameof(Properties.Languages.Option_AutoFetchIntervalMinutes_DisplayName))]
    [LocalizedDescription(nameof(Properties.Languages.Option_AutoFetchIntervalMinutes_Description))]
    public int AutoFetchIntervalMinutes { get; set; } = 5;

    [LocalizedCategory(nameof(Properties.Languages.OptionCategory_Pull))]
    [LocalizedDisplayName(nameof(Properties.Languages.Option_UseRebase_DisplayName))]
    [LocalizedDescription(nameof(Properties.Languages.Option_UseRebase_Description))]
    public bool UseRebase { get; set; } = true;

    [LocalizedCategory(nameof(Properties.Languages.OptionCategory_Logging))]
    [LocalizedDisplayName(nameof(Properties.Languages.Option_LogLevel_DisplayName))]
    [LocalizedDescription(nameof(Properties.Languages.Option_LogLevel_Description))]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>Copy current values into a POCO for consumption by services.</summary>
    public GitPlusOption ToOption() => new(TimeoutSeconds,
        GitFilePath,
        AutoFetchEnabled,
        AutoFetchIntervalMinutes,
        UseRebase,
        LogLevel);
}


/// <summary>Lightweight POCO snapshot of options — DI-friendly, no WPF dependency.</summary>
/// <param name="TimeoutSeconds"></param>
/// <param name="AutoFetchEnabled"></param>
/// <param name="AutoFetchIntervalMinutes"></param>
/// <param name="UseRebase"></param>
/// <param name="LogLevel"></param>
public sealed record GitPlusOption(int TimeoutSeconds,
    string GitFilePath,
    bool AutoFetchEnabled,
    int AutoFetchIntervalMinutes,
    bool UseRebase,
    LogLevel LogLevel);