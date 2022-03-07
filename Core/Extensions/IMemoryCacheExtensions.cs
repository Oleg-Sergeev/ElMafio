using System.Reflection;
using Microsoft.Extensions.Caching.Memory;

namespace Core.Extensions;

public static class IMemoryCacheExtensions
{
    public static void Clear(this IMemoryCache cache)
    {
        if (cache is MemoryCache memCache)
        {
            memCache.Compact(100);

            return;
        }

        MethodInfo? clearMethod = cache
            .GetType()
            .GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);

        if (clearMethod is not null)
        {
            clearMethod.Invoke(cache, null);

            return;
        }

        PropertyInfo? prop = cache
            .GetType()
            .GetProperty("EntriesCollection", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
        
        if (prop is not null)
        {
            object? innerCache = prop.GetValue(cache);

            if (innerCache is not null)
            {
                clearMethod = innerCache.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);

                if (clearMethod is not null)
                {
                    clearMethod.Invoke(innerCache, null);

                    return;
                }
            }
        }

        throw new InvalidOperationException($"Unable to clear memory cache instance of type {cache.GetType().FullName}");
    }
}
