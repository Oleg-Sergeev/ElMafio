namespace Core.Extensions;

public static class DictionaryExtensions
{
    public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> pairs)
    {
        foreach (var pair in pairs)
            dict.Add(pair.Key, pair.Value);
    }

    public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dict, IDictionary<TKey, TValue> otherDict)
    {
        foreach (var item in otherDict)
            dict.Add(item.Key, item.Value);
    }
}
