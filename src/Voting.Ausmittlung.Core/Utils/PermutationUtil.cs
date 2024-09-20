// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Core.Utils;

public static class PermutationUtil
{
    /// <summary>
    /// Generates unique permutations of the input items.
    /// Ex: [1,1,0] => [[1,1,0], [1,0,1], [0,1,1]].
    /// </summary>
    /// <typeparam name="T">Generic type param.</typeparam>
    /// <param name="items">The items.</param>
    /// <returns>Unique permutations of the input.</returns>
    public static IReadOnlyCollection<IReadOnlyCollection<T>> GenerateUniquePermutations<T>(IReadOnlyCollection<T> items)
        where T : notnull
    {
        var result = new List<List<T>>();
        var permutation = new List<T>();
        var countByItem = items
            .GroupBy(i => i)
            .ToDictionary(x => x.Key, x => x.Count());

        GenerateUniquePermutationDfs(items, result, permutation, countByItem);
        return result;
    }

    private static void GenerateUniquePermutationDfs<T>(
        IReadOnlyCollection<T> items,
        List<List<T>> result,
        List<T> permutation,
        Dictionary<T, int> countByItem)
        where T : notnull
    {
        if (permutation.Count == items.Count)
        {
            result.Add(permutation.ToList());
            return;
        }

        foreach (var item in countByItem.Keys.Where(num => countByItem[num] > 0))
        {
            permutation.Add(item);
            countByItem[item]--;

            GenerateUniquePermutationDfs(items, result, permutation, countByItem);

            countByItem[item]++;
            permutation.RemoveAt(permutation.Count - 1);
        }
    }
}
