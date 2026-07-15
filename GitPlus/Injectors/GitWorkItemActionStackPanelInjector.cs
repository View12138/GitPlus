using System.Diagnostics;
using System.Windows.Resources;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;

namespace GitPlus.Injectors;

[RequiredArgsConstructor]
public sealed partial class GitWorkItemActionStackPanelInjector : InjectorBase
{
    private readonly ILogger logger;
    private readonly GitCommandService git;

    private const string ConventionalCommitsRulesResourceName = "conventional-commits-rules.json";
    private const string BreakingChangeType = "BREAKING CHANGE";
    private const string GeneratedScopesFileType = "GENERATE SCOPESFILE";

    public override bool CanInject(string caption)
        => caption != null && caption.IndexOf("git", StringComparison.OrdinalIgnoreCase) >= 0;

    public override async Task InjectAsync(string caption, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace("[GitWindowActionButtonPanelInjector] enter '{method}', caption=\"{Caption}\"", nameof(InjectAsync), caption);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var commentTextBox = await GitWindowLocator.LocateGitCommentTextBoxAsync(cancellationToken) as TextBox;
        if (commentTextBox is null)
        {
            logger.LogDebug("[GitWindowActionButtonPanelInjector] comment text box not found — aborting injection.");
            return;
        }
        var workItemActionsStackPanel = await GitWindowLocator.LocateGitWorkItemActionsStackPanelAsync(cancellationToken) as Panel;
        if (workItemActionsStackPanel is null)
        {
            logger.LogDebug("[GitWindowActionButtonPanelInjector] work item actions stack panel not found — aborting injection.");
            return;
        }
        var insertConventionalCommitItemElement = await workItemActionsStackPanel.GetChildIndexAsync("InsertConventionalCommitItem", cancellationToken);
        if (insertConventionalCommitItemElement is null)
        {
            var insertItemElement = await workItemActionsStackPanel.GetChildIndexAsync("InsertItem", cancellationToken);
            if (insertItemElement is not null && insertItemElement.Value.Element is ButtonBase insertItem)
            {
                var options = Extensions.GetRequiredService<GitPlusOption>();
                var insertConventionalCommitItem = new ImageButton();
                insertConventionalCommitItem.CopyLocalValuesFrom(insertItem);
                insertConventionalCommitItem.Name = "InsertConventionalCommitItem";
                insertConventionalCommitItem.ImageNormal = Application.Current.FindResource("CommentIconNormal") as ImageSource;
                insertConventionalCommitItem.ImageHover = Application.Current.FindResource("CommentIconHover") as ImageSource;
                insertConventionalCommitItem.ImagePressed = Application.Current.FindResource("CommentIconPressed") as ImageSource;
                insertConventionalCommitItem.Style = Application.Current.FindResource("GitActionButtonStyle") as Style;
                insertConventionalCommitItem.Visibility = options.Commit.ConventionalCommitsVisible ? Visibility.Visible : Visibility.Collapsed;
                insertConventionalCommitItem.ToolTip = Assets.Languages.InsertConventionalCommitToolTip;
                var contextMenu = new ContextMenu();
                {
                    var jsonStream = Application.GetResourceStream(ConventionalCommitsRulesResourceName, ResourceFolders.Assets, System.Globalization.CultureInfo.CurrentCulture);
                    if (jsonStream == null || jsonStream.ContentType != ContentTypes.ApplicationJson)
                    {
                        logger.LogError("conventional commits rules resources not found — aborting injection.");
                        return;
                    }
                    using var reader = new StreamReader(jsonStream.Stream);
                    var conventionalCommits = JsonConvert.DeserializeObject<ConventionalCommits>(reader.ReadToEnd());
                    if (conventionalCommits == null)
                    {
                        logger.LogError("conventional commits rules deserialize failed — aborting injection.");
                        return;
                    }
                    foreach (var rule in conventionalCommits.Rules)
                    {
                        if (rule.Type == BreakingChangeType || rule.Type == GeneratedScopesFileType)
                        {
                            contextMenu.Items.Add(new Separator());
                        }
                        contextMenu.Items.Add(new MenuItem()
                        {
                            Header = new TextBlock { Text = $"{rule.Title} ({rule.Type})", FontSize = 12, VerticalAlignment = VerticalAlignment.Center },
                            Padding = new Thickness(0, 4, 0, 4),
                            Icon = new TextBlock { Text = rule.Emoji, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center },
                            ToolTip = rule.Description,
                            Command = new AsyncRelayCommand(async () =>
                            {
                                var _options = Extensions.GetRequiredService<GitPlusOption>();
                                var dte = Extensions.GetRequiredService<EnvDTE.DTE>();
                                var solutionName = dte.Solution?.FullName;
                                var scopeFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(solutionName), _options.Commit.ConventionalCommitScopeFileName);

                                if (rule.Type == GeneratedScopesFileType)
                                {
                                    if (File.Exists(scopeFileName))
                                    {
                                        MessageDialog.Show(Assets.Languages.ScopesFileExistsTitle, Assets.Languages.ScopesFileExistsMessage, MessageDialogCommandSet.Ok);
                                    }
                                    else
                                    {
                                        var scopes = JsonConvert.SerializeObject(new ConventionalCommitScopes() { Scopes = ["project"] });
                                        File.WriteAllText(scopeFileName, scopes);
                                    }

                                }
                                else if (rule.Type == BreakingChangeType)
                                {
                                    commentTextBox.Text += $"{Environment.NewLine}{Environment.NewLine}BREAKING CHANGE: ";
                                    commentTextBox.CaretIndex = commentTextBox.Text.Length;
                                }
                                else
                                {
                                    string currentType = string.Empty;
                                    foreach (var _rule in conventionalCommits.Rules)
                                    {
                                        if (commentTextBox.Text.StartsWith(_rule.Type))
                                            currentType = _rule.Type;
                                    }
                                    if (!string.IsNullOrEmpty(currentType))
                                    {
                                        commentTextBox.Text = commentTextBox.Text.ReplaceFirst(currentType, rule.Type);
                                        commentTextBox.CaretIndex = commentTextBox.Text.Length;
                                        return;
                                    }
                                    if (_options.Commit.UseConventionalCommitScope && File.Exists(scopeFileName))
                                    {
                                        var scopes = JsonConvert.DeserializeObject<ConventionalCommitScopes>(File.ReadAllText(scopeFileName));
                                        if (scopes != null && scopes.Scopes.Count > 0)
                                        {
                                            var contextMenu = new ContextMenu();
                                            {
                                                foreach (var scope in scopes.Scopes)
                                                {
                                                    contextMenu.Items.Add(new MenuItem()
                                                    {
                                                        Header = new TextBlock { Text = $"{scope}", FontSize = 12, VerticalAlignment = VerticalAlignment.Center },
                                                        Padding = new Thickness(0, 4, 0, 4),
                                                        Command = new AsyncRelayCommand(async () =>
                                                        {
                                                            commentTextBox.Text = commentTextBox.Text.Insert(0, $"{rule.Type}({scope}): ");
                                                            commentTextBox.CaretIndex = commentTextBox.Text.Length;
                                                        })
                                                    });
                                                }
                                            }
                                            contextMenu.PlacementTarget = insertConventionalCommitItem;
                                            contextMenu.IsOpen = true;
                                            return;
                                        }
                                    }
                                    commentTextBox.Text = commentTextBox.Text.Insert(0, $"{rule.Type}: ");
                                    commentTextBox.CaretIndex = commentTextBox.Text.Length;
                                }
                            }),
                        });
                    }
                }
                insertConventionalCommitItem.Command = new AsyncRelayCommand(async () =>
                {
                    contextMenu.PlacementTarget = insertConventionalCommitItem;
                    contextMenu.IsOpen = true;
                });
                await workItemActionsStackPanel.InsertElementAsync(insertConventionalCommitItem, insertItemElement.Value.Index + 1, cancellationToken);
                logger.LogTrace("[GitWindowActionButtonPanelInjector] pull-with-stash button created.");
            }
        }
        stopwatch.Stop();
        logger.LogTrace("[GitWindowActionButtonPanelInjector] exit '{method}', elapsed={elapsed}ms", nameof(InjectAsync), stopwatch.ElapsedMilliseconds);
    }
}
