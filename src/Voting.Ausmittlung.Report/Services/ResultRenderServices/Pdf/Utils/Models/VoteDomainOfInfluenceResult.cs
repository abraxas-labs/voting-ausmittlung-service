// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public class VoteDomainOfInfluenceResult : DomainOfInfluenceResult
{
    public IEnumerable<VoteDomainOfInfluenceBallotResult> BallotResults => ResultsByBallotId.Values
        .OrderBy(b => b.Ballot.Position);

    internal Dictionary<Guid, VoteDomainOfInfluenceBallotResult> ResultsByBallotId { get; } =
        new Dictionary<Guid, VoteDomainOfInfluenceBallotResult>();

    internal List<VoteResult> VoteResults { get; set; } = [];

    public override void OrderCountingCircleResults(ContestCantonDefaults cantonDefaults)
    {
        foreach (var ballotResult in BallotResults)
        {
            ballotResult.OrderCountingCircleResults(cantonDefaults);
        }
    }
}
