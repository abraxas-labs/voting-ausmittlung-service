// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Ech.Models;

public class EVotingElectionBallot
{
    public EVotingElectionBallot(Guid? listId, bool unmodified, IReadOnlyCollection<EVotingElectionBallotPosition> positions)
    {
        ListId = listId;
        Positions = positions;
        Unmodified = unmodified;
    }

    public Guid? ListId { get; internal set; }

    public bool Unmodified { get; internal set; }

    public IReadOnlyCollection<EVotingElectionBallotPosition> Positions { get; }
}
