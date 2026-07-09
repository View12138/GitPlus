namespace GitPlus.Commons;

/// <summary>
/// Encapsulates the outcome of a Git operation.
/// </summary>
/// <remarks>
/// Always constructed via the factory methods <see cref="Success"/> or
/// <see cref="Failure"/> — never directly — to make intent explicit at call sites.
/// </remarks>
public sealed class GitResult
{
    /// <summary>Gets <c>true</c> if the operation completed without errors.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets the standard output from the Git process, or <c>null</c> on failure.</summary>
    public string Output { get; }

    /// <summary>Gets the error message or stderr text, or <c>null</c> on success.</summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the exception that caused the failure, or <c>null</c> if the failure
    /// was a non-exceptional Git error (e.g. merge conflict, dirty worktree).
    /// </summary>
    public Exception? Exception { get; }

    private GitResult(bool isSuccess, string? output, string? error, Exception? exception)
    {
        IsSuccess = isSuccess;
        Output = output ?? string.Empty;
        Error = error;
        Exception = exception;
    }

    /// <summary>Creates a successful <see cref="GitResult"/>.</summary>
    /// <param name="output">Optional standard output from the Git process.</param>
    public static GitResult Success(string? output = null)
        => new(true, output, null, null);

    /// <summary>Creates a failed <see cref="GitResult"/>.</summary>
    /// <param name="error">A human-readable error message.</param>
    /// <param name="exception">The causing exception, if any.</param>
    public static GitResult Failure(string error, Exception? exception = null)
        => new(false, null, error, exception);

    /// <inheritdoc />
    public override string ToString()
        => IsSuccess
            ? $"Success: {Output ?? "(no output)"}"
            : $"Failure: {Error ?? "(no message)"}";
}
