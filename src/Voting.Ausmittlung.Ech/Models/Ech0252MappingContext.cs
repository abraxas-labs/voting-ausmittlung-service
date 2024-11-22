﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Models;

public class Ech0252MappingContext
{
    private readonly List<DomainOfInfluence> _domainOfInfluences;

    public Ech0252MappingContext(List<DomainOfInfluence>? domainOfInfluences = null)
    {
        _domainOfInfluences = domainOfInfluences ?? new();
    }

    public DomainOfInfluence? GetSuperiorAuthority(Guid doiId)
    {
        var doi = _domainOfInfluences.FirstOrDefault(x => x.Id == doiId);

        if (doi == null)
        {
            return null;
        }

        return doi.SuperiorAuthorityDomainOfInfluenceId == null
            ? doi
            : doi.SuperiorAuthorityDomainOfInfluence ?? throw new ArgumentException($"{nameof(doi.SuperiorAuthorityDomainOfInfluence)} must not be null");
    }
}
