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

    public static Dictionary<TKey, TValue> Shuffle<TKey, TValue>(
      this Dictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        Random r = new();

        return dictionary.OrderBy(x => r.Next())
           .ToDictionary(item => item.Key, item => item.Value);
    }
}
