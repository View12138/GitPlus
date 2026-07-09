using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using Path = System.IO.Path;

namespace GitPlus.Services;

[RequiredArgsConstructor]
public sealed partial class GitCommandService
{
    private readonly ILogger logger;

    public Task<GitResult> StatusAsync(CancellationToken cancellationToken = default)
        => RunAsync("status", cancellationToken);

    public Task<GitResult> FetchAsync(CancellationToken cancellationToken = default)
        => RunAsync("fetch --all --prune", cancellationToken);

    public Task<GitResult> PullAsync(bool rebase = false, CancellationToken cancellationToken = default)
        => RunAsync($"pull{(rebase ? " --rebase=true" : "")}", cancellationToken);

    public Task<GitResult> PushAsync(CancellationToken cancellationToken = default)
        => RunAsync("push", cancellationToken);

    public Task<GitResult> StashPushAsync(string message = "PULL_AUTO_STASH", CancellationToken cancellationToken = default)
        => RunAsync($"stash push -m \"{message}\"", cancellationToken);
    public Task<GitResult> StashPopAsync(int index = 0, CancellationToken cancellationToken = default)
        => RunAsync($"stash pop -q --index \"stash@{{{index}}}\"", cancellationToken);

    private async Task<GitResult> RunAsync(string args, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace("[GitCommandService] enter '{method}', params:{args}", nameof(RunAsync), args);

        var directory = await GetRepositoryDirectoryAsync(cancellationToken);
        if (directory is null)
        {
            logger.LogWarning("git {args}: no git repository found.", args);
            return GitResult.Failure("No Git repository found.");
        }

        logger.LogDebug("[GitCommandService] git {args} starting in \"{directory}\"", args, directory);
        try { return await Task.Run(() => Execute(args, directory, cancellationToken), cancellationToken); }
        catch (OperationCanceledException)
        {
            logger.LogWarning("git {args} cancelled.", args);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "git {args} threw an unhandled exception.", args);
            return GitResult.Failure(ex.Message, ex);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogTrace("[GitCommandService] exit '{method}', elapsed={elapsed}ms", nameof(RunAsync), stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<string?> GetRepositoryDirectoryAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace("[GitCommandService] enter '{method}'", nameof(GetRepositoryDirectoryAsync));
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        try
        {
            var dte = Extensions.GetRequiredService<EnvDTE.DTE>();
            var solutionName = dte.Solution?.FullName;
            if (string.IsNullOrEmpty(solutionName))
            {
                logger.LogWarning("no solution loaded — cannot determine Git root.");
                return null;
            }
            var gitDirectory = Path.Combine(Path.GetDirectoryName(solutionName), ".git");
            if (!Directory.Exists(gitDirectory))
            {
                gitDirectory = null;
                logger.LogWarning("no .git directory found above solution path \"{solutionDirectory}\".", Path.GetDirectoryName(solutionName));
            }
            return Path.GetDirectoryName(gitDirectory);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to resolve repository directory via DTE.");
            return null;
        }
        finally
        {
            stopwatch.Stop();
            logger.LogTrace("[GitCommandService] exit '{method}', elapsed={elapsed}ms", nameof(GetRepositoryDirectoryAsync), stopwatch.ElapsedMilliseconds);
        }
    }

    private GitResult Execute(string args, string dir, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogTrace("[GitCommandService] enter '{method}'", nameof(Execute));
        using var p = new Process();
        try
        {
            var option = Extensions.GetRequiredService<GitPlusOption>();
            var gitFileName = "git";
            if (!string.IsNullOrWhiteSpace(option.GitFilePath)
                && Path.GetFileName(option.GitFilePath).Equals("git.exe", StringComparison.OrdinalIgnoreCase)
                && File.Exists(option.GitFilePath))
            {
                gitFileName = option.GitFilePath;
            }
            p.StartInfo = new ProcessStartInfo
            {
                FileName = gitFileName,
                Arguments = args,
                WorkingDirectory = dir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            p.Start();
            logger.LogInformation("git {args}", args);
            logger.LogDebug("[GitCommandService] process started ({git}) (PID={pid}) (cwd={dir})", gitFileName, p.Id, dir);

            var outTask = Task.Run(() => p.StandardOutput.ReadToEnd());
            var errTask = Task.Run(() => p.StandardError.ReadToEnd());
            if (!Task.WhenAll(outTask, errTask).Wait(option.TimeoutSeconds * 1000, ct))
            {
                try { p.Kill(); }
                catch (Exception killEx) { logger.LogError(killEx, "failed to kill git {args} (PID={Pid}).", args, p.Id); }
                logger.LogWarning("git {args} timed out after {Timeout}s.", args, option.TimeoutSeconds);
                return GitResult.Failure($"Timed out after {option.TimeoutSeconds}s.");
            }
            p.WaitForExit();
            var o = outTask.Result.Trim();
            var e = errTask.Result.Trim();
            logger.LogDebug("[GitCommandService] process exited (PID={Pid}, ExitCode={ExitCode}, Output={output}, Error={error}).", p.Id, p.ExitCode, o, e);

            if (p.ExitCode == 0)
            {
                return GitResult.Success(o);
            }
            else
            {
                return GitResult.Failure(e.Length > 0 ? e : $"Exit {p.ExitCode}");
            }
        }
        catch (OperationCanceledException ocex)
        {
            try { p.Kill(); }
            catch (Exception killEx) { logger.LogError(killEx, "failed to kill git {args} on cancellation.", args); }
            logger.LogWarning(ocex, "git {args} cancelled.", args);
            return GitResult.Failure(ocex.Message, ocex);
        }
        catch (Exception ex)
        {
            try { p.Kill(); }
            catch (Exception killEx) { logger.LogError(killEx, "failed to kill git {args} on exception.", args); }
            logger.LogError(ex, "git {args} threw an exception.", args);
            return GitResult.Failure(ex.Message, ex);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogTrace("[GitCommandService] exit '{method}', elapsed={elapsed}ms", nameof(Execute), stopwatch.ElapsedMilliseconds);
        }
    }
}
