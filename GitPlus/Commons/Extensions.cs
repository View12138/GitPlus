using Microsoft.VisualStudio.Shell;
using System.Collections;

namespace GitPlus.Commons;

public static class Extensions
{
    #region ServiceProvider
    private static IServiceProvider? serviceProvider;
    public static void BuildServiceProvider(IServiceCollection services) => serviceProvider = services.BuildServiceProvider();
    public static TService? GetService<TService>() where TService : class
    {
        return serviceProvider?.GetService<TService>();
    }

    public static object? GetService(Type serviceType)
    {
        return serviceProvider?.GetService(serviceType);
    }

    public static TService GetRequiredService<TService>() where TService : class
    {
        if (serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider is not initialized. Call BuildServiceProvider() first.");
        }
        return serviceProvider.GetRequiredService<TService>();
    }

    public static object GetRequiredService(Type serviceType)
    {
        if (serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider is not initialized. Call BuildServiceProvider() first.");
        }
        return serviceProvider.GetRequiredService(serviceType);
    }
    #endregion

    #region DependencyObject
    public static void CopyLocalValuesFrom(this DependencyObject target, DependencyObject source)
    {
        var logger = GetRequiredService<ILogger>();
        logger.LogTrace("[Extensions] enter '{method}'", nameof(CopyLocalValuesFrom));
        var enumerator = source.GetLocalValueEnumerator();
        while (enumerator.MoveNext())
        {
            try
            {
                var entry = enumerator.Current;
                if (entry.Property.ReadOnly || entry.Property.Name == "Name")
                    continue;
                if (entry.Value is Expression)
                    continue;
                target.SetValue(entry.Property, entry.Value);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to copy a property value in CopyLocalValuesFrom.");
            }
        }
        if (target is FrameworkElement targetElement && source is FrameworkElement sourceElement)
        {
            targetElement.DataContext = sourceElement.DataContext;
            targetElement.Style = sourceElement.Style;
            targetElement.Resources = sourceElement.Resources;
        }
        logger.LogTrace("[Extensions] exit '{method}'", nameof(CopyLocalValuesFrom));
    }

    public static async Task<FrameworkElement?> FindChildAsync(this DependencyObject parent, string childName, bool recursive = true, CancellationToken cancellationToken = default)
    {
        if (parent == null)
            return null;

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index) as FrameworkElement;
            if (child != null)
            {
                if (child.Name == childName)
                {
                    return child;
                }
                if (recursive)
                {
                    child = await child.FindChildAsync(childName, recursive, cancellationToken);
                    if (child != null)
                    {
                        return child;
                    }
                }
            }
        }

        return null;
    }

    public static async Task<TUIElement?> FindChildAsync<TUIElement>(this DependencyObject parent, bool recursive = true, CancellationToken cancellationToken = default)
        where TUIElement : FrameworkElement
    {
        if (parent == null)
            return null;

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index) as FrameworkElement;
            if (child != null)
            {

                if (child.GetType() == typeof(TUIElement))
                {
                    return (TUIElement)child;
                }
                if (recursive)
                {
                    child = await child.FindChildAsync<TUIElement>(recursive, cancellationToken);
                    if (child != null)
                    {
                        return (TUIElement)child;
                    }
                }
            }
        }

        return null;
    }

    public static async Task<(int Index, FrameworkElement Element)?> GetChildIndexAsync(this FrameworkElement parent, string elementName, CancellationToken cancellationToken = default)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var element = await parent.FindChildAsync(elementName, false, cancellationToken);
        if (element is not null)
        {
            var index = -1;
            if (parent is ToolBarTray tray && element is ToolBar toolBar)
            {
                index = tray.ToolBars.IndexOf(toolBar);
            }
            if (parent is ItemsControl itemsControl)
            {
                index = itemsControl.Items.IndexOf(element);
            }
            if (parent is Panel panel)
            {
                index = panel.Children.IndexOf(element);
            }
            if (index >= 0)
            {
                return (index, element);
            }
        }
        return null;
    }

    public static async Task<bool> InsertElementAsync(this FrameworkElement parent, FrameworkElement element, int? index = null, CancellationToken cancellationToken = default)
    {
        var logger = GetRequiredService<ILogger>();
        logger.LogTrace("[Extensions] enter '{method}', name='{Name}', index={Index}", nameof(InsertElementAsync), element.Name, index?.ToString() ?? "end");
        var existingElement = await parent.FindChildAsync(element.Name, cancellationToken: cancellationToken);
        if (existingElement != null)
        {
            logger.LogDebug("[Extensions] element '{Name}' already present — skipping insert.", element.Name);
            return true;
        }

        var result = false;
        if (parent is ToolBarTray tray && element is ToolBar toolBar)
        {
            if (index.HasValue)
                tray.ToolBars.Insert(index.Value, toolBar);
            else
                tray.ToolBars.Add(toolBar);
            result = true;
        }
        else if (parent is ItemsControl itemsControl)
        {
            if (index.HasValue)
                itemsControl.Items.Insert(index.Value, element);
            else
                itemsControl.Items.Add(element);
            result = true;
        }
        else if (parent is Panel panel)
        {
            if (index.HasValue)
                panel.Children.Insert(index.Value, element);
            else
                panel.Children.Add(element);
            result = true;
        }
        else
        {
            logger.LogWarning("InsertElementAsync: unsupported parent type '{ParentType}' for '{Name}'.", parent.GetType().Name, element.Name);
        }

        logger.LogTrace("[Extensions] exit '{method}', result={Result}", nameof(InsertElementAsync), result);
        return result;
    }

    public static void RemoveElement(this FrameworkElement parent, FrameworkElement element)
    {
        var logger = GetRequiredService<ILogger>();
        logger.LogTrace("[Extensions] enter '{method}', name='{Name}'", nameof(RemoveElement), element.Name);
        if (parent is ToolBarTray tray && element is ToolBar toolBar)
        {
            tray.ToolBars.Remove(toolBar);
        }
        else if (parent is ItemsControl ic)
            ic.Items.Remove(element);
        else if (parent is Panel panel)
            panel.Children.Remove(element);
        else
            logger.LogWarning("RemoveElement: unsupported parent type '{ParentType}'.", parent.GetType().Name);
        logger.LogTrace("[Extensions] exit '{method}'", nameof(RemoveElement));
    }
    #endregion

    #region Resources
    public static Uri GetResourceUri(this string resourceName, ResourceFolders folder, string assembly = "GitPlus")
        => new($"pack://application:,,,/{assembly};component/{folder}/{resourceName}", UriKind.Absolute);
    public static Uri GetResourceUri(this string resourceName, string folder, string assembly = "GitPlus")
        => new($"pack://application:,,,/{assembly};component/{folder}/{resourceName}", UriKind.Absolute);

    extension(System.Windows.Resources.ContentTypes)
    {
        public static string ApplicationJson => "application/json";
    }


    private readonly static Dictionary<ResourceDictionary, Dictionary<string, object>> _cache = [];
    extension(Application application)
    {
        public static System.Windows.Resources.StreamResourceInfo? GetResourceStream(string defaultResourceName, ResourceFolders folder, System.Globalization.CultureInfo cultureInfo)
        {
            var logger = GetRequiredService<ILogger>();
            try
            {
                logger.LogTrace("[Extensions] enter '{method}', defaultResourceName='{defaultResourceName}', folder='{folder}', cultureInfo='{cultureInfo}'", nameof(GetResourceStream), defaultResourceName, folder, cultureInfo);
                var culture = cultureInfo;
                while (culture != System.Globalization.CultureInfo.InvariantCulture)
                {
                    var index = defaultResourceName.LastIndexOf('.');
                    var resourceName = index <= 0 ? defaultResourceName : defaultResourceName.Insert(index, $".{culture.Name}");
                    System.Windows.Resources.StreamResourceInfo? stream = null;
                    try
                    {
                        stream = Application.GetResourceStream(resourceName.GetResourceUri(folder));
                    }
                    catch (IOException)
                    {
                        logger.LogDebug("[Extensions] culture resource '{}' not found.", resourceName);
                    }
                    if (stream != null)
                    {
                        logger.LogDebug("[Extensions] use culture resource '{}'.", resourceName);
                        return stream;
                    }
                    culture = culture.Parent;
                }

                logger.LogDebug("[Extensions] use default resource '{}'.", defaultResourceName);
                try
                {
                    return Application.GetResourceStream(defaultResourceName.GetResourceUri(folder));
                }
                catch (IOException)
                {
                    logger.LogDebug("[Extensions] default resource '{}' not found.", defaultResourceName);
                    return null;
                }
            }
            finally
            {
                logger.LogTrace("[Extensions] exit '{method}'", nameof(RemoveElement));
            }
        }

        public object? GetResource(string resourceKey)
        {
            var resources = EnumerateAllResources(application.Resources);
            resources.TryGetValue(resourceKey, out var resourceValue);
            return resourceValue;
        }

        /// <summary>
        /// 递归遍历 ResourceDictionary 及其所有 MergedDictionaries，
        /// 返回所有资源的 Key-Value 对（扁平化）
        /// </summary>
        private static Dictionary<string, object> EnumerateAllResources(ResourceDictionary resource)
        {
            if (_cache.TryGetValue(resource, out var cachedResources))
            {
                return cachedResources;
            }
            var visited = new Dictionary<string, object>();
            foreach (DictionaryEntry item in resource)
            {
                if (!visited.ContainsKey(item.Key.ToString()))
                {
                    visited.Add(item.Key.ToString(), item.Value);
                }
            }

            foreach (var merged in resource.MergedDictionaries)
            {
                foreach (var item in EnumerateAllResources(merged))
                {
                    if (!visited.ContainsKey(item.Key))
                    {
                        visited.Add(item.Key, item.Value);
                    }
                }
            }
            _cache.Add(resource, visited);
            return visited;
        }
    }
    #endregion

    #region string
    public static string ReplaceFirst(this string text, string oldValue, string newValue)
    {
        int index = text.IndexOf(oldValue, StringComparison.Ordinal);
        if (index < 0)
            return text;

        return text.Substring(0, index) + newValue + text.Substring(index + oldValue.Length);
    }
    #endregion
}

public enum ResourceFolders
{
    Resources,
    Assets,
}


internal static class Hash
{
    /// <summary>
    /// This is how VB Anonymous Types combine hash values for fields.
    /// </summary>
    internal static int Combine(int newKey, int currentKey) => unchecked((currentKey * (int)0xA5555529) + newKey);
}