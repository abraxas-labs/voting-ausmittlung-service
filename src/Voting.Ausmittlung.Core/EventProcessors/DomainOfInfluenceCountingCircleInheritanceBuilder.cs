// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class DomainOfInfluenceCountingCircleInheritanceBuilder
{
    private readonly DomainOfInfluenceCountingCircleRepo _doiCountingCirclesRepo;
    private readonly DomainOfInfluenceRepo _doiRepo;

    public DomainOfInfluenceCountingCircleInheritanceBuilder(
        DomainOfInfluenceCountingCircleRepo doiCountingCirclesRepo,
        DomainOfInfluenceRepo doiRepo)
    {
        _doiCountingCirclesRepo = doiCountingCirclesRepo;
        _doiRepo = doiRepo;
    }

    internal async Task BuildInheritanceForCountingCircles(
        Guid doiId,
        List<Guid> hierarchicalGreaterOrSelfDoiIds,
        List<Guid> countingCircleIdsToAdd,
        List<Guid> countingCircleIdsToRemove)
    {
        var newEntries = BuildDomainOfInfluenceCountingCircleEntries(doiId, hierarchicalGreaterOrSelfDoiIds, countingCircleIdsToAdd);
        await _doiCountingCirclesRepo.RemoveAll(hierarchicalGreaterOrSelfDoiIds, countingCircleIdsToRemove);
        await _doiCountingCirclesRepo.AddRange(newEntries);
    }

    internal Task<List<Guid>> GetHierarchicalGreaterOrSelfDomainOfInfluenceIds(Guid domainOfInfluenceId)
    {
        return _doiRepo.GetHierarchicalGreaterOrSelfDomainOfInfluenceIds(domainOfInfluenceId);
    }

    private IEnumerable<DomainOfInfluenceCountingCircle> BuildDomainOfInfluenceCountingCircleEntries(
        Guid currentDoiId,
        IEnumerable<Guid> doiIds,
        IReadOnlyCollection<Guid> ccIds)
    {
        return doiIds.SelectMany(doiId =>
            ccIds.Select(ccId => new DomainOfInfluenceCountingCircle
            {
                CountingCircleId = ccId,
                DomainOfInfluenceId = doiId,
                Inherited = doiId != currentDoiId,
            }));
    }
}
