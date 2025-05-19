// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using ResultImportType = Voting.Ausmittlung.Data.Models.ResultImportType;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProportionalElectionResultImportProcessor : IEventProcessor<ProportionalElectionResultImported>
{
    private readonly IDbRepository<DataContext, ProportionalElectionResult> _proportionalElectionResultRepo;

    public ProportionalElectionResultImportProcessor(IDbRepository<DataContext, ProportionalElectionResult> proportionalElectionResultRepo)
    {
        _proportionalElectionResultRepo = proportionalElectionResultRepo;
    }

    public async Task Process(ProportionalElectionResultImported eventData)
    {
        var electionId = GuidParser.Parse(eventData.ProportionalElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var importType = (ResultImportType)eventData.ImportType;

        // all legacy events are evoting events
        if (importType == ResultImportType.Unspecified)
        {
            importType = ResultImportType.EVoting;
        }

        var dataSource = importType.GetDataSource();
        var result = await _proportionalElectionResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ListResults)
            .ThenInclude(x => x.CandidateResults)
            .ThenInclude(x => x.VoteSources)
            .Include(x => x.UnmodifiedListResults)
            .FirstOrDefaultAsync(x => x.CountingCircle.BasisCountingCircleId == countingCircleId && x.ProportionalElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(ProportionalElectionResult), new { countingCircleId, electionId });

        // The event.CountOfModifiedLists also includes lists without a party. But TotalCountOfModifiedLists only counts lists with a party.
        var subTotal = result.GetSubTotal(dataSource);
        subTotal.TotalCountOfUnmodifiedLists = eventData.CountOfUnmodifiedLists;
        subTotal.TotalCountOfModifiedLists = eventData.CountOfModifiedLists - eventData.CountOfListsWithoutParty;
        subTotal.TotalCountOfListsWithoutParty = eventData.CountOfListsWithoutParty;
        subTotal.TotalCountOfBlankRowsOnListsWithoutParty = eventData.CountOfBlankRowsOnListsWithoutParty;
        UpdateCountOfVoters(importType, eventData, result);
        result.UpdateVoterParticipation();

        if (importType == ResultImportType.EVoting)
        {
            result.TotalSentEVotingVotingCards = eventData.CountOfVotersInformation?.TotalCountOfVoters;
        }

        ProcessCandidates(dataSource, result, eventData.CandidateResults);
        ProcessLists(dataSource, result, eventData.ListResults);

        await _proportionalElectionResultRepo.Update(result);
    }

    private static void UpdateCountOfVoters(
        ResultImportType importType,
        ProportionalElectionResultImported eventData,
        ProportionalElectionResult result)
    {
        var subTotal = result.CountOfVoters.GetNonNullableSubTotal(importType.GetDataSource());
        subTotal.ReceivedBallots = eventData.CountOfVoters;
        subTotal.BlankBallots = eventData.BlankBallotCount;
        subTotal.InvalidBallots = eventData.InvalidBallotCount;
        subTotal.AccountedBallots = eventData.CountOfVoters - eventData.BlankBallotCount - eventData.InvalidBallotCount;
    }

    private void ProcessLists(
        VotingDataSource dataSource,
        ProportionalElectionResult result,
        IEnumerable<ProportionalElectionListResultImportEventData> importedListResults)
    {
        var listResultsById = result.ListResults.ToDictionary(x => x.ListId);
        var unmodifiedListResultsById = result.UnmodifiedListResults.ToDictionary(x => x.ListId);

        foreach (var importedListResult in importedListResults)
        {
            var listId = GuidParser.Parse(importedListResult.ListId);
            var listResult = listResultsById[listId];
            var unmodifiedListResult = unmodifiedListResultsById[listId];

            unmodifiedListResult.SetVoteCountOfDataSource(dataSource, importedListResult.UnmodifiedListsCount);

            var subTotal = listResult.GetSubTotal(dataSource);
            subTotal.UnmodifiedListsCount = importedListResult.UnmodifiedListsCount;
            subTotal.UnmodifiedListVotesCount = importedListResult.UnmodifiedListVotesCount;
            subTotal.UnmodifiedListBlankRowsCount = importedListResult.UnmodifiedListBlankRowsCount;
            subTotal.ModifiedListsCount = importedListResult.ModifiedListsCount;
            subTotal.ModifiedListVotesCount = importedListResult.ModifiedListVotesCount;
            subTotal.ListVotesCountOnOtherLists = importedListResult.ListVotesCountOnOtherLists;
            subTotal.ModifiedListBlankRowsCount = importedListResult.ModifiedListBlankRowsCount;
        }
    }

    private void ProcessCandidates(
        VotingDataSource dataSource,
        ProportionalElectionResult result,
        IEnumerable<ProportionalElectionCandidateResultImportEventData> importedCandidateResults)
    {
        var candidateResultsById = result.ListResults
            .SelectMany(x => x.CandidateResults)
            .ToDictionary(x => x.CandidateId);

        foreach (var importedCandidateResult in importedCandidateResults)
        {
            var candidateResult = candidateResultsById[GuidParser.Parse(importedCandidateResult.CandidateId)];
            var subTotal = candidateResult.GetSubTotal(dataSource);
            subTotal.UnmodifiedListVotesCount = importedCandidateResult.UnmodifiedListVotesCount;
            subTotal.ModifiedListVotesCount = importedCandidateResult.ModifiedListVotesCount;
            subTotal.CountOfVotesOnOtherLists = importedCandidateResult.CountOfVotesOnOtherLists;
            subTotal.CountOfVotesFromAccumulations = importedCandidateResult.CountOfVotesFromAccumulations;
            ProcessCandidateVoteSources(dataSource, candidateResult, importedCandidateResult.VoteSources);
        }
    }

    private void ProcessCandidateVoteSources(
        VotingDataSource dataSource,
        ProportionalElectionCandidateResult candidateResult,
        IEnumerable<ProportionalElectionCandidateVoteSourceResultImportEventData> importedVoteSources)
    {
        var candidateResultVoteSources = candidateResult.VoteSources.ToDictionary(x => x.ListId ?? Guid.Empty);
        foreach (var voteSource in importedVoteSources)
        {
            var listId = GuidParser.ParseNullable(voteSource.ListId);
            if (candidateResultVoteSources.TryGetValue(listId ?? Guid.Empty, out var endResultVoteSource))
            {
                endResultVoteSource.SetVoteCountOfDataSource(dataSource, voteSource.VoteCount);
                continue;
            }

            endResultVoteSource = new ProportionalElectionCandidateVoteSourceResult
            {
                ListId = listId == Guid.Empty ? null : listId,
                CandidateResultId = candidateResult.Id,
            };
            endResultVoteSource.SetVoteCountOfDataSource(dataSource, voteSource.VoteCount);
            candidateResult.VoteSources.Add(endResultVoteSource);
        }
    }
}
