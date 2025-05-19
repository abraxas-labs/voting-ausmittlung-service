// (c) Copyright by Abraxas Informatik AG
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
        List<ContestCountingCircleDetails> ccDetails,
        string tenantId)
    {
        var (doiResults, notAssignableResult, aggregatedResult) = await base.BuildResults(politicalBusiness, ccDetails, tenantId);
        OrderCountingCircleAndCandidateResults(doiResults);
        OrderCountingCircleAndCandidateResult(notAssignableResult);
        OrderCountingCircleAndCandidateResult(aggregatedResult);
        return (doiResults, notAssignableResult, aggregatedResult);
    }

    protected override IEnumerable<MajorityElectionResult> GetResults(MajorityElection politicalBusiness) => politicalBusiness.Results;

    protected override int GetReportLevel(MajorityElection politicalBusiness) => politicalBusiness.ReportDomainOfInfluenceLevel;

    protected override void ApplyCountingCircleResult(MajorityElectionDomainOfInfluenceResult doiResult, MajorityElectionResult ccResult)
    {
        ApplyCountingCircleResult(doiResult, ccResult, 1);
        doiResult.Results.Add(ccResult);
    }

    protected override void ApplyCountingCircleResultToVirtualResult(MajorityElectionResult target, MajorityElectionResult ccResult)
    {
        if (ccResult.State < target.State)
        {
            target.State = ccResult.State;
        }

        target.Published &= ccResult.Published;

        if (target.TotalSentEVotingVotingCards == null && ccResult.TotalSentEVotingVotingCards != null)
        {
            target.TotalSentEVotingVotingCards = ccResult.TotalSentEVotingVotingCards;
        }
        else if (target.TotalSentEVotingVotingCards != null)
        {
            target.TotalSentEVotingVotingCards += ccResult.TotalSentEVotingVotingCards ?? 0;
        }

        target.CountOfVoters.AddForAllSubTotals(ccResult.CountOfVoters);
        target.TotalCountOfVoters += ccResult.TotalCountOfVoters;
        target.AddForAllSubTotals(ccResult);

        var targetCandidateResultsById = target.CandidateResults.ToDictionary(x => x.CandidateId);
        foreach (var ccCandidateResult in ccResult.CandidateResults)
        {
            if (!targetCandidateResultsById.TryGetValue(ccCandidateResult.CandidateId, out var targetCandidateResult))
            {
                targetCandidateResult = targetCandidateResultsById[ccCandidateResult.CandidateId] = new MajorityElectionCandidateResult
                {
                    Candidate = ccCandidateResult.Candidate,
                    CandidateId = ccCandidateResult.CandidateId,
                    ConventionalVoteCount = 0,
                };

                targetCandidateResult.ElectionResult = target;
                target.CandidateResults.Add(targetCandidateResult);
            }

            targetCandidateResult.ConventionalVoteCount += ccCandidateResult.ConventionalVoteCount ?? 0;
            targetCandidateResult.EVotingWriteInsVoteCount += ccCandidateResult.EVotingWriteInsVoteCount;
            targetCandidateResult.EVotingExclWriteInsVoteCount += ccCandidateResult.EVotingExclWriteInsVoteCount;
            targetCandidateResult.ECountingWriteInsVoteCount += ccCandidateResult.ECountingWriteInsVoteCount;
            targetCandidateResult.ECountingExclWriteInsVoteCount += ccCandidateResult.ECountingExclWriteInsVoteCount;
        }

        target.UpdateVoterParticipation();
    }

    protected override IEnumerable<MajorityElectionResult> GetCountingCircleResults(MajorityElectionDomainOfInfluenceResult doiResult)
        => doiResult.Results;

    protected override void RemoveCountingCircleResult(MajorityElectionDomainOfInfluenceResult doiResult, MajorityElectionResult result)
    {
        ApplyCountingCircleResult(doiResult, result, -1);
        doiResult.Results.Remove(result);
    }

    protected override void ResetCountingCircleResult(MajorityElectionResult ccResult)
    {
        ccResult.TotalCountOfVoters = 0;

        ccResult.ResetAllSubTotals(true);

        ccResult.ConventionalCountOfDetailedEnteredBallots = 0;
        ccResult.ConventionalCountOfBallotGroupVotes = 0;
        ccResult.CountOfBundlesNotReviewedOrDeleted = 0;
    }

    private void ApplyCountingCircleResult(
        MajorityElectionDomainOfInfluenceResult doiResult,
        MajorityElectionResult ccResult,
        int deltaFactor)
    {
        PoliticalBusinessCountOfVotersUtils.AdjustCountOfVoters(
            doiResult.CountOfVoters,
            ccResult.CountOfVoters,
            doiResult.ContestDomainOfInfluenceDetails.TotalCountOfVoters,
            deltaFactor);

        doiResult.IndividualVoteCount += ccResult.IndividualVoteCount * deltaFactor;
        doiResult.InvalidVoteCount += ccResult.InvalidVoteCount * deltaFactor;
        doiResult.EmptyVoteCount += ccResult.EmptyVoteCount * deltaFactor;

        foreach (var candidateResult in ccResult.CandidateResults)
        {
            if (!doiResult.CandidateResultsByCandidateId.TryGetValue(candidateResult.CandidateId, out var doiCandidateResult))
            {
                doiCandidateResult = doiResult.CandidateResultsByCandidateId[candidateResult.CandidateId] =
                    new MajorityElectionCandidateDomainOfInfluenceResult(candidateResult.Candidate);
            }

            doiCandidateResult.VoteCount += candidateResult.VoteCount * deltaFactor;
        }
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
