using Microsoft.Extensions.Configuration;

namespace Core.Extensions;

public static class IConfigurationExtensions
{
    public static string GetConnectionStringProductionDb(this IConfiguration config)
        => config.GetConnectionString("SQLServer_Production");
    public static string GetConnectionStringDebugDb(this IConfiguration config)
        => config.GetConnectionString("SQLServer_Debug");


    public static string? GetTitle(this IConfiguration config)
        => config["Title"];

    public static string? GetDescription(this IConfiguration config)
        => config["Description"];
    public static string? GetImageUrl(this IConfiguration config)
        => config["ImageUrl"];


    public static (string, string)? GetEmbedFieldInfo(this IConfigurationSection section, string key = "Key", string value = "Value")
    {
        var fieldInfo = section.GetChildren().ToDictionary(k => k.Key);

        if (fieldInfo.Count == 0)
            return null;

        if (fieldInfo.TryGetValue(key, out var fieldKey) && fieldInfo.TryGetValue(value, out var fieldValue))
            return (fieldKey.Value, fieldValue.Value);

        return null;
    }


    public static IReadOnlyDictionary<string, string> GetSectionFields(this IConfiguration config, string sectionName)
    {
        var section = config.GetSection(sectionName);

        return section.GetSectionFields();
    }

    public static IReadOnlyDictionary<string, string> GetSectionFields(this IConfigurationSection section)
    {
        var fields = new Dictionary<string, string>();

        var fieldsSection = section.GetChildren().ToDictionary(k => k.Key);

        foreach (var field in fieldsSection)
        {
            fields.Add(field.Key, field.Value.Value);
        }

        return fields;
    }
}
