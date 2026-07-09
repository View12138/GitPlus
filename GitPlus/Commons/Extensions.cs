using Microsoft.VisualStudio.Shell;

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

}
