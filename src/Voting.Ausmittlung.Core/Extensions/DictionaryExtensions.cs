// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace System.Collections.Generic;

public static class DictionaryExtensions
{
    /// <summary>
    /// Retrieves a value from a dictionary. If the value is not present, compute it, add it to the dictionary and return it.
    /// </summary>
    /// <param name="dict">The dictionary.</param>
    /// <param name="key">The key to retrieve.</param>
    /// <param name="newProvider">The function to compute new values.</param>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <returns>The existing or added value.</returns>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> newProvider)
        where TKey : notnull
        => dict.TryGetValue(key, out var item)
            ? item
            : dict[key] = newProvider();

    /// <summary>
    /// Retrieves a value from a dictionary.
    /// If the value is present, update it.
    /// If the value is not present, compute it and add it to the dictionary.
    /// </summary>
    /// <param name="dict">The dictionary.</param>
    /// <param name="key">The key to retrieve.</param>
    /// <param name="newProvider">The function to compute new values.</param>
    /// <param name="update">The function to update existing values.</param>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <returns>The updated or added value.</returns>
    public static TValue AddOrUpdate<TKey, TValue>(
        this IDictionary<TKey, TValue> dict,
        TKey key,
        Func<TValue> newProvider,
        Func<TValue, TValue> update)
        where TKey : notnull
    {
        return dict.TryGetValue(key, out var value)
            ? dict[key] = update(value)
            : dict[key] = newProvider();
    }
}
