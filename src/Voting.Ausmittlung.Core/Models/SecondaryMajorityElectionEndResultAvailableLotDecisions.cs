// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models;

public class SecondaryMajorityElectionEndResultAvailableLotDecisions
{
    public SecondaryMajorityElectionEndResultAvailableLotDecisions(
        SecondaryMajorityElection secondaryMajorityElection,
        List<MajorityElectionEndResultAvailableLotDecision> lotDecisions)
    {
        SecondaryMajorityElection = secondaryMajorityElection;
        LotDecisions = lotDecisions;
    }

    public SecondaryMajorityElection SecondaryMajorityElection { get; }

    public List<MajorityElectionEndResultAvailableLotDecision> LotDecisions { get; }
}
