using System.Reflection;

namespace Core.Extensions;

public static class PropertyInfoExtensions
{
    public static string GetPropertyFullName(this PropertyInfo p)
        => $"{p.Name} {p.GetPropertyShortType()}";


    public static string GetPropertyShortType(this PropertyInfo p)
        => $"[{p.PropertyType.ToString().Split('.')[^1].Trim('[', ']')}]";
}
