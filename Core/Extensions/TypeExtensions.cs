using System.Reflection;

namespace Core.Extensions;

public static class TypeExtensions
{
    public static List<Type> GetAllDerivedTypes(this Type type)
    {
        var assembly = Assembly.GetAssembly(type);

        if (assembly is null)
            return Enumerable.Empty<Type>().ToList();

        return assembly.GetAllDerivedTypes(type);
    }

    public static List<Type> GetAllDerivedTypes(this Assembly assembly, Type type)
        => assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(type) && !t.IsAbstract)
            .ToList();
}
