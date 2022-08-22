// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public class VoteDomainOfInfluenceResult : DomainOfInfluenceResult
{
    public IEnumerable<VoteDomainOfInfluenceBallotResult> BallotResults => ResultsByBallotId.Values
        .OrderBy(b => b.Ballot.Position);

    internal Dictionary<Guid, VoteDomainOfInfluenceBallotResult> ResultsByBallotId { get; } =
        new Dictionary<Guid, VoteDomainOfInfluenceBallotResult>();
}
