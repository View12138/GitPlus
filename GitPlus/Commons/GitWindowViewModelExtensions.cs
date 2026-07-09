#pragma warning disable IDE0060,CS8625

using System.Reflection;

namespace GitPlus.Commons;

public static class GitWindowViewModelExtensions
{
    private const string GitWindowViewModelFullName = "Microsoft.TeamFoundation.Git.Controls.GitWindow.GitWindowViewModel";

    private static (MethodInfo Method, ParameterInfo[] Parameters)? GetViewModelMethod(ILogger logger, object dataContext, string methodName, int expectedParameterCount)
    {
        var dataContextType = dataContext.GetType();
        if (dataContextType.FullName != GitWindowViewModelFullName)
        {
            logger.LogDebug("[GitWindowViewModelExtensions] DataContext type mismatch: expected {ExpectedType}, actual {ActualType}.", GitWindowViewModelFullName, dataContextType.FullName);
            return null;
        }

        var method = dataContextType.GetMethod(methodName);
        if (method is null)
        {
            logger.LogWarning("Method '{MethodName}' not found on {TypeName}.", methodName, dataContextType.FullName);
            return null;
        }

        var parameterInfos = method.GetParameters();
        if (parameterInfos.Length != expectedParameterCount)
        {
            logger.LogWarning("Method '{MethodName}' parameter count mismatch: expected {Expected}, actual {Actual}.", methodName, expectedParameterCount, parameterInfos.Length);
            return null;
        }

        logger.LogTrace("[GitWindowViewModelExtensions] resolved method '{MethodName}' on {TypeName} with {ParamCount} parameters.", methodName, dataContextType.FullName, parameterInfos.Length);
        return (method, parameterInfos);
    }

    public static void ShowNotification(this object dataContext, string message, NotificationType type = NotificationType.Information, NotificationFlags flags = NotificationFlags.None, ICommand command = null, Guid guid = default)
    {
        var logger = Extensions.GetRequiredService<ILogger>();
        var result = GetViewModelMethod(logger, dataContext, nameof(ShowNotification), 5);
        if (result is null) return;
        var (method, parameterInfos) = result.Value;

        var vsTypeValue = Enum.ToObject(parameterInfos[1].ParameterType, (int)type);
        var vsFlagsValue = Enum.ToObject(parameterInfos[2].ParameterType, (int)flags);

        try
        {
            method.Invoke(dataContext, [message, vsTypeValue, vsFlagsValue, command, guid]);
            logger.LogTrace("[GitWindowViewModelExtensions] ShowNotification invoked (type={Type}, flags={Flags}).", type, flags);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to invoke ShowNotification on {TypeName}.", dataContext.GetType().FullName);
        }
    }

    public static void ShowError(this object dataContext, string errorMessage)
    {
        var logger = Extensions.GetRequiredService<ILogger>();
        var result = GetViewModelMethod(logger, dataContext, nameof(ShowError), 1);
        if (result is null) return;
        var (method, _) = result.Value;

        try
        {
            method.Invoke(dataContext, [errorMessage]);
            logger.LogTrace("[GitWindowViewModelExtensions] ShowError invoked.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to invoke ShowError on {TypeName}.", dataContext.GetType().FullName);
        }
    }

    public static void ShowException(this object dataContext, Exception ex)
    {
        var logger = Extensions.GetRequiredService<ILogger>();
        var result = GetViewModelMethod(logger, dataContext, nameof(ShowException), 1);
        if (result is null) return;
        var (method, _) = result.Value;

        try
        {
            method.Invoke(dataContext, [ex]);
            logger.LogTrace("[GitWindowViewModelExtensions] ShowException invoked (exception={ExceptionType}).", ex.GetType().Name);
        }
        catch (Exception invokeEx)
        {
            logger.LogError(invokeEx, "Failed to invoke ShowException on {TypeName}.", dataContext.GetType().FullName);
        }
    }

    public static void ClearNotifications(this object dataContext)
    {
        var logger = Extensions.GetRequiredService<ILogger>();
        var result = GetViewModelMethod(logger, dataContext, nameof(ClearNotifications), 0);
        if (result is null) return;
        var (method, _) = result.Value;

        try
        {
            method.Invoke(dataContext, []);
            logger.LogTrace("[GitWindowViewModelExtensions] ClearNotifications invoked.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to invoke ClearNotifications on {TypeName}.", dataContext.GetType().FullName);
        }
    }

    public static void HideNotification(this object dataContext, Guid id)
    {
        var logger = Extensions.GetRequiredService<ILogger>();
        var result = GetViewModelMethod(logger, dataContext, nameof(HideNotification), 1);
        if (result is null) return;
        var (method, _) = result.Value;

        try
        {
            method.Invoke(dataContext, [id]);
            logger.LogTrace("[GitWindowViewModelExtensions] HideNotification invoked (id={Id}).", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to invoke HideNotification on {TypeName}.", dataContext.GetType().FullName);
        }
    }
}

/// <summary>
/// <para>Microsoft.TeamFoundation.Controls.NotificationType</para>
/// </summary>
public enum NotificationType
{
    Information,
    Warning,
    Error
}

/// <summary>
/// <para>Microsoft.TeamFoundation.Controls.NotificationFlags</para>
/// </summary>
[Flags]
public enum NotificationFlags
{
    None = 0,
    RequiresConfirmation = 1,
    NoTooltips = 2,
    All = 3
}
