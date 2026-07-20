using System.Diagnostics;
using System.Windows.Markup;
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
    private const string GeneratedOptionFileType = "GENERATE OPTIONFILE";

    public override bool CanInject(string caption)
        => caption != null && caption.IndexOf("git", StringComparison.OrdinalIgnoreCase) >= 0;

    public override async Task InjectAsync(string caption, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace("[GitWindowActionButtonPanelInjector] enter '{method}', caption=\"{Caption}\"", nameof(InjectAsync), caption);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);


        Brush errorBrush = Application.Current.TryFindResource("IconErrorFillBrushKey") as Brush ?? Brushes.Red;
        Brush warningBrush = Application.Current.TryFindResource("IconWarningFillBrushKey") as Brush ?? Brushes.Yellow;

        var commentAreaBorder = await GitWindowLocator.LocateChildElementAsync("commentAreaBorder", cancellationToken) as Border;
        if (commentAreaBorder is null)
        {
            logger.LogDebug("[GitWindowActionButtonPanelInjector] comment area border not found — aborting injection.");
            return;
        }

        var commitButton = await GitWindowLocator.LocateChildElementAsync("commitButton", cancellationToken) as ButtonBase;
        if (commitButton is null)
        {
            logger.LogDebug("[GitWindowActionButtonPanelInjector] comment area border not found — aborting injection.");
            return;
        }

        var commentTextBox = await GitWindowLocator.LocateChildElementAsync("commentTextBox", cancellationToken) as TextBox;
        if (commentTextBox is null)
        {
            logger.LogDebug("[GitWindowActionButtonPanelInjector] comment text box not found — aborting injection.");
            return;
        }

        var errorMessage = await GitWindowLocator.LocateChildElementAsync("errorMessage", cancellationToken);
        if (errorMessage is null)
        {
            logger.LogDebug("[GitWindowActionButtonPanelInjector] error message not found — aborting injection.");
            return;
        }
        var workItemActionsStackPanel = await GitWindowLocator.LocateGitWorkItemActionsStackPanelAsync(cancellationToken) as Panel;
        if (workItemActionsStackPanel is null)
        {
            logger.LogDebug("[GitWindowActionButtonPanelInjector] work item actions stack panel not found — aborting injection.");
            return;
        }
        bool needAddTextChangedEvent = true;
        var insertConventionalCommitItemElement = await workItemActionsStackPanel.GetChildIndexAsync("InsertConventionalCommitItem", cancellationToken);
        if (insertConventionalCommitItemElement is not null)
        {
            needAddTextChangedEvent = false;
            workItemActionsStackPanel.RemoveElement(insertConventionalCommitItemElement.Value.Element);
        }

        var insertItemElement = await workItemActionsStackPanel.GetChildIndexAsync("InsertItem", cancellationToken);
        if (insertItemElement is not null && insertItemElement.Value.Element is ButtonBase insertItem)
        {
            var options = Extensions.GetRequiredService<GitPlusOption>();
            var rules = new List<ConventionalCommitRule>();
            var footers = new List<string>();
            ConventionalCommitOption? commitOption = null;
            {
                var dte = Extensions.GetRequiredService<EnvDTE.DTE>();
                var solutionName = dte.Solution?.FullName;
                var optionFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(solutionName), options.Commit.ConventionalCommitOptionFileName);
                if (File.Exists(optionFileName))
                {
                    commitOption = JsonConvert.DeserializeObject<ConventionalCommitOption>(File.ReadAllText(optionFileName));
                }
            }
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
                contextMenu.Background = Application.Current.FindResource("EmbeddedDialogBackgroundBrushKey") as Brush;
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
                    if (rule.Type == BreakingChangeType || rule.Type == GeneratedOptionFileType)
                    {
                        contextMenu.Items.Add(new Separator());
                        if (rule.Type == BreakingChangeType)
                        { footers.Add(rule.Type); }
                    }
                    else
                    {
                        rules.Add(rule);
                    }
                    contextMenu.Items.Add(BuildMenuItem(commentTextBox, insertConventionalCommitItem, conventionalCommits, rule));
                }
                if (commitOption != null && commitOption.CustomRules.Count > 0)
                {
                    contextMenu.Items.Add(new Separator());
                    foreach (var rule in commitOption.CustomRules)
                    {
                        rules.Add(rule);
                        contextMenu.Items.Add(BuildMenuItem(commentTextBox, insertConventionalCommitItem, conventionalCommits, rule));
                    }
                }
                if (commitOption != null && commitOption.CustomFooters.Count > 0)
                {
                    footers.AddRange(commitOption.CustomFooters);
                }

            }
            insertConventionalCommitItem.Command = new AsyncRelayCommand(async () =>
            {
                contextMenu.PlacementTarget = insertConventionalCommitItem;
                contextMenu.IsOpen = true;
            });
            if (needAddTextChangedEvent
                && commitOption?.EnableStrictValidation == true
                && errorMessage.Parent is FrameworkElement errorMessageGrid
                && commentAreaBorder.Parent is FrameworkElement commentAreaBorderGrid)
            {
                var systemFillCriticalBrush = Application.Current.GetResource("SystemFillCriticalBrushKey") as Brush;
                var errorMessaggeNew = new TextBlock
                {
                    Name = "errorMessaggeNew",
                    Text = "⚠️ 格式不符合约定式提交规范。",
                    Visibility = Visibility.Collapsed,
                    Foreground = systemFillCriticalBrush,
                };
                {
                    errorMessaggeNew.SetValue(Grid.RowProperty, errorMessage.GetValue(Grid.RowProperty));
                    errorMessaggeNew.SetValue(Grid.ColumnProperty, errorMessage.GetValue(Grid.ColumnProperty));
                    errorMessaggeNew.SetValue(TextBlock.PaddingProperty, errorMessage.GetValue(TextBlock.PaddingProperty));
                    errorMessaggeNew.SetValue(FrameworkElement.MarginProperty, errorMessage.GetValue(FrameworkElement.MarginProperty));
                    await errorMessageGrid.InsertElementAsync(errorMessaggeNew);
                }
                var commentAreaBorderNew = new Border
                {
                    Name = "commentAreaBorderNew",
                    Visibility = Visibility.Collapsed,
                    BorderThickness = commentAreaBorder.BorderThickness,
                    BorderBrush = systemFillCriticalBrush,
                    CornerRadius = commentAreaBorder.CornerRadius,
                    Background = Brushes.Transparent,
                    Padding = commentAreaBorder.Padding,
                    Margin = commentAreaBorder.Margin,
                    IsHitTestVisible = false,
                };
                {
                    commentAreaBorderNew.SetValue(Grid.RowProperty, commentAreaBorder.GetValue(Grid.RowProperty));
                    commentAreaBorderNew.SetValue(Grid.ColumnProperty, commentAreaBorder.GetValue(Grid.ColumnProperty));
                    commentAreaBorderNew.SetValue(TextBlock.PaddingProperty, commentAreaBorder.GetValue(TextBlock.PaddingProperty));
                    commentAreaBorderNew.SetValue(FrameworkElement.MarginProperty, commentAreaBorder.GetValue(FrameworkElement.MarginProperty));
                    await commentAreaBorderGrid.InsertElementAsync(commentAreaBorderNew);
                }
                // 强制验证约定式提交格式
                var defaultIsEnabled = commitButton.IsEnabled;
                var typePattern = string.Join("|", rules.Select(r => Regex.Escape(r.Type)));
                var parserSettings = new ParserSettings()
                {
                    AllowTypes = [.. rules.Select(r => r.Type)],
                    AllowScopes = commitOption.Scopes,
                    AllowFooters = [.. footers],
                };
                var layer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(commentTextBox);
                commentTextBox.TextChanged += (s, e) => TextChanged(commentTextBox, errorBrush, warningBrush, commitButton, commentTextBox, errorMessaggeNew, commentAreaBorderNew, defaultIsEnabled, parserSettings, layer);
                var scrollViewer = await commentTextBox.FindChildAsync("PART_ContentHost") as ScrollViewer;
                scrollViewer?.ScrollChanged += (s, e) => TextChanged(commentTextBox, errorBrush, warningBrush, commitButton, commentTextBox, errorMessaggeNew, commentAreaBorderNew, defaultIsEnabled, parserSettings, layer);
                TextChanged(commentTextBox, errorBrush, warningBrush, commitButton, commentTextBox, errorMessaggeNew, commentAreaBorderNew, defaultIsEnabled, parserSettings, layer);
            }
            await workItemActionsStackPanel.InsertElementAsync(insertConventionalCommitItem, insertItemElement.Value.Index + 1, cancellationToken);
            logger.LogTrace("[GitWindowActionButtonPanelInjector] pull-with-stash button created.");
        }

        stopwatch.Stop();
        logger.LogTrace("[GitWindowActionButtonPanelInjector] exit '{method}', elapsed={elapsed}ms", nameof(InjectAsync), stopwatch.ElapsedMilliseconds);

        static MenuItem BuildMenuItem(TextBox commentTextBox, ImageButton insertConventionalCommitItem, ConventionalCommits conventionalCommits, ConventionalCommitRule rule) => new MenuItem()
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
                var scopeFileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(solutionName), _options.Commit.ConventionalCommitOptionFileName);

                if (rule.Type == GeneratedOptionFileType)
                {
                    if (File.Exists(scopeFileName))
                    {
                        MessageDialog.Show(Assets.Languages.OptionFileExistsTitle, Assets.Languages.OptionFileExistsMessage, MessageDialogCommandSet.Ok);
                    }
                    else
                    {
                        var scopes = JsonConvert.SerializeObject(new ConventionalCommitOption()
                        {
                            EnableStrictValidation = true,
                            Scopes = ["project"],
                            CustomRules = [new ConventionalCommitRule()
                                            {
                                                 Type = "custom",
                                                 Emoji = "✨",
                                                 Title = "Custom",
                                                 Description = "custom conventional commit rule",
                                            }]
                        });
                        File.WriteAllText(scopeFileName, scopes);
                    }

                }
                else if (rule.Type == BreakingChangeType)
                {
                    commentTextBox.Text += $"{Environment.NewLine}BREAKING CHANGE: ";
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
                    commentTextBox.Text = commentTextBox.Text.Insert(0, $"{rule.Type}: ");
                    commentTextBox.CaretIndex = commentTextBox.Text.Length;
                }
            }),
        };

        static void TextChanged(TextBox textBox, Brush errorBrush, Brush warningBrush, ButtonBase commitButton, TextBox commentTextBox, TextBlock errorMessaggeNew, Border commentAreaBorderNew, bool defaultIsEnabled, ParserSettings parserSettings, System.Windows.Documents.AdornerLayer layer)
        {
            commentTextBox.UpdateLayout();
            var adorners = layer.GetAdorners(commentTextBox);
            if (adorners != null)
            {
                foreach (var adorner in adorners)
                { layer.Remove(adorner); }
            }
            var txt = textBox.Text;
            if (string.IsNullOrWhiteSpace(txt))
            {
                commitButton.IsEnabled = defaultIsEnabled;
                errorMessaggeNew.Visibility = Visibility.Collapsed;
                commentAreaBorderNew.Visibility = Visibility.Collapsed;
                return;
            }

            var root = Parser.Parse(txt, parserSettings);
            foreach (var diagnostic in root.Diagnostics.Where(x => x.Severity != DiagnosticSeverity.Info))
            {
                layer.Add(new WaveUnderlineAdorner(commentTextBox, diagnostic.Span, diagnostic.Severity == DiagnosticSeverity.Error ? errorBrush : warningBrush));
            }

            if (root.Diagnostics.Any(x => x.Severity != DiagnosticSeverity.Info))
            {
                commitButton.IsEnabled = false;
                errorMessaggeNew.Visibility = Visibility.Visible;
                errorMessaggeNew.Text = string.Join(", ", root.Diagnostics.Where(x => x.Severity != DiagnosticSeverity.Info).Select(x => $"{x.Message} {x.Span}"));
                errorMessaggeNew.ToolTip = errorMessaggeNew.Text;
                commentAreaBorderNew.Visibility = Visibility.Visible;
            }
            else
            {
                commitButton.IsEnabled = defaultIsEnabled;
                errorMessaggeNew.Visibility = Visibility.Collapsed;
                commentAreaBorderNew.Visibility = Visibility.Collapsed;
            }
        }
    }
}
