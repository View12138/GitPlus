using System.Diagnostics;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Window = EnvDTE.Window;

namespace GitPlus.Services;

[RequiredArgsConstructor]
public sealed partial class WindowWatcher : IDisposable
{
    private readonly ILogger logger;
    private WindowEvents? windowEvents;
    private volatile bool isRunning;
    private bool disposed;

    public event EventHandler<WindowEventArgs>? WindowCreated;
    public event EventHandler<WindowEventArgs>? WindowClosed;
    public event EventHandler<WindowEventArgs>? WindowActivated;

    /// <inheritdoc />
    public bool IsRunning => isRunning;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace("[WindowWatcher] enter '{method}'", nameof(StartAsync));
        if (isRunning)
        {
            return;
        }

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        try
        {
            var dte = Extensions.GetRequiredService<DTE>();
            windowEvents = dte.Events.WindowEvents;
            windowEvents.WindowCreated += OnDteWindowCreated;
            windowEvents.WindowActivated += OnDteWindowActivated;
            windowEvents.WindowClosing += OnDteWindowClosing;

            isRunning = true;
            logger.LogDebug("WindowWatcher started (DTE.Events.WindowEvents).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WindowWatcher.StartAsync failed.");
        }
        finally
        {
            stopwatch.Stop();
            logger.LogTrace("[WindowWatcher] exit '{method}', elapsed={elapsed}ms", nameof(StartAsync), stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        logger.LogTrace("[WindowWatcher] enter '{method}'", nameof(Stop));
        if (!isRunning)
        {
            return;
        }
        isRunning = false;

        if (windowEvents is not null)
        {
            try
            {
                windowEvents.WindowCreated -= OnDteWindowCreated;
                windowEvents.WindowActivated -= OnDteWindowActivated;
                windowEvents.WindowClosing -= OnDteWindowClosing;
                logger.LogDebug("[WindowWatcher] unsubscribed from DTE.WindowEvents.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "WindowWatcher: failed to unsubscribe from DTE.WindowEvents.");
            }
            windowEvents = null;
        }

        logger.LogTrace("[WindowWatcher] exit '{method}'", nameof(Stop));
    }

    // ── DTE WindowEvents callbacks (always on UI thread) ─────────────────────

    private void OnDteWindowCreated(Window window)
    {
        var caption = window?.Caption ?? string.Empty;
        logger.LogDebug("[WindowWatcher] DTE WindowCreated: \"{Caption}\" (Kind={Kind})", caption, window?.Kind);
        WindowCreated?.Invoke(this, new WindowEventArgs(caption, window));
    }

    private void OnDteWindowActivated(Window gotFocus, Window lostFocus)
    {
        var caption = gotFocus?.Caption ?? string.Empty;
        var lostCaption = lostFocus?.Caption ?? string.Empty;
        logger.LogDebug("[WindowWatcher] DTE WindowActivated: \"{Caption}\" (lost: \"{Lost}\")", caption, lostCaption);
        WindowActivated?.Invoke(this, new WindowEventArgs(caption, gotFocus));
    }

    private void OnDteWindowClosing(Window window)
    {
        var caption = window?.Caption ?? string.Empty;
        logger.LogDebug("[WindowWatcher] DTE WindowClosing: \"{Caption}\"", caption);
        WindowClosed?.Invoke(this, new WindowEventArgs(caption, window));
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Stop();
    }
}

public sealed class WindowEventArgs(string caption, object? frame = null) : EventArgs
{
    public string Caption { get; } = caption ?? string.Empty;
    public object? Frame { get; } = frame;
}
