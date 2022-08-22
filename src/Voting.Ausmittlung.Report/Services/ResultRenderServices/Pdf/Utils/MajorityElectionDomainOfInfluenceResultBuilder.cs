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

    public override async Task<(List<MajorityElectionDomainOfInfluenceResult> Results, MajorityElectionDomainOfInfluenceResult NotAssignableResult)> BuildResults(
        MajorityElection politicalBusiness,
        List<ContestCountingCircleDetails> ccDetails)
    {
        var (doiResults, notAssignableResult) = await base.BuildResults(politicalBusiness, ccDetails);
        OrderCountingCircleAndCandidateResults(doiResults);
        OrderCountingCircleAndCandidateResult(notAssignableResult);
        return (doiResults, notAssignableResult);
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

    private void OrderCountingCircleAndCandidateResults(IEnumerable<MajorityElectionDomainOfInfluenceResult> doiResults)
    {
        foreach (var doiResult in doiResults)
        {
            OrderCountingCircleAndCandidateResult(doiResult);
        }
    }

    private void OrderCountingCircleAndCandidateResult(MajorityElectionDomainOfInfluenceResult doiResult)
    {
        doiResult.Results = doiResult.Results
            .OrderBy(x => x.CountingCircle.Name)
            .ToList();

        foreach (var ccResult in doiResult.Results)
        {
            ccResult.CandidateResults = ccResult.CandidateResults
                .OrderBy(x => x.Candidate.Position)
                .ToList();
        }
    }
}
