using System.Diagnostics;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace GitPlus;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(PackageGuidString)]
[ProvideOptionPage(typeof(GitPlusOptionPage), "Git +", "General", 0, 0, true)]
[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
public sealed class GitPlusPackage : AsyncPackage
{
    public const string PackageGuidString = "3501f9fc-3ea2-4112-ae05-c14ab051dc79";

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        var stopwatch = Stopwatch.StartNew();
        var logger = new OutputWindowLogger("Git +");
        logger.LogTrace("[GitPlusPackage] enter '{method}'", nameof(InitializeAsync));
        try
        {
            var services = new ServiceCollection()
                .AddSingleton<ILogger>(logger)
                .AddSingleton<WindowWatcher>()
                .AddSingleton<GitCommandService>()
                .AddSingleton<AutoFetchService>()
                .AddTransient(p => (GetDialogPage(typeof(GitPlusOptionPage)) as GitPlusOptionPage)?.ToOption()!)
                .AddTransient(p => (GetGlobalService(typeof(DTE)) as DTE)!)
                .AddTransient(p => (GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow)!)
                ;

            foreach (var injectorType in typeof(GitPlusPackage).Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(InjectorBase))))
            {
                services.AddSingleton(injectorType);
                logger.LogDebug("[GitPlusPackage] injector registered: {Type}", injectorType.Name);
            }
            Extensions.BuildServiceProvider(services);
            logger.LogDebug("[GitPlusPackage] DI container built with {ServiceCount} services.", services.Count);

            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = "SharedResources.xaml".GetResourceUri("WPF","Microsoft.TeamFoundation.Controls") });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = "Icons.xaml".GetResourceUri(ResourceFolders.Resources) });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = "GitButtonStyle.xaml".GetResourceUri(ResourceFolders.Resources) });
            logger.LogDebug("[GitPlusPackage] XAML resource dictionaries merged.");

            var watcher = Extensions.GetRequiredService<WindowWatcher>();
            var autoFetch = Extensions.GetRequiredService<AutoFetchService>();
            watcher.WindowCreated += (s, e) => ScheduleProcess(e.Caption);
            watcher.WindowActivated += (s, e) => ScheduleProcess(e.Caption);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await watcher.StartAsync(cancellationToken);
            await autoFetch.StartAsync();
            logger.LogDebug("[GitPlusPackage] GitPlus initialization complete.");
        }
        catch (OperationCanceledException ex) { logger.LogWarning(ex, "Initialization canceled."); }
        catch (Exception ex) { logger.LogCritical(ex, "Initialization failed."); }
        finally
        {
            stopwatch.Stop();
            logger.LogTrace("[GitPlusPackage] exit '{method}', elapsed={elapsed}ms", nameof(InitializeAsync), stopwatch.ElapsedMilliseconds);
        }
    }

#pragma warning disable VSTHRD100
    private async void ScheduleProcess(string caption)
#pragma warning restore VSTHRD100
    {
        var stopwatch = Stopwatch.StartNew();
        var logger = Extensions.GetRequiredService<ILogger>();
        logger.LogTrace("[GitPlusPackage] enter '{method}', caption=\"{Caption}\"", nameof(ScheduleProcess), caption);
        await Task.Delay(200);
        foreach (var injectorType in typeof(GitPlusPackage).Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(InjectorBase))))
        {
            if (Extensions.GetService(injectorType) is not InjectorBase injector) continue;
            if (!injector.CanInject(caption))
            {
                logger.LogDebug("[GitPlusPackage] injector {Type} skipped for \"{Caption}\"", injectorType.Name, caption);
                continue;
            }
            logger.LogDebug("[GitPlusPackage] injector {Type} processing \"{Caption}\"...", injectorType.Name, caption);
            try
            {
                await injector.InjectAsync(caption);
                logger.LogDebug("[GitPlusPackage] injector {Type} completed for \"{Caption}\".", injectorType.Name, caption);
            }
            catch (OperationCanceledException ex) { logger.LogWarning(ex, "Injection canceled for {Type}.", injectorType.Name); }
            catch (Exception ex) { logger.LogError(ex, "Injection failed for {Type}.", injectorType.Name); }
        }
        stopwatch.Stop();
        logger.LogTrace("[GitPlusPackage] exit '{method}', elapsed={elapsed}ms", nameof(ScheduleProcess), stopwatch.ElapsedMilliseconds);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                var watcher = Extensions.GetService<WindowWatcher>();
                watcher?.Dispose();
                Extensions.GetService<ILogger>()?.LogInformation("GitPlus disposed.");
            }
            catch (Exception ex)
            {
                Extensions.GetService<ILogger>()?.LogError(ex, "Dispose failed.");
            }
        }
        base.Dispose(disposing);
    }
}
