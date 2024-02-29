// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

public class ProportionalElectionManualCandidateEndResult
{
    public Guid CandidateId { get; set; }

    public ProportionalElectionCandidateEndResultState State { get; set; }
}
