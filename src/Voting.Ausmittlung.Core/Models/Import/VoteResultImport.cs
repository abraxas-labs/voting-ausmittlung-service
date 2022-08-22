// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Models.Import;

public class VoteResultImport : PoliticalBusinessResultImport
{
    private readonly Dictionary<Guid, VoteBallotResultImport> _ballotResults =
        new Dictionary<Guid, VoteBallotResultImport>();

    public VoteResultImport(Guid voteId, Guid basisCountingCircleId)
        : base(voteId, basisCountingCircleId)
    {
    }

    public Guid VoteId => PoliticalBusinessId;

    public IEnumerable<VoteBallotResultImport> BallotResults => _ballotResults.Values;

    internal VoteBallotResultImport GetOrAddBallotResult(Guid ballotId)
        => _ballotResults.GetOrAdd(ballotId, () => new VoteBallotResultImport(ballotId));
}
