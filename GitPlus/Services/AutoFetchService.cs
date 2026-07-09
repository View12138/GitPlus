using System.Diagnostics;

namespace GitPlus.Services;

/// <summary>
/// Periodically runs <c>git fetch --all --prune</c> on a configurable timer.
/// </summary>
[RequiredArgsConstructor]
public sealed partial class AutoFetchService : IDisposable
{
    private readonly GitCommandService git;
    private readonly ILogger logger;
    private Timer? timer;
    private bool disposed;

    public async Task StartAsync()
    {
        logger.LogTrace("[AutoFetchService] enter '{method}'", nameof(StartAsync));
        if (disposed) throw new ObjectDisposedException(nameof(AutoFetchService));
        if (timer is not null)
        {
            return;
        }

        var settings = Extensions.GetRequiredService<GitPlusOption>();
        if (!settings.AutoFetchEnabled)
        {
            logger.LogDebug("Auto-fetch is disabled by options — not starting timer.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, settings.AutoFetchIntervalMinutes));
        timer = new Timer(_ => FetchSafe(), null, interval, interval);
        logger.LogTrace("[AutoFetchService] exit '{method}'", nameof(StartAsync));
    }

    public void Stop()
    {
        logger.LogTrace("[AutoFetchService] enter '{method}'", nameof(Stop));
        if (timer is null)
        {
            return;
        }
        timer?.Dispose();
        timer = null;
        logger.LogTrace("[AutoFetchService] exit '{method}'", nameof(Stop));
    }

#pragma warning disable VSTHRD100 // Timer callback must be void
    private async void FetchSafe()
#pragma warning restore VSTHRD100
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace("[AutoFetchService] enter '{method}'", nameof(FetchSafe));
        try
        {
            var settings = Extensions.GetRequiredService<GitPlusOption>();
            if (!settings.AutoFetchEnabled)
            {
                logger.LogDebug("[AutoFetchService] callback skipped — disabled in options.");
                return;
            }

            var result = await git.FetchAsync(CancellationToken.None);
            if (result.IsSuccess)
            {
                logger.LogDebug("[AutoFetchService] fetch succeeded.");
            }

            var interval = TimeSpan.FromMinutes(Math.Max(1, settings.AutoFetchIntervalMinutes));
            timer?.Change(interval, interval);
            logger.LogDebug("[AutoFetchService] timer rescheduled to {Interval}min.", interval.TotalMinutes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Auto-fetch callback threw an unhandled exception.");
        }
        finally
        {
            stopwatch.Stop();
            logger.LogTrace("[AutoFetchService] exit '{method}', elapsed={elapsed}ms", nameof(FetchSafe), stopwatch.ElapsedMilliseconds);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Stop();
    }
}
