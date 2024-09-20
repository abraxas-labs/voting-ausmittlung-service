// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionWriteInBallotPosition : MajorityElectionWriteInBallotPositionBase
{
    public MajorityElectionWriteInBallot? Ballot { get; set; }

    public MajorityElectionWriteInMapping? WriteInMapping { get; set; }
}
