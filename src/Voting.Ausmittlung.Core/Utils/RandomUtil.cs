// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Voting.Ausmittlung.Core.Utils;

public static class RandomUtil
{
    /// <summary>
    /// Returns a probe of size <see cref="samples"/> from the enumerable <see cref="elements"/>.
    /// Copied from https://stackoverflow.com/a/10739419/3302887.
    /// Not a perfect solution in terms of performance/computational complexity, but easy readable code.
    /// </summary>
    /// <param name="elements">The source of elements.</param>
    /// <param name="samples">The count of samples.</param>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <returns>An enumerable of the samples picked.</returns>
    public static IEnumerable<T> Samples<T>(IEnumerable<T> elements, int samples)
    {
        return elements
            .OrderBy(x => Guid.NewGuid())
            .Take(samples);
    }

    /// <summary>
    /// Returns a random string.
    /// Copied from https://stackoverflow.com/a/1344255.
    /// </summary>
    /// <param name="size">The size of the generated string.</param>
    /// <param name="chars">The possible chars of the generated string.</param>
    /// <returns>A random string.</returns>
    public static string GetRandomString(int size, char[] chars)
    {
        var data = new byte[sizeof(int) * size];
        using (var crypto = RandomNumberGenerator.Create())
        {
            crypto.GetBytes(data);
        }

        var result = new StringBuilder(size);
        for (var i = 0; i < size; i++)
        {
            var rnd = BitConverter.ToUInt32(data, i * sizeof(int));
            var idx = rnd % chars.Length;

            result.Append(chars[idx]);
        }

        return result.ToString();
    }
}
