// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0155_4_0;
using Voting.Ausmittlung.Data.Models;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class CountingCircleMapping
{
    internal static CountingCircleType ToEchCountingCircle(this CountingCircle countingCircle)
    {
        return new CountingCircleType
        {
            CountingCircleId = countingCircle.BasisCountingCircleId.ToString(),
            CountingCircleName = countingCircle.Name,
        };
    }

    internal static Ech0252_2_0.CountingCircleType ToEch0252CountingCircle(this CountingCircle countingCircle, Guid contestDomainOfInfluenceId)
    {
        var doiType = GetDomainOfInfluenceType(countingCircle, contestDomainOfInfluenceId);

        return new Ech0252_2_0.CountingCircleType
        {
            CountingCircleId = countingCircle.Bfs,
            CountingCircleName = countingCircle.Name,
            DomainOfInfluenceType = doiType?.ToEchDomainOfInfluenceType(),
        };
    }

    private static DomainOfInfluenceType? GetDomainOfInfluenceType(CountingCircle countingCircle, Guid contestDomainOfInfluenceId)
    {
        var domainOfInfluences = countingCircle.DomainOfInfluences
            .Select(doiCc => doiCc.DomainOfInfluence)
            .WhereNotNull()
            .ToList();

        return domainOfInfluences.Any(doi => doi.Type == DomainOfInfluenceType.Sk)
            ? DomainOfInfluenceType.Sk
            : DomainOfInfluenceType.Mu;
    }
}
