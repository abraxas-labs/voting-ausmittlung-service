// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public abstract class DomainOfInfluenceResultBuilder<TPoliticalBusiness, TResult, TCountingCircleResult>
    where TPoliticalBusiness : PoliticalBusiness, IHasResults
    where TResult : DomainOfInfluenceResult, new()
    where TCountingCircleResult : CountingCircleResult
{
    private readonly DomainOfInfluenceRepo _doiRepo;

    protected DomainOfInfluenceResultBuilder(DomainOfInfluenceRepo doiRepo)
    {
        _doiRepo = doiRepo;
    }

    public virtual async Task<(List<TResult> Results, TResult NotAssignableResult, TResult AggregatedResult)> BuildResults(
        TPoliticalBusiness politicalBusiness,
        List<ContestCountingCircleDetails> ccDetails)
    {
        // not assignable result group are results which are not matched by a report level (ex. report level 1 or Auslandschweizer)
        var notAssignableResult = new TResult();
        var aggregatedResult = new TResult();

        var dois = await _doiRepo.GetDomainOfInfluencesByReportingLevel(
            politicalBusiness.DomainOfInfluenceId,
            GetReportLevel(politicalBusiness));

        politicalBusiness.Results = politicalBusiness
            .Results
            .OrderByCountingCircle(x => x.CountingCircle, politicalBusiness.Contest.CantonDefaults)
            .ToList();

        var doi = politicalBusiness.DomainOfInfluence;
        var ccResults = GetResults(politicalBusiness).ToList();
        var ccIds = ccResults.ConvertAll(r => r.CountingCircleId);

        var doiResults = dois
            .Where(doi => doi.CountingCircles.Any(doiCc => ccIds.Contains(doiCc.CountingCircleId)))
            .Select(x => new TResult { DomainOfInfluence = x })
            .OrderByDomainOfInfluence(x => x.DomainOfInfluence!, politicalBusiness.Contest.CantonDefaults)
            .ToList();

        var doiResultByCcId = doiResults
            .SelectMany(result => result.DomainOfInfluence?.CountingCircles.DistinctBy(x => x.CountingCircleId).Select(y => (y.CountingCircleId, result)) ?? Enumerable.Empty<(Guid, TResult)>())
            .ToDictionary(x => x.CountingCircleId, x => x.result);
        var ccDetailsByCcId = ccDetails.ToDictionary(cc => cc.CountingCircleId);

        foreach (var ccResult in ccResults)
        {
            var isCcResultResultAuditedTentatively = ccResult.State.IsResultAuditedTentatively();
            if (!isCcResultResultAuditedTentatively)
            {
                ResetCountingCircleResult(ccResult);
            }

            if (!doiResultByCcId.TryGetValue(ccResult.CountingCircleId, out var doiResult))
            {
                doiResult = notAssignableResult;
            }

            if (ccDetailsByCcId.TryGetValue(ccResult.CountingCircleId, out var ccDetail))
            {
                if (!isCcResultResultAuditedTentatively)
                {
                    ResetCountingCircleDetail(ccDetail);
                }

                ApplyContestCountingCircleDetail(doiResult, ccDetail, doi);
                ApplyContestCountingCircleDetail(aggregatedResult, ccDetail, doi);
            }

            ApplyCountingCircleResult(doiResult, ccResult);
            ApplyCountingCircleResult(aggregatedResult, ccResult);
        }

        return (doiResults, notAssignableResult, aggregatedResult);
    }

    internal void ApplyContestCountingCircleDetail(TResult result, ContestCountingCircleDetails ccDetail, DomainOfInfluence doi)
    {
        var receivedVotingCards = ccDetail.SumVotingCards(doi.Type);
        result.ContestDomainOfInfluenceDetails.TotalCountOfVoters += ccDetail.GetTotalCountOfVotersForDomainOfInfluence(doi);
        result.ContestDomainOfInfluenceDetails.TotalCountOfValidVotingCards += receivedVotingCards.Valid;
        result.ContestDomainOfInfluenceDetails.TotalCountOfInvalidVotingCards += receivedVotingCards.Invalid;
    }

    protected abstract IEnumerable<TCountingCircleResult> GetResults(TPoliticalBusiness politicalBusiness);

    protected abstract int GetReportLevel(TPoliticalBusiness politicalBusiness);

    protected abstract void ResetCountingCircleResult(TCountingCircleResult ccResult);

    protected abstract void ApplyCountingCircleResult(
        TResult doiResult,
        TCountingCircleResult ccResult);

    private void ResetCountingCircleDetail(ContestCountingCircleDetails ccDetails)
    {
        ccDetails.TotalCountOfVoters = 0;

        foreach (var subTotal in ccDetails.CountOfVotersInformationSubTotals)
        {
            subTotal.CountOfVoters = 0;
        }

        foreach (var vc in ccDetails.VotingCards)
        {
            vc.CountOfReceivedVotingCards = 0;
        }
    }
}
