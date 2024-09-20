// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models;

public class ProportionalElectionListEndResultAvailableLotDecisions
{
    public ProportionalElectionListEndResultAvailableLotDecisions(
        Guid proportionalElectionEndResultId,
        ProportionalElectionList proportionalElectionList,
        List<ProportionalElectionEndResultAvailableLotDecision> lotDecisions)
    {
        ProportionalElectionEndResultId = proportionalElectionEndResultId;
        ProportionalElectionList = proportionalElectionList;
        ProportionalElectionListId = proportionalElectionList.Id;
        LotDecisions = lotDecisions;
    }

    public Guid ProportionalElectionEndResultId { get; }

    public Guid ProportionalElectionListId { get; }

    public ProportionalElectionList ProportionalElectionList { get; }

    public List<ProportionalElectionEndResultAvailableLotDecision> LotDecisions { get; }
}
