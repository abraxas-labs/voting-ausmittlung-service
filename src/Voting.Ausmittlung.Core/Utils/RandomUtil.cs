// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

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
}
