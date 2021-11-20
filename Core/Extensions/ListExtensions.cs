namespace Core.Extensions;

public static class ListExtensions
{
    public static IList<T> Shuffle<T>(this IList<T> list, int iterations = 1)
    {
        var random = new Random();

        for (int i = 0; i < iterations; i++)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;

                int k = random.Next(n + 1);

                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        return list;
    }


    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        return source.Shuffle(new Random());
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));

        return source.ShuffleIterator(rng);
    }

    private static IEnumerable<T> ShuffleIterator<T>(this IEnumerable<T> source, Random rng)
    {
        var buffer = source.ToList();
        for (int i = 0; i < buffer.Count; i++)
        {
            int j = rng.Next(i, buffer.Count);
            yield return buffer[j];

            buffer[j] = buffer[i];
        }
    }
}