using System.Resources;

namespace GitPlus.Commons;

internal static class Localization
{
    public static string GetLocalizedString(string name)
    {
        string text = name;
        try
        {
            text = Assets.Languages.ResourceManager.GetString(name, Assets.Languages.Culture);
        }
        catch (MissingManifestResourceException ex)
        {
            var logger = Extensions.GetRequiredService<ILogger>();
            logger.LogError(ex, "ILocalizedAttribute: Missing resource for key '{Key}'", name);
        }
        return text;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
public sealed class LocalizedDisplayNameAttribute(string name) : DisplayNameAttribute
{
    public override string DisplayName => Localization.GetLocalizedString(name);
}

[AttributeUsage(AttributeTargets.All)]
public sealed class LocalizedDescriptionAttribute(string name) : DescriptionAttribute
{
    public override string Description => Localization.GetLocalizedString(name);
}

[AttributeUsage(AttributeTargets.All)]
public sealed class LocalizedCategoryAttribute(string name) : CategoryAttribute(name)
{
    protected override string GetLocalizedString(string value) => Localization.GetLocalizedString(value);
}