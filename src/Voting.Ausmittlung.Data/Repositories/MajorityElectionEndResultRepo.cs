// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class MajorityElectionEndResultRepo : DbRepository<DataContext, MajorityElectionEndResult>
{
    public MajorityElectionEndResultRepo(DataContext context)
        : base(context)
    {
    }

    public Task<MajorityElectionEndResult?> GetByMajorityElectionIdAsTracked(Guid majorityElectionId)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.MajorityElection.Contest.CantonDefaults)
            .Include(x => x.MajorityElection.MajorityElectionCandidates)
            .Include(x => x.MajorityElection.SecondaryMajorityElections).ThenInclude(x => x.Candidates)
            .Include(x => x.CandidateEndResults)
            .Include(x => x.SecondaryMajorityElectionEndResults).ThenInclude(x => x.CandidateEndResults)
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(x => x.MajorityElectionId == majorityElectionId);
    }

    public Task<MajorityElectionEndResult?> GetByMajorityElectionId(Guid majorityElectionId)
    {
        return Set
            .AsSplitQuery()
            .Include(x => x.CandidateEndResults)
            .Include(x => x.SecondaryMajorityElectionEndResults).ThenInclude(x => x.CandidateEndResults)
            .Include(x => x.MajorityElection)
            .FirstOrDefaultAsync(x => x.MajorityElectionId == majorityElectionId);
    }

    public Task<List<MajorityElectionEndResult>> ListWithResultsByContestIdAsTracked(Guid contestId)
    {
        return Set
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.CandidateEndResults)
            .Include(x => x.SecondaryMajorityElectionEndResults).ThenInclude(x => x.CandidateEndResults)
            .Include(x => x.MajorityElection.Results)
            .ThenInclude(x => x.CandidateResults)
            .Include(x => x.MajorityElection.SecondaryMajorityElections)
            .ThenInclude(x => x.Results)
            .ThenInclude(x => x.CandidateResults)
            .Where(x => x.MajorityElection.ContestId == contestId)
            .ToListAsync();
    }
}
