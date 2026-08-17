using System.Collections;
using System.Runtime.CompilerServices;

namespace Reticleave.Helpers;

public static class StandardExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(this T[] array, int indexA, int indexB)
    {
        (array[indexA], array[indexB]) = (array[indexB], array[indexA]);
    }

    public static void SwapWithLast<T>(this T[] array, int indexA) => array.Swap(indexA, array.Length - 1);

    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
        where TKey : notnull
    {
        return source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static bool TryGetFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate, out T? value)
    {
        foreach (var item in source)
            if (predicate(item))
            {
                value = item;
                return true;
            }

        value = default;
        return false;
    }

    public static bool TryGetNonEnumeratedCount<T>(this IEnumerable<T> items, out int count)
    {
        (bool result, count) = items switch
        {
            IReadOnlyCollection<T> readOnlyCollection => (true, readOnlyCollection.Count),
            ICollection<T> collection => (true, collection.Count),
            ICollection nonGenericCollection => (true, nonGenericCollection.Count),
            _ => (false, 0)
        };
        return result;
    }

    public static bool ZipAndCompare<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> comparer)
    {
        if (first.TryGetNonEnumeratedCount(out var firstCount) && second.TryGetNonEnumeratedCount(out var secondCount))
        {
            if (firstCount != secondCount)
                return false;
        }

        using var e1 = first.GetEnumerator();
        using var e2 = second.GetEnumerator();

        while (e1.MoveNext())
        {
            if (!e2.MoveNext()) // If first has more elements than second, return false
                return false;

            if (!comparer(e1.Current, e2.Current))
                return false;
        }
        if (e2.MoveNext()) // If second has more elements than first, return false
            return false;

        return true;
    }
}
