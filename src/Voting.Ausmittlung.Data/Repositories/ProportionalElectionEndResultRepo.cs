// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionEndResultRepo : DbRepository<DataContext, ProportionalElectionEndResult>
{
    public ProportionalElectionEndResultRepo(DataContext context)
        : base(context)
    {
    }

    public Task<ProportionalElectionEndResult?> GetByProportionalElectionId(Guid proportionalElectionId)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.ProportionalElection)
            .Include(x => x.ListEndResults).ThenInclude(x => x.CandidateEndResults)
            .FirstOrDefaultAsync(x => x.ProportionalElectionId == proportionalElectionId);
    }

    public Task<ProportionalElectionEndResult?> GetByProportionalElectionIdAsTracked(Guid proportionalElectionId)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.HagenbachBischoffRootGroup)
            .Include(x => x.ProportionalElection)
            .Include(x => x.ListEndResults)
                .ThenInclude(x => x.List)
                    .ThenInclude(x => x.ProportionalElectionListUnionEntries)
                        .ThenInclude(x => x.ProportionalElectionListUnion)
            .Include(x => x.ListEndResults)
                .ThenInclude(x => x.CandidateEndResults)
                    .ThenInclude(x => x.VoteSources)
            .FirstOrDefaultAsync(x => x.ProportionalElectionId == proportionalElectionId);
    }

    public Task<List<ProportionalElectionEndResult>> ListWithResultsByContestIdAsTracked(Guid contestId)
    {
        return Set
            .AsSplitQuery()
            .AsTracking()
            .Include(x => x.HagenbachBischoffRootGroup)
            .Include(x => x.ListEndResults)
                .ThenInclude(x => x.CandidateEndResults)
                    .ThenInclude(x => x.VoteSources)
            .Include(x => x.ProportionalElection.Results)
                .ThenInclude(x => x.UnmodifiedListResults)
            .Include(x => x.ProportionalElection.Results)
                .ThenInclude(x => x.ListResults)
                    .ThenInclude(x => x.CandidateResults)
                        .ThenInclude(x => x.VoteSources)
            .Where(x => x.ProportionalElection.ContestId == contestId)
            .ToListAsync();
    }
}
