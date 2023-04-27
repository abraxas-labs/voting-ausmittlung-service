// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public class MajorityElectionDomainOfInfluenceResultBuilder
    : DomainOfInfluenceResultBuilder<MajorityElection, MajorityElectionDomainOfInfluenceResult, MajorityElectionResult>
{
    public MajorityElectionDomainOfInfluenceResultBuilder(DomainOfInfluenceRepo doiRepo)
        : base(doiRepo)
    {
    }

    public override async Task<(List<MajorityElectionDomainOfInfluenceResult> Results, MajorityElectionDomainOfInfluenceResult NotAssignableResult, MajorityElectionDomainOfInfluenceResult AggregatedResult)> BuildResults(
        MajorityElection politicalBusiness,
        List<ContestCountingCircleDetails> ccDetails)
    {
        var (doiResults, notAssignableResult, aggregatedResult) = await base.BuildResults(politicalBusiness, ccDetails);
        OrderCountingCircleAndCandidateResults(doiResults);
        OrderCountingCircleAndCandidateResult(notAssignableResult);
        OrderCountingCircleAndCandidateResult(aggregatedResult);
        return (doiResults, notAssignableResult, aggregatedResult);
    }

    protected override IEnumerable<MajorityElectionResult> GetResults(MajorityElection politicalBusiness) => politicalBusiness.Results;

    protected override int GetReportLevel(MajorityElection politicalBusiness) => politicalBusiness.ReportDomainOfInfluenceLevel;

    protected override void ApplyCountingCircleResult(MajorityElectionDomainOfInfluenceResult doiResult, MajorityElectionResult ccResult)
    {
        PoliticalBusinessCountOfVotersUtils.AdjustCountOfVoters(
            doiResult.CountOfVoters,
            ccResult.CountOfVoters,
            doiResult.ContestDomainOfInfluenceDetails.TotalCountOfVoters);

        doiResult.IndividualVoteCount += ccResult.IndividualVoteCount;
        doiResult.InvalidVoteCount += ccResult.InvalidVoteCount;
        doiResult.EmptyVoteCount += ccResult.EmptyVoteCount;

        foreach (var candidateResult in ccResult.CandidateResults)
        {
            if (!doiResult.CandidateResultsByCandidateId.TryGetValue(candidateResult.CandidateId, out var doiCandidateResult))
            {
                doiCandidateResult = doiResult.CandidateResultsByCandidateId[candidateResult.CandidateId] =
                    new MajorityElectionCandidateDomainOfInfluenceResult(candidateResult.Candidate);
            }

            doiCandidateResult.VoteCount += candidateResult.VoteCount;
        }

        doiResult.Results.Add(ccResult);
    }

    protected override void ResetCountingCircleResult(MajorityElectionResult ccResult)
    {
        ccResult.TotalCountOfVoters = 0;

        ccResult.ResetAllSubTotals(VotingDataSource.EVoting, true);
        ccResult.ResetAllSubTotals(VotingDataSource.Conventional, true);

        ccResult.ConventionalCountOfDetailedEnteredBallots = 0;
        ccResult.ConventionalCountOfBallotGroupVotes = 0;
        ccResult.CountOfBundlesNotReviewedOrDeleted = 0;
    }

    private void OrderCountingCircleAndCandidateResults(IEnumerable<MajorityElectionDomainOfInfluenceResult> doiResults)
    {
        foreach (var doiResult in doiResults)
        {
            OrderCountingCircleAndCandidateResult(doiResult);
        }
    }

    private void OrderCountingCircleAndCandidateResult(MajorityElectionDomainOfInfluenceResult doiResult)
    {
        foreach (var ccResult in doiResult.Results)
        {
            ccResult.CandidateResults = ccResult.CandidateResults
                .OrderBy(x => x.Candidate.Position)
                .ToList();
        }
    }
}
