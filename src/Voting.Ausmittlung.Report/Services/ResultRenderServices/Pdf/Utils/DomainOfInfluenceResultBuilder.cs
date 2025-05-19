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
    where TCountingCircleResult : CountingCircleResult, new()
{
    private readonly DomainOfInfluenceRepo _doiRepo;

    protected DomainOfInfluenceResultBuilder(DomainOfInfluenceRepo doiRepo)
    {
        _doiRepo = doiRepo;
    }

    public virtual async Task<(List<TResult> Results, TResult NotAssignableResult, TResult AggregatedResult)> BuildResults(
        TPoliticalBusiness politicalBusiness,
        List<ContestCountingCircleDetails> ccDetails,
        string tenantId)
    {
        // not assignable result group are results which are not matched by a report level (ex. report level 1 or Auslandschweizer)
        var notAssignableResult = new TResult();
        var aggregatedResult = new TResult();

        // If a DOI has a higher report level, it is only fetched to hide counting circle results.
        // It won't be shown as a DOI result.
        var reportLevel = GetReportLevel(politicalBusiness);
        var relevantDois = await _doiRepo.GetRelevantDomainOfInfluencesForReportingLevel(
            politicalBusiness.DomainOfInfluenceId,
            reportLevel);

        politicalBusiness.Results = politicalBusiness
            .Results
            .OrderByCountingCircle(x => x.CountingCircle, politicalBusiness.Contest.CantonDefaults)
            .ToList();

        var doi = politicalBusiness.DomainOfInfluence;
        var ccResults = GetResults(politicalBusiness).ToList();
        var ccIds = ccResults.ConvertAll(r => r.CountingCircleId);

        var doiResults = relevantDois
            .Where(d =>
                d.ReportLevel <= reportLevel
                && d.DomainOfInfluence!.CountingCircles.Any(doiCc => ccIds.Contains(doiCc.CountingCircleId)))
            .Select(x => new TResult { DomainOfInfluence = x.DomainOfInfluence })
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

        // Now that everything has been set up, we need to hide the counting circles
        // that are hidden by DOIs with the HideLowerDoisInReports flag, if they are from another tenant.
        var doisWithHideFlag = relevantDois
            .Where(x => x.DomainOfInfluence!.HideLowerDomainOfInfluencesInReports && x.DomainOfInfluence.SecureConnectId != tenantId)
            .Select(x => x.DomainOfInfluence!);
        HideAndReplaceCountingCircleResults(
            doiResults.Append(aggregatedResult),
            doisWithHideFlag,
            ccDetails,
            politicalBusiness.Contest.CantonDefaults);

        doiResults = doiResults
            .OrderByDomainOfInfluence(x => x.DomainOfInfluence!, politicalBusiness.Contest.CantonDefaults)
            .ToList();
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

    protected abstract void ApplyCountingCircleResultToVirtualResult(
        TCountingCircleResult target,
        TCountingCircleResult ccResult);

    protected abstract IEnumerable<TCountingCircleResult> GetCountingCircleResults(TResult doiResult);

    protected abstract void RemoveCountingCircleResult(TResult doiResult, TCountingCircleResult result);

    private void HideAndReplaceCountingCircleResults(
        IEnumerable<TResult> results,
        IEnumerable<DomainOfInfluence> doisWithHideFlag,
        List<ContestCountingCircleDetails> ccDetails,
        ContestCantonDefaults cantonDefaults)
    {
        var replacementDoiByCcId = doisWithHideFlag
            .SelectMany(x => x.CountingCircles)
            .ToDictionary(x => x.CountingCircleId, x => x.DomainOfInfluence);

        foreach (var result in results)
        {
            var virtualCccResultByDoiId = new Dictionary<Guid, TCountingCircleResult>();

            // ToArray on the ccResults to ensure that we do not modify the enumerable during iteration
            foreach (var ccResult in GetCountingCircleResults(result).ToArray())
            {
                if (!replacementDoiByCcId.TryGetValue(ccResult.CountingCircleId, out var replacementDoi))
                {
                    continue;
                }

                if (!virtualCccResultByDoiId.TryGetValue(replacementDoi.Id, out var virtualCcResult))
                {
                    virtualCcResult = virtualCccResultByDoiId[replacementDoi.Id] = new TCountingCircleResult
                    {
                        CountingCircleId = replacementDoi.Id,
                        CountingCircle = new CountingCircle
                        {
                            Id = replacementDoi.Id,
                            Name = replacementDoi.Name,
                            NameForProtocol = replacementDoi.NameForProtocol,
                            Bfs = replacementDoi.Bfs,
                            Code = replacementDoi.Code,
                            SortNumber = replacementDoi.SortNumber,
                        },
                        State = CountingCircleResultState.Plausibilised,
                        Published = true,
                    };
                }

                ApplyCountingCircleResultToVirtualResult(virtualCcResult, ccResult);
                RemoveCountingCircleResult(result, ccResult);

                // Need to adjust the list of contest cc details, since this will be used later on
                AdjustContestCcDetails(ccDetails, ccResult.CountingCircle, virtualCcResult.CountingCircle);
            }

            foreach (var virtualCcResult in virtualCccResultByDoiId.Values)
            {
                ApplyCountingCircleResult(result, virtualCcResult);
            }

            result.OrderCountingCircleResults(cantonDefaults);
        }
    }

    private void AdjustContestCcDetails(
        List<ContestCountingCircleDetails> ccDetails,
        CountingCircle toRemove,
        CountingCircle toAdd)
    {
        var ccDetailsToRemove = ccDetails.Find(x => x.CountingCircleId == toRemove.Id);
        if (ccDetailsToRemove == null)
        {
            // Has already been adjusted
            return;
        }

        ccDetails.Remove(ccDetailsToRemove);

        var ccDetailsToAdd = ccDetails.Find(x => x.CountingCircleId == toAdd.Id);
        if (ccDetailsToAdd == null)
        {
            ccDetailsToAdd = new ContestCountingCircleDetails
            {
                CountingCircleId = toAdd.Id,
                CountingCircle = toAdd,
            };
            ccDetails.Add(ccDetailsToAdd);
        }

        ccDetailsToAdd.EVoting |= ccDetailsToRemove.EVoting;
        ccDetailsToAdd.ECounting |= ccDetailsToRemove.ECounting;
        ccDetailsToAdd.TotalCountOfVoters += ccDetailsToRemove.TotalCountOfVoters;
        if (ccDetailsToAdd.CountingMachine < ccDetailsToRemove.CountingMachine)
        {
            ccDetailsToAdd.CountingMachine = ccDetailsToRemove.CountingMachine;
        }

        foreach (var votingCards in ccDetailsToRemove.VotingCards)
        {
            var existing = ccDetailsToAdd.VotingCards.FirstOrDefault(
                x => x.Valid == votingCards.Valid
                    && x.DomainOfInfluenceType == votingCards.DomainOfInfluenceType
                    && x.Channel == votingCards.Channel);
            if (existing == null)
            {
                existing = new VotingCardResultDetail
                {
                    Valid = votingCards.Valid,
                    DomainOfInfluenceType = votingCards.DomainOfInfluenceType,
                    Channel = votingCards.Channel,
                    CountOfReceivedVotingCards = 0,
                };
                ccDetailsToAdd.VotingCards.Add(existing);
            }

            existing.CountOfReceivedVotingCards += votingCards.CountOfReceivedVotingCards ?? 0;
        }

        foreach (var countOfVoters in ccDetailsToRemove.CountOfVotersInformationSubTotals)
        {
            var existing = ccDetailsToAdd.CountOfVotersInformationSubTotals.FirstOrDefault(
                x => x.VoterType == countOfVoters.VoterType && x.Sex == countOfVoters.Sex);
            if (existing == null)
            {
                existing = new CountOfVotersInformationSubTotal
                {
                    VoterType = countOfVoters.VoterType,
                    Sex = countOfVoters.Sex,
                    CountOfVoters = 0,
                };
                ccDetailsToAdd.CountOfVotersInformationSubTotals.Add(existing);
            }

            existing.CountOfVoters += countOfVoters.CountOfVoters ?? 0;
        }
    }

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
