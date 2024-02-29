// (c) Copyright 2024 by Abraxas Informatik AG
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

public class DomainOfInfluencePartyRepo : DbRepository<DataContext, DomainOfInfluenceParty>
{
    public DomainOfInfluencePartyRepo(DataContext context)
        : base(context)
    {
    }

    public async Task<List<DomainOfInfluenceParty>> GetInTestingPhaseOrNoContestParties(Guid partyId)
    {
        return await Query()
            .Include(p => p.Translations)
            .Where(p => p.BaseDomainOfInfluencePartyId == partyId)
            .WhereContestIsInTestingPhaseOrNoContest()
            .ToListAsync();
    }
}
