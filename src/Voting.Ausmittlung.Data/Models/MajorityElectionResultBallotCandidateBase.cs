// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class MajorityElectionResultBallotCandidateBase : BaseEntity
{
    public Guid CandidateId { get; set; }

    public bool Selected { get; set; }
}
