// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

internal static class ProtocolSortUtil
{
    public static IEnumerable<T> OrderByDomainOfInfluence<T>(
        this IEnumerable<T> items,
        Func<T, DomainOfInfluence> doiSelector,
        DomainOfInfluenceCantonDefaults cantonDefaults)
    {
        return cantonDefaults.ProtocolDomainOfInfluenceSortType switch
        {
            ProtocolDomainOfInfluenceSortType.SortNumber => items.OrderBy(i => doiSelector(i).SortNumber).ThenBy(i => doiSelector(i).Name),
            ProtocolDomainOfInfluenceSortType.Alphabetical => items.OrderBy(i => doiSelector(i).Name).ThenBy(i => doiSelector(i).SortNumber),
            _ => throw new InvalidOperationException($"Cannot sort because invalid {nameof(ProtocolDomainOfInfluenceSortType)}"),
        };
    }

    public static IEnumerable<T> OrderByCountingCircle<T>(
        this IEnumerable<T> items,
        Func<T, CountingCircle> ccSelector,
        DomainOfInfluenceCantonDefaults cantonDefaults)
    {
        return cantonDefaults.ProtocolCountingCircleSortType switch
        {
            ProtocolCountingCircleSortType.SortNumber => items.OrderBy(i => ccSelector(i).SortNumber).ThenBy(i => ccSelector(i).Name),
            ProtocolCountingCircleSortType.Alphabetical => items.OrderBy(i => ccSelector(i).Name).ThenBy(i => ccSelector(i).SortNumber),
            _ => throw new InvalidOperationException($"Cannot sort because invalid {nameof(ProtocolCountingCircleSortType)}"),
        };
    }
}
