// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Core.Utils;

public static class CombinationsUtil
{
    /// <summary>
    /// Generates all combinations (which match the optional validCombinationFn).
    /// Ex: [[[1,0],[0,1]], [[3,2], [2,3]]] = [[[1,0],[3,2]], [[1,0],[2,3]], [[0,1],[3,2]], [[0,1],[2,3]]].
    /// </summary>
    /// <typeparam name="T">Generic type.</typeparam>
    /// <param name="items">Input items.</param>
    /// <param name="validCombinationFn">Valid combination filter function.</param>
    /// <returns>All (valid) combinations.</returns>
    public static IReadOnlyCollection<IReadOnlyCollection<T>> GenerateCombinations<T>(
        IReadOnlyCollection<IReadOnlyCollection<T>> items,
        Func<T[], bool>? validCombinationFn = null)
    {
        var combinations = new List<List<T>>();
        GenerateCombinationsRecursive(items, combinations, new T[items.Count], 0, validCombinationFn);
        return combinations;
    }

    private static void GenerateCombinationsRecursive<T>(
        IReadOnlyCollection<IReadOnlyCollection<T>> items,
        List<List<T>> combinations,
        T[] currentCombination,
        int level,
        Func<T[], bool>? validCombinationFn)
    {
        if (level == items.Count)
        {
            if (validCombinationFn != null && !validCombinationFn.Invoke(currentCombination))
            {
                return;
            }

            combinations.Add(currentCombination.ToList());
            return;
        }

        foreach (var item in items.ElementAt(level))
        {
            currentCombination[level] = item;
            GenerateCombinationsRecursive(items, combinations, currentCombination, level + 1, validCombinationFn);
        }
    }
}
