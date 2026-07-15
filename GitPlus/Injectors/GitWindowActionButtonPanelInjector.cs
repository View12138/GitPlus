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
            var toolTip = Assets.Languages.AutoFetchToolTipSuffix;
            var oldToolTip = fetchButton.ToolTip.ToString();
            if (oldToolTip.Contains(toolTip))
            {
                var index = oldToolTip.IndexOf(toolTip, StringComparison.Ordinal);
                oldToolTip = oldToolTip.Substring(0, index);
            }
            fetchButton.ToolTip = $"{oldToolTip}{(options.AutoFetch.UseAutoFetch ? toolTip : string.Empty)}";
            logger.LogTrace("[GitWindowActionButtonPanelInjector] fetch button tooltip updated.");
        }

        var pullWithStashButtonElement = await buttonPanel.GetChildIndexAsync("pullWithStashButton", cancellationToken);
        if (pullWithStashButtonElement is null)
        {
            var pullButtonElement = await buttonPanel.GetChildIndexAsync("pullButton", cancellationToken);
            if (pullButtonElement is not null && pullButtonElement.Value.Element is ButtonBase pullButton)
            {
                var options = Extensions.GetRequiredService<GitPlusOption>();
                var pullWithStash = new ImageButton();
                pullWithStash.CopyLocalValuesFrom(pullButton);
                pullWithStash.Name = "pullWithStashButton";
                pullWithStash.ImageNormal = Application.Current.FindResource("PullWithStashIconNormal") as ImageSource;
                pullWithStash.ImageHover = Application.Current.FindResource("PullWithStashIconHover") as ImageSource;
                pullWithStash.ImagePressed = Application.Current.FindResource("PullWithStashIconPressed") as ImageSource;
                pullWithStash.Style = Application.Current.FindResource("GitStatusButtonStyle") as Style;
                pullWithStash.Visibility = options.Pull.AutoPullVisible ? Visibility.Visible : Visibility.Collapsed;
                {
                    var actionButtonElement = await buttonPanel.GetChildIndexAsync("actionButton", cancellationToken);
                    if (actionButtonElement is not null && actionButtonElement.Value.Element is ButtonBase actionButton)
                    {
                        DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(UIElement))
                            .AddValueChanged(actionButton, (s, e) =>
                            {
                                var _options = Extensions.GetRequiredService<GitPlusOption>();
                                if (actionButton.Visibility == Visibility.Visible)
                                {
                                    pullWithStash.Visibility = Visibility.Collapsed;
                                }
                                else
                                {
                                    pullWithStash.Visibility = _options.Pull.AutoPullVisible ? Visibility.Visible : Visibility.Collapsed;
                                }
                            });
                    }
                }
                pullWithStash.ToolTip = Assets.Languages.PullWithStashToolTip;
                pullWithStash.Command = new AsyncRelayCommand(async () =>
                {
                    if (!pullButton.Command.CanExecute(null))
                    {
                        logger.LogDebug("[GitWindowActionButtonPanelInjector] PullWithStash: pull command not executable — skipping.");
                        return;
                    }
                    var guid = Guid.NewGuid();
                    var _options = Extensions.GetRequiredService<GitPlusOption>();
                    bool needStashPop = false;

                    pullButton.DataContext.ShowNotification(Assets.Languages.FetchingStatus, guid: guid);
                    var result = await git.StatusAsync();
                    if (!result.IsSuccess)
                    {
                        logger.LogWarning("PullWithStash: status check failed: {Error}", result.Error);
                        pullButton.DataContext.ShowError(string.Format(Assets.Languages.FetchStatusFailed, result.Error));
                        return;
                    }

                    if (result.Output.Contains("Changes to be committed:") || result.Output.Contains("Changes not staged for commit:"))
                    {
                        pullButton.DataContext.ShowNotification(Assets.Languages.StashingChanges, guid: guid);
                        result = await git.StashPushAsync();
                        if (!result.IsSuccess)
                        {
                            logger.LogWarning("PullWithStash: stash push failed: {Error}", result.Error);
                            pullButton.DataContext.ShowError(string.Format(Assets.Languages.StashFailed, result.Error));
                            return;
                        }
                        needStashPop = true;
                    }

                    pullButton.DataContext.ShowNotification(Assets.Languages.Pulling, guid: guid);
                    var pullResult = await git.PullAsync(_options.Pull.UseRebase);
                    if (!pullResult.IsSuccess)
                    {
                        logger.LogWarning("PullWithStash: pull failed: {Error}", pullResult.Error);
                        pullButton.DataContext.ShowError(string.Format(Assets.Languages.PullFailed, pullResult.Error));
                    }

                    if (needStashPop)
                    {
                        pullButton.DataContext.ShowNotification(Assets.Languages.RestoringStash, guid: guid);
                        result = await git.StashPopAsync();
                        if (!result.IsSuccess)
                        {
                            logger.LogWarning("PullWithStash: stash pop failed: {Error}", result.Error);
                            pullButton.DataContext.ShowError(string.Format(Assets.Languages.StashPopFailed, result.Error));
                            return;
                        }
                    }

                    pullButton.DataContext.ShowNotification(pullResult.Output, guid: guid);

                    await Task.Delay(5000);
                    pullButton.DataContext.HideNotification(guid);
                });
                await buttonPanel.InsertElementAsync(pullWithStash, pullButtonElement.Value.Index + 1, cancellationToken);
                logger.LogTrace("[GitWindowActionButtonPanelInjector] pull-with-stash button created.");
            }
        }
        stopwatch.Stop();
        logger.LogTrace("[GitWindowActionButtonPanelInjector] exit '{method}', elapsed={elapsed}ms", nameof(InjectAsync), stopwatch.ElapsedMilliseconds);
    }
}
