// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public abstract class MajorityElectionBase : Election
{
    public abstract bool InvalidVotes { get; set; }
}
