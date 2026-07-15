using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GitPlus.Commons;

/// <summary>
/// Writes log messages to a dedicated Visual Studio Output Window pane named "Git +".
/// Implements the standard <see cref="ILogger"/> interface.
/// </summary>
/// <remarks>Creates a new <see cref="OutputWindowLogger"/>.</remarks>
/// <param name="categoryName">The category name shown on the Output Window drop-down.</param>
public sealed class OutputWindowLogger(string categoryName = "Git +") : ILogger
{
    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        try
        {
            var option = Extensions.GetService<GitPlusOption>();
            return logLevel != LogLevel.None && logLevel >= (option?.Logging.LogLevel ?? LogLevel.Information);
        }
        catch
        {
            return logLevel != LogLevel.None;
        }
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);

        var level = logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "FATAL",
            _ => "?????"
        };

        Write(level, message);

        if (exception is not null && logLevel >= LogLevel.Error)
            Write(level, $"  \u2192 {exception.GetType().Name}: {exception.Message}");
    }

    private void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}][{level}] {message}";
        System.Diagnostics.Debug.WriteLine($"[Git +] {line}");
        if (ThreadHelper.CheckAccess())
        {
            WriteCore(line);
        }
        else
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                WriteCore(line);
            }).FileAndForget("GitPlus/logger");
        }
    }

    private IVsOutputWindowPane? pane;
    private void WriteCore(string line)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            if (pane == null)
            {
                var paneGuid = Guid.NewGuid();
                var outputWindow = Extensions.GetRequiredService<IVsOutputWindow>();
                outputWindow.CreatePane(ref paneGuid, categoryName, fInitVisible: 1, fClearWithSolution: 0);
                outputWindow.GetPane(ref paneGuid, out pane);
            }
            pane?.OutputString(line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Git +]  \u2192 {ex.GetType().Name}: {ex.Message}");
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
