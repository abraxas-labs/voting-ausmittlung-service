// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.LotDecisionBuilder;

public class ProportionalElectionListEndResultAvailableLotDecisionsBuilder : ElectionEndResultAvailableLotDecisionsBuilder
{
    public ProportionalElectionListEndResultAvailableLotDecisions BuildAvailableLotDecisions(ProportionalElectionListEndResult listEndResult)
    {
        return new ProportionalElectionListEndResultAvailableLotDecisions(
            listEndResult.ElectionEndResultId,
            listEndResult.List,
            BuildAvailableLotDecisions<
                ProportionalElectionEndResultAvailableLotDecision,
                ProportionalElectionCandidateEndResult,
                ProportionalElectionCandidate>(listEndResult.CandidateEndResults, x => x.Candidate));
    }
}
