using System.Diagnostics;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace GitPlus.Injectors;

[RequiredArgsConstructor]
public sealed partial class GitWindowActionButtonPanelInjector : InjectorBase
{
    private readonly ILogger logger;
    private readonly GitCommandService git;

    public override bool CanInject(string caption)
        => caption != null && caption.IndexOf("git", StringComparison.OrdinalIgnoreCase) >= 0;

    public override async Task InjectAsync(string caption, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace("[GitWindowActionButtonPanelInjector] enter '{method}', caption=\"{Caption}\"", nameof(InjectAsync), caption);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var buttonPanel = await GitWindowLocator.LocateGitButtonPanelAsync(cancellationToken) as Panel;
        if (buttonPanel is null)
        {
            logger.LogDebug("[GitWindowActionButtonPanelInjector] button panel not found — aborting injection.");
            return;
        }

        var fetchButtonElement = await buttonPanel.GetChildIndexAsync("fetchButton", cancellationToken);
        if (fetchButtonElement.HasValue && fetchButtonElement.Value.Element is ButtonBase fetchButton)
        {
            var options = Extensions.GetRequiredService<GitPlusOption>();
            var toolTip = Properties.Languages.AutoFetchToolTipSuffix;
            var oldToolTip = fetchButton.ToolTip.ToString();
            if (oldToolTip.Contains(toolTip))
            {
                var index = oldToolTip.IndexOf(toolTip, StringComparison.Ordinal);
                oldToolTip = oldToolTip.Substring(0, index);
            }
            fetchButton.ToolTip = $"{oldToolTip}{(options.AutoFetchEnabled ? toolTip : string.Empty)}";
            logger.LogTrace("[GitWindowActionButtonPanelInjector] fetch button tooltip updated.");
        }

        var pullButtonElement = await buttonPanel.GetChildIndexAsync("pullButton", cancellationToken);
        if (pullButtonElement.HasValue && pullButtonElement.Value.Element is ButtonBase pullButton)
        {
            var pullWithStash = new ImageButton();
            pullWithStash.CopyLocalValuesFrom(pullButton);
            pullWithStash.Name = "pullWithStashButton";
            pullWithStash.ImageNormal = Application.Current.FindResource("PullWithStashIconNormal") as ImageSource;
            pullWithStash.ImageHover = Application.Current.FindResource("PullWithStashIconHover") as ImageSource;
            pullWithStash.ImagePressed = Application.Current.FindResource("PullWithStashIconPressed") as ImageSource;
            pullWithStash.Style = Application.Current.FindResource("GitButtonStyle") as Style;
            pullWithStash.SetBinding(UIElement.VisibilityProperty, BindingOperations.GetBinding(pullButton, UIElement.VisibilityProperty));
            pullWithStash.Command = new AsyncRelayCommand(async () =>
            {
                if (!pullButton.Command.CanExecute(null))
                {
                    logger.LogDebug("[GitWindowActionButtonPanelInjector] PullWithStash: pull command not executable — skipping.");
                    return;
                }

                var guid = Guid.NewGuid();
                var options = Extensions.GetRequiredService<GitPlusOption>();
                bool needStashPop = false;

                pullButton.DataContext.ShowNotification(Properties.Languages.FetchingStatus, guid: guid);
                var result = await git.StatusAsync();
                if (!result.IsSuccess)
                {
                    logger.LogWarning("PullWithStash: status check failed: {Error}", result.Error);
                    pullButton.DataContext.ShowError(string.Format(Properties.Languages.FetchStatusFailed, result.Error));
                    return;
                }

                if (result.Output.Contains("Changes to be committed:") || result.Output.Contains("Changes not staged for commit:"))
                {
                    pullButton.DataContext.ShowNotification(Properties.Languages.StashingChanges, guid: guid);
                    result = await git.StashPushAsync();
                    if (!result.IsSuccess)
                    {
                        logger.LogWarning("PullWithStash: stash push failed: {Error}", result.Error);
                        pullButton.DataContext.ShowError(string.Format(Properties.Languages.StashFailed, result.Error));
                        return;
                    }
                    needStashPop = true;
                }

                pullButton.DataContext.ShowNotification(Properties.Languages.Pulling, guid: guid);
                var pullResult = await git.PullAsync(options.UseRebase);
                if (!pullResult.IsSuccess)
                {
                    logger.LogWarning("PullWithStash: pull failed: {Error}", pullResult.Error);
                    pullButton.DataContext.ShowError(string.Format(Properties.Languages.PullFailed, pullResult.Error));
                }

                if (needStashPop)
                {
                    pullButton.DataContext.ShowNotification(Properties.Languages.RestoringStash, guid: guid);
                    result = await git.StashPopAsync();
                    if (!result.IsSuccess)
                    {
                        logger.LogWarning("PullWithStash: stash pop failed: {Error}", result.Error);
                        pullButton.DataContext.ShowError(string.Format(Properties.Languages.StashPopFailed, result.Error));
                        return;
                    }
                }

                pullButton.DataContext.ShowNotification(pullResult.Output, guid: guid);

                await Task.Delay(5000);
                pullButton.DataContext.HideNotification(guid);
            });
            pullWithStash.ToolTip = Properties.Languages.PullWithStashToolTip;
            await buttonPanel.InsertElementAsync(pullWithStash, pullButtonElement.Value.Index + 1, cancellationToken);
            logger.LogTrace("[GitWindowActionButtonPanelInjector] pull-with-stash button created.");
        }
        stopwatch.Stop();
        logger.LogTrace("[GitWindowActionButtonPanelInjector] exit '{method}', elapsed={elapsed}ms", nameof(InjectAsync), stopwatch.ElapsedMilliseconds);
    }
}
