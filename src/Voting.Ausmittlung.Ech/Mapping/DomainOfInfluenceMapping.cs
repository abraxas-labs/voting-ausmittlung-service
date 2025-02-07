// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Ech0155_5_1;
using Voting.Ausmittlung.Data.Models;
using DomainOfInfluenceType = Ech0155_5_1.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class DomainOfInfluenceMapping
{
    private const string DomainOfInfluenceIdPlaceholder = "?";

    internal static DomainOfInfluenceType ToEchDomainOfInfluence(this DomainOfInfluence domainOfInfluence)
    {
        return new DomainOfInfluenceType
        {
            DomainOfInfluenceIdentification = !string.IsNullOrEmpty(domainOfInfluence.Bfs) ? domainOfInfluence.Bfs : DomainOfInfluenceIdPlaceholder,
            DomainOfInfluenceName = domainOfInfluence.Name,
            DomainOfInfluenceShortname = !string.IsNullOrEmpty(domainOfInfluence.ShortName) ? domainOfInfluence.ShortName.Truncate(5) : null,
            DomainOfInfluenceTypeProperty = ToEchDomainOfInfluenceType(domainOfInfluence.Type),
        };
    }

    internal static DomainOfInfluenceTypeType ToEchDomainOfInfluenceType(this Data.Models.DomainOfInfluenceType domainOfInfluenceType)
        => Enum.Parse<DomainOfInfluenceTypeType>(domainOfInfluenceType.ToString());
}
