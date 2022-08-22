// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Queries;

public static class PoliticalBusinessQueryExtensions
{
    public static IOrderedQueryable<T> OrderByNaturalOrder<T>(this IQueryable<T> query)
        where T : PoliticalBusiness
        => query.OrderBy(x => x.PoliticalBusinessNumber);
}
