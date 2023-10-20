// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class ContestCountingCircleDetailsRepo : DbRepository<DataContext, ContestCountingCircleDetails>
{
    public ContestCountingCircleDetailsRepo(DataContext context)
        : base(context)
    {
    }

    public Task<ContestCountingCircleDetails?> GetWithRelatedEntities(Guid id)
    {
        return Query()
            .AsSplitQuery()
            .Include(x => x.Contest)
            .Include(x => x.CountingCircle)
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<ContestCountingCircleDetails?> GetWithResults(Guid id)
    {
        return Query()
            .AsSplitQuery()
            .Include(x => x.Contest)
            .Include(x => x.CountingCircle)
                .ThenInclude(x => x.VoteResults)
                    .ThenInclude(x => x.Vote.DomainOfInfluence)
            .Include(x => x.CountingCircle)
                .ThenInclude(x => x.VoteResults)
                    .ThenInclude(x => x.Results)
            .Include(x => x.CountingCircle)
                .ThenInclude(x => x.ProportionalElectionResults)
                    .ThenInclude(x => x.ProportionalElection.DomainOfInfluence)
            .Include(x => x.CountingCircle)
                .ThenInclude(x => x.MajorityElectionResults)
                    .ThenInclude(x => x.MajorityElection.DomainOfInfluence)
            .Include(x => x.CountingCircle.ResponsibleAuthority)
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
