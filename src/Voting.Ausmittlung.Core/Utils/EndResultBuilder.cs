// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils;

public class EndResultBuilder
{
    private readonly DataContext _db;
    private readonly VoteEndResultBuilder _voteEndResultBuilder;
    private readonly ProportionalElectionEndResultBuilder _proportionalElectionEndResultBuilder;
    private readonly MajorityElectionEndResultBuilder _majorityElectionEndResultBuilder;

    public EndResultBuilder(
        DataContext db,
        VoteEndResultBuilder voteEndResultBuilder,
        ProportionalElectionEndResultBuilder proportionalElectionEndResultBuilder,
        MajorityElectionEndResultBuilder majorityElectionEndResultBuilder)
    {
        _db = db;
        _voteEndResultBuilder = voteEndResultBuilder;
        _proportionalElectionEndResultBuilder = proportionalElectionEndResultBuilder;
        _majorityElectionEndResultBuilder = majorityElectionEndResultBuilder;
    }

    internal async Task AdjustEndResultsForCountingCircleDetailsReset(ContestCountingCircleDetails ccDetails)
    {
        var affectedResults = await _db.SimpleCountingCircleResults
            .Include(r => r.PoliticalBusiness)
            .Where(r => r.CountingCircleId == ccDetails.CountingCircleId
                && r.State >= CountingCircleResultState.SubmissionDone)
            .ToListAsync();

        foreach (var affectedResult in affectedResults)
        {
            switch (affectedResult.PoliticalBusiness!.BusinessType)
            {
                case PoliticalBusinessType.Vote:
                    await _voteEndResultBuilder.AdjustVoteEndResult(affectedResult.Id, true);
                    break;
                case PoliticalBusinessType.ProportionalElection:
                    await _proportionalElectionEndResultBuilder.AdjustEndResult(affectedResult.Id, true);
                    break;
                case PoliticalBusinessType.MajorityElection:
                    await _majorityElectionEndResultBuilder.AdjustEndResult(affectedResult.Id, true);
                    break;
            }
        }
    }
}
