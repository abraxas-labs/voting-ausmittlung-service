// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public class VoteEndResult : PoliticalBusinessEndResultBase,
    IEndResultDetail<VoteEndResultCountOfVotersInformationSubTotal, VoteEndResultVotingCardDetail>
{
    public Guid VoteId { get; set; }

    public Vote Vote { get; set; } = null!;

    public ICollection<VoteEndResultCountOfVotersInformationSubTotal> CountOfVotersInformationSubTotals { get; set; }
        = new HashSet<VoteEndResultCountOfVotersInformationSubTotal>();

    public ICollection<VoteEndResultVotingCardDetail> VotingCards { get; set; }
        = new HashSet<VoteEndResultVotingCardDetail>();

    public ICollection<BallotEndResult> BallotEndResults { get; set; } = new HashSet<BallotEndResult>();

    public void ResetAllSubTotals(VotingDataSource dataSource, bool includeCountOfVoters = false)
    {
        foreach (var endResult in BallotEndResults)
        {
            endResult.ResetAllSubTotals(dataSource);

            if (includeCountOfVoters)
            {
                endResult.ResetCountOfVoters(dataSource, TotalCountOfVoters);
            }
        }
    }

    public void OrderBallotResults()
    {
        BallotEndResults = BallotEndResults
            .OrderBy(x => x.Ballot.Position)
            .ToList();

        foreach (var ballotEndResult in BallotEndResults)
        {
            ballotEndResult.OrderQuestionResults();
        }
    }
}
