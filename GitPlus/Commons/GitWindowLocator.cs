using System.Diagnostics;
using Microsoft.VisualStudio.Shell;

namespace GitPlus.Commons;

public static class GitWindowLocator
{
    private static ILogger Logger => Extensions.GetRequiredService<ILogger>();

    public static async Task<FrameworkElement?> LocateChildElementAsync(string elementName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.LogTrace("[GitWindowLocator] enter '{method}'", nameof(LocateChildElementAsync));
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var gitWindowMainGrid = await Application.Current.MainWindow.FindChildAsync("gitWindowMainGrid", cancellationToken: cancellationToken);
        if (gitWindowMainGrid is null)
        {
            Logger.LogDebug("[GitWindowLocator] 'gitWindowMainGrid' not found.");
            return null;
        }

        var element = await gitWindowMainGrid.FindChildAsync(elementName, cancellationToken: cancellationToken);
        if (element is null)
            Logger.LogDebug("[GitWindowLocator] '{elementName}' not found inside 'gitWindowMainGrid'.", elementName);

        stopwatch.Stop();
        Logger.LogTrace("[GitWindowLocator] exit '{method}', elapsed={elapsed}ms", nameof(LocateChildElementAsync), stopwatch.ElapsedMilliseconds);
        return element;
    }

    public static async Task<StackPanel?> LocateGitWorkItemActionsStackPanelAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Logger.LogTrace("[GitWindowLocator] enter '{method}'", nameof(LocateGitWorkItemActionsStackPanelAsync));
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var gitWindowMainGrid = await Application.Current.MainWindow.FindChildAsync("gitWindowMainGrid", cancellationToken: cancellationToken);
        if (gitWindowMainGrid is null)
        {
            Logger.LogDebug("[GitWindowLocator] 'gitWindowMainGrid' not found.");
            return null;
        }

        var workItemActions = await gitWindowMainGrid.FindChildAsync("WorkItemActions", cancellationToken: cancellationToken);
        if (workItemActions is null)
        {
            stopwatch.Stop();
            Logger.LogTrace("[GitWindowLocator] exit '{method}', elapsed={elapsed}ms", nameof(LocateGitWorkItemActionsStackPanelAsync), stopwatch.ElapsedMilliseconds);
            return null;
        }
        var stackPanel = await workItemActions.FindChildAsync<StackPanel>(cancellationToken: cancellationToken);
        if (stackPanel is null)
            Logger.LogDebug("[GitWindowLocator] 'StackPanel' not found inside 'WorkItemActions'.");
        stopwatch.Stop();
        Logger.LogTrace("[GitWindowLocator] exit '{method}', elapsed={elapsed}ms", nameof(LocateGitWorkItemActionsStackPanelAsync), stopwatch.ElapsedMilliseconds);
        return stackPanel;
    }
}
