// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Ech0155_5_0;
using Voting.Ausmittlung.Data.Models;
using DomainOfInfluenceType = Ech0155_5_0.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class DomainOfInfluenceMapping
{
    internal static DomainOfInfluenceType ToEchDomainOfInfluence(this DomainOfInfluence domainOfInfluence)
    {
        return new DomainOfInfluenceType
        {
            DomainOfInfluenceIdentification = domainOfInfluence.BasisDomainOfInfluenceId.ToString(),
            DomainOfInfluenceName = domainOfInfluence.Name,
            DomainOfInfluenceShortname = domainOfInfluence.ShortName.Truncate(5),
            DomainOfInfluenceTypeProperty = Enum.Parse<DomainOfInfluenceTypeType>(domainOfInfluence.Type.ToString()),
        };
    }
}
