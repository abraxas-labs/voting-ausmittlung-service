﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionBase : Election
{
    public bool IndividualCandidatesDisabled { get; set; }
}
