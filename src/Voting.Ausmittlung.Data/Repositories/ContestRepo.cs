// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class ContestRepo : DbRepository<DataContext, Contest>
{
    public ContestRepo(DataContext context)
        : base(context)
    {
    }

    public Task<List<Contest>> GetContestsInTestingPhase()
    {
        return Query().WhereInTestingPhase().ToListAsync();
    }

    public Task<Contest?> GetWithValidationContextData(Guid contestId)
    {
        return Query()
            .AsSplitQuery()
            .Include(x => x.DomainOfInfluence.CountingCircles)
                .ThenInclude(x => x.CountingCircle)
            .Include(x => x.DomainOfInfluence.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonVoterParticipationConfigurations)
            .Include(x => x.DomainOfInfluence.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonCountOfVotersConfigurations)
            .Include(x => x.DomainOfInfluence.PlausibilisationConfiguration)
                .ThenInclude(x => x!.ComparisonVotingChannelConfigurations.OrderBy(y => y.VotingChannel))
            .FirstOrDefaultAsync(x => x.Id == contestId);
    }
}
