// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.LotDecisionBuilder;

public class MajorityElectionEndResultAvailableLotDecisionsBuilder : ElectionEndResultAvailableLotDecisionsBuilder
{
    public MajorityElectionEndResultAvailableLotDecisions BuildAvailableLotDecisions(MajorityElectionEndResult endResult)
    {
        return new MajorityElectionEndResultAvailableLotDecisions(
            endResult.Id,
            endResult.MajorityElection,
            BuildAvailableLotDecisions<
                MajorityElectionEndResultAvailableLotDecision,
                MajorityElectionCandidateEndResult,
                MajorityElectionCandidateBase>(endResult.CandidateEndResults, x => x.Candidate),
            endResult.SecondaryMajorityElectionEndResults
                .OrderBy(x => x.SecondaryMajorityElection.PoliticalBusinessNumber)
                .ThenBy(x => x.SecondaryMajorityElection.ShortDescription)
                .Select(x => new SecondaryMajorityElectionEndResultAvailableLotDecisions(
                    x.SecondaryMajorityElection,
                    BuildAvailableLotDecisions<
                        MajorityElectionEndResultAvailableLotDecision,
                        SecondaryMajorityElectionCandidateEndResult,
                        MajorityElectionCandidateBase>(x.CandidateEndResults, y => y.Candidate)))
                .ToList());
    }
}
