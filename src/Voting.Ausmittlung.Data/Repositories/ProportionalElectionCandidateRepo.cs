// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionCandidateRepo : DbRepository<DataContext, ProportionalElectionCandidate>
{
    public ProportionalElectionCandidateRepo(DataContext context)
        : base(context)
    {
    }

    public Task<ProportionalElectionCandidate?> GetWithEndResultsAsTracked(Guid id)
    {
        return Set
            .AsTracking()
            .Include(x => x.EndResult)
            .Include(x => x.ProportionalElectionList.EndResult)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task UpdateReferencesForNewContest(Guid proportionalElectionId, Guid newContestId)
    {
        var candidates = await Set
            .Where(c => c.PartyId != null && c.ProportionalElectionList.ProportionalElectionId == proportionalElectionId)
            .Include(c => c.Party)
            .ToListAsync();

        foreach (var candidate in candidates)
        {
            candidate.PartyId = AusmittlungUuidV5.BuildDomainOfInfluenceParty(newContestId, candidate.Party!.BaseDomainOfInfluencePartyId);
        }

        await UpdateRangeIgnoreRelations(candidates);
    }
}
