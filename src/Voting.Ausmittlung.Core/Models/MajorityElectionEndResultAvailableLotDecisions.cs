// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models;

public class MajorityElectionEndResultAvailableLotDecisions
{
    public MajorityElectionEndResultAvailableLotDecisions(
        Guid majorityElectionEndResultId,
        MajorityElection majorityElection,
        IReadOnlyCollection<MajorityElectionEndResultAvailableLotDecision> lotDecisions,
        IReadOnlyCollection<SecondaryMajorityElectionEndResultAvailableLotDecisions> secondaryLotDecisions)
    {
        MajorityElectionEndResultId = majorityElectionEndResultId;
        MajorityElection = majorityElection;
        LotDecisions = lotDecisions;
        SecondaryLotDecisions = secondaryLotDecisions;
        PrimaryAndSecondaryLotDecisions = LotDecisions.Concat(SecondaryLotDecisions.SelectMany(x => x.LotDecisions)).ToList();
    }

    public Guid MajorityElectionEndResultId { get; }

    public MajorityElection MajorityElection { get; }

    public IReadOnlyCollection<MajorityElectionEndResultAvailableLotDecision> LotDecisions { get; }

    public IReadOnlyCollection<SecondaryMajorityElectionEndResultAvailableLotDecisions> SecondaryLotDecisions { get; }

    public IReadOnlyCollection<MajorityElectionEndResultAvailableLotDecision> PrimaryAndSecondaryLotDecisions { get; }
}
