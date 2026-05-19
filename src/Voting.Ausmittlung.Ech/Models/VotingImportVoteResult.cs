// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Models;

public class VotingImportVoteResult : VotingImportPoliticalBusinessResult
{
    private List<VotingImportVoteBallotResult> _ballotResults;

    // An imported eCH vote result may contain ballot results from different VOTING vote results,
    // because VOTING votes of the same domain of influence get grouped into the same eCH vote.
    // The voteId may not be correct for all ballot results!
    public VotingImportVoteResult(Guid voteId, string basisCountingCircleId, IReadOnlyCollection<VotingImportVoteBallotResult> ballotResults)
        : base(voteId, basisCountingCircleId)
    {
        _ballotResults = ballotResults.ToList();
        PoliticalBusinessType = PoliticalBusinessType.Vote;
    }

    public IReadOnlyCollection<VotingImportVoteBallotResult> BallotResults => _ballotResults;

    public void RemoveBallotResults(IReadOnlyCollection<Guid> ballotIds)
    {
        _ballotResults = _ballotResults.Where(b => !ballotIds.Contains(b.BallotId)).ToList();
    }
}
