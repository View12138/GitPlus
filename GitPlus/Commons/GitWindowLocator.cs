using System.Diagnostics;
using Microsoft.VisualStudio.Shell;

namespace GitPlus.Commons;

public static class GitWindowLocator
{
    private static ILogger Logger => Extensions.GetRequiredService<ILogger>();

    public static async Task<FrameworkElement?> LocateGitButtonPanelAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.LogTrace("[GitWindowLocator] enter '{method}'", nameof(LocateGitButtonPanelAsync));
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var gitWindowMainGrid = await Application.Current.MainWindow.FindChildAsync("gitWindowMainGrid", cancellationToken: cancellationToken);
        if (gitWindowMainGrid is null)
        {
            Logger.LogDebug("[GitWindowLocator] 'gitWindowMainGrid' not found.");
            return null;
        }

        var buttonPanel = await gitWindowMainGrid.FindChildAsync("buttonPanel", cancellationToken: cancellationToken);
        if (buttonPanel is null)
            Logger.LogDebug("[GitWindowLocator] 'buttonPanel' not found inside 'gitWindowMainGrid'.");

        stopwatch.Stop();
        Logger.LogTrace("[GitWindowLocator] exit '{method}', elapsed={elapsed}ms", nameof(LocateGitButtonPanelAsync), stopwatch.ElapsedMilliseconds);
        return buttonPanel;
    }

    public static async Task<FrameworkElement?> LocateGitCommentTextBoxAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.LogTrace("[GitWindowLocator] enter '{method}'", nameof(LocateGitCommentTextBoxAsync));
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var gitWindowMainGrid = await Application.Current.MainWindow.FindChildAsync("gitWindowMainGrid", cancellationToken: cancellationToken);
        if (gitWindowMainGrid is null)
        {
            Logger.LogDebug("[GitWindowLocator] 'gitWindowMainGrid' not found.");
            return null;
        }

        var commentTextBox = await gitWindowMainGrid.FindChildAsync("commentTextBox", cancellationToken: cancellationToken);
        if (commentTextBox is null)
            Logger.LogDebug("[GitWindowLocator] 'commentTextBox' not found inside 'gitWindowMainGrid'.");

        stopwatch.Stop();
        Logger.LogTrace("[GitWindowLocator] exit '{method}', elapsed={elapsed}ms", nameof(LocateGitCommentTextBoxAsync), stopwatch.ElapsedMilliseconds);
        return commentTextBox;
    }
}
