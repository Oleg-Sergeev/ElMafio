using System.Reflection;

namespace Core.Extensions;

public static class PropertyInfoExtensions
{
    public static string GetPropertyFullName(this PropertyInfo p)
        => $"{p.Name} [{p.PropertyType.ToString().Split('.')[^1].Trim('[', ']')}]";
}
