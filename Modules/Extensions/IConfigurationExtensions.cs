using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Modules.Extensions;

public static class IConfigurationExtensions
{
    public static string? GetTitle(this IConfiguration config)
        => config["Title"];

    public static string? GetDescription(this IConfiguration config)
        => config["Description"];
    public static string? GetImageUrl(this IConfiguration config)
        => config["ImageUrl"];

    public static (string, string)? GetEmbedFieldInfo(this IConfiguration config, string sectionName, string key = "Key", string value = "Value")
    {
        var section = config.GetSection(sectionName);

        return section.GetEmbedFieldInfo(key, value);
    }
    public static (string, string)? GetEmbedFieldInfo(this IConfigurationSection section, string key = "Key", string value = "Value")
    {
        var fieldInfo = section.GetChildren().ToDictionary(k => k.Key);

        if (fieldInfo.Count == 0)
            return null;

        if (fieldInfo.TryGetValue(key, out var fieldKey) && fieldInfo.TryGetValue(value, out var fieldValue))
            return (fieldKey.Value, fieldValue.Value);

        return null;
    }
}
