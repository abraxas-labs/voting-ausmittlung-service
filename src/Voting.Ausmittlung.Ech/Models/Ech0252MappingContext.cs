// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

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

    public DomainOfInfluence? GetSuperiorAuthority(string bfs)
    {
        return _domainOfInfluences
            .Where(d => d.Bfs == bfs)
            .OrderBy(d => d.Type)
            .FirstOrDefault(d => d.Type.IsPolitical());
    }
}
