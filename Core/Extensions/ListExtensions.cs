using System.Collections.Generic;

namespace Core.Extensions;

public static class ListExtensions
{
    public static List<T> Shuffle<T>(this List<T> list, int iterations = 1)
    {
        if (iterations < 0)
            throw new ArgumentOutOfRangeException(nameof(iterations), "the value must be at least zero");

        var random = new Random();

        for (int i = 0; i < iterations; i++)
        {
            int n = list.Count;

            while (n > 1)
            {
                n--;

                int k = random.Next(n + 1);

                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        return list;
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        => source.ShuffleIterator();

    private static IEnumerable<T> ShuffleIterator<T>(this IEnumerable<T> source)
    {
        var rnd = new Random();

        var buffer = source.ToArray();

        for (int i = 0; i < buffer.Length; i++)
        {
            int j = rnd.Next(i, buffer.Length);
            yield return buffer[j];

            buffer[j] = buffer[i];
        }
    }
}