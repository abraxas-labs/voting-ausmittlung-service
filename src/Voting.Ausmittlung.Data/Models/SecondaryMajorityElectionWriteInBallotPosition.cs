// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionWriteInBallotPosition : MajorityElectionWriteInBallotPositionBase
{
    public SecondaryMajorityElectionWriteInBallot? Ballot { get; set; }

    public SecondaryMajorityElectionWriteInMapping? WriteInMapping { get; set; }
}
