// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Report.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// Sums an IEnumerable of nullable integers.
    /// </summary>
    /// <param name="enumerable">The enumerable to sum.</param>
    /// <param name="selector">The selector.</param>
    /// <typeparam name="T">Generic type.</typeparam>
    /// <returns>Null if all values are null, the sum of integers otherwise.</returns>
    public static int? SumNullable<T>(this IEnumerable<T> enumerable, Func<T, int?> selector)
    {
        int? result = null;
        foreach (var element in enumerable)
        {
            var i = selector(element);
            if (!i.HasValue)
            {
                continue;
            }

            result ??= 0;
            result += i.Value;
        }

        return result;
    }
}
