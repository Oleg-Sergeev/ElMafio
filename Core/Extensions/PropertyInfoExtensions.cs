using System.ComponentModel;
using System.Reflection;

namespace Core.Extensions;

public static class PropertyInfoExtensions
{
    public static string GetFullName(this PropertyInfo p)
        => $"{p.Name} {p.GetShortTypeName()}";


    public static string GetShortTypeName(this PropertyInfo p)
        => $"[{p.PropertyType.ToString().Split('.')[^1].Trim('[', ']')}]";


    public static string GetDisplayNameOrFullName(this PropertyInfo prop)
    {
        var displayNameAttribute = prop.GetCustomAttribute<DisplayNameAttribute>();

        return displayNameAttribute is not null
        ? $"{displayNameAttribute.DisplayName} {prop.GetShortTypeName()}"
        : prop.GetFullName();
    }
}
