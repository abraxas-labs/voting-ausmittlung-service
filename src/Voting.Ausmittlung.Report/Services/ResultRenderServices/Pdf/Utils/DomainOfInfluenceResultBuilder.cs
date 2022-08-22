// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public virtual async Task<(List<TResult> Results, TResult NotAssignableResult)> BuildResults(
        TPoliticalBusiness politicalBusiness,
        List<ContestCountingCircleDetails> ccDetails)
    {
        // not assignable result group are results which are not matched by a report level (ex. report level 1 or Auslandschweizer)
        var notAssignableResult = new TResult();

        var dois = await _doiRepo.GetDomainOfInfluencesByReportingLevel(
            politicalBusiness.DomainOfInfluenceId,
            GetReportLevel(politicalBusiness));

        var doiResults = dois
            .Select(x => new TResult { DomainOfInfluence = x })
            .OrderBy(x => x.DomainOfInfluence?.Name)
            .ToList();

        var doiResultByCcId = doiResults
            .SelectMany(result => result.DomainOfInfluence?.CountingCircles.Select(y => (y.CountingCircleId, result)) ?? Enumerable.Empty<(Guid, TResult)>())
            .ToDictionary(x => x.CountingCircleId, x => x.result);
        var ccDetailsByCcId = ccDetails.ToDictionary(cc => cc.CountingCircleId);

        foreach (var ccResult in GetResults(politicalBusiness).Where(x => x.State.IsResultCollectionDone()))
        {
            if (!doiResultByCcId.TryGetValue(ccResult.CountingCircleId, out var doiResult))
            {
                doiResult = notAssignableResult;
            }

            if (ccDetailsByCcId.TryGetValue(ccResult.CountingCircleId, out var ccDetail))
            {
                var receivedVotingCards = ccDetail.SumVotingCards(politicalBusiness.DomainOfInfluence.Type);
                doiResult.ContestDomainOfInfluenceDetails.TotalCountOfVoters += ccDetail.TotalCountOfVoters;
                doiResult.ContestDomainOfInfluenceDetails.TotalCountOfValidVotingCards += receivedVotingCards.Valid;
                doiResult.ContestDomainOfInfluenceDetails.TotalCountOfInvalidVotingCards += receivedVotingCards.Invalid;
            }

            ApplyCountingCircleResult(doiResult, ccResult);
        }

        return (doiResults, notAssignableResult);
    }

    protected abstract IEnumerable<TCountingCircleResult> GetResults(TPoliticalBusiness politicalBusiness);

    protected abstract int GetReportLevel(TPoliticalBusiness politicalBusiness);

    protected abstract void ApplyCountingCircleResult(
        TResult doiResult,
        TCountingCircleResult ccResult);
}
