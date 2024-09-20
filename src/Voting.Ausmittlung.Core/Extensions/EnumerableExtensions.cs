// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace System.Linq;

public static class EnumerableExtensions
{
    /// <summary>
    /// Returns a collection of <see cref="IEnumerable{T}"/> containing the elements with the maximum value in a generic sequence according to the specified key selector function.
    /// Since <see cref="Enumerable.MaxBy"/> only forsees one return value, this function is still required.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the source sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key to compare elements by.</typeparam>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A function to extract the key for each element.</param>
    /// <returns>The <see cref="IEnumerable{T}"/> collection of elements in the sequence with the maximum value in the sequence.</returns>
    public static IEnumerable<T> MaxsBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> selector)
    {
        var max = source.Max(selector);
        return source.Where(x => EqualityComparer<TKey>.Default.Equals(selector(x), max));
    }

    public static IEnumerable<TSource> Flatten<TSource>(this IEnumerable<TSource> enumerable, Func<TSource, IEnumerable<TSource>> childrenSelector)
    {
        return enumerable.SelectMany(c => childrenSelector(c).Flatten(childrenSelector)).Concat(enumerable);
    }

    /// <summary>
    /// Iterates through an enumerable and calls an action with each item and the matching item from the second enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable to enumerate.</param>
    /// <param name="keyExtractor">The key extractor to extract the property for the comparison.</param>
    /// <param name="enumerable2">The second enumerable to find matching items.</param>
    /// <param name="keyExtractor2">The key extractor to extract the property from the second item for the comparison.</param>
    /// <param name="action">The action to be called if a matching item is found in the second enumerable.</param>
    /// <typeparam name="TSource1">The source type of the first enumerable.</typeparam>
    /// <typeparam name="TSource2">The source type of the second enumerable.</typeparam>
    /// <typeparam name="TKey">The type of the key to compare the items.</typeparam>
    public static void MatchAndExec<TSource1, TSource2, TKey>(
        this IEnumerable<TSource1> enumerable,
        Func<TSource1, TKey> keyExtractor,
        IEnumerable<TSource2> enumerable2,
        Func<TSource2, TKey> keyExtractor2,
        Action<TSource1, TSource2> action)
        where TKey : notnull
    {
        var secondByKey = enumerable2.ToDictionary(keyExtractor2);
        foreach (var item in enumerable)
        {
            if (secondByKey.TryGetValue(keyExtractor(item), out var item2))
            {
                action(item, item2);
            }
        }
    }

    /// <summary>
    /// Iterates through an enumerable and calls an action with each item and the matching item from the second enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable to enumerate.</param>
    /// <param name="enumerable2">The second enumerable to find matching items.</param>
    /// <param name="keyExtractor">The key extractor to extract the property for the comparison.</param>
    /// <param name="action">The action to be called if a matching item is found in the second enumerable.</param>
    /// <typeparam name="T">The source type of the items.</typeparam>
    /// <typeparam name="TKey">The type of the key to compare the items.</typeparam>
    public static void MatchAndExec<T, TKey>(
        this IEnumerable<T> enumerable,
        IEnumerable<T> enumerable2,
        Func<T, TKey> keyExtractor,
        Action<T, T> action)
        where TKey : notnull
    {
        var secondByKey = enumerable2.ToDictionary(keyExtractor);
        foreach (var item in enumerable)
        {
            if (secondByKey.TryGetValue(keyExtractor(item), out var item2))
            {
                action(item, item2);
            }
        }
    }
}
