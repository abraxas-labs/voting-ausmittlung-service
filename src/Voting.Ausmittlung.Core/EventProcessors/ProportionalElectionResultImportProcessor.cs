// (c) Copyright 2022 by Abraxas Informatik AG
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
        var result = await _proportionalElectionResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ListResults)
            .ThenInclude(x => x.CandidateResults)
            .ThenInclude(x => x.VoteSources)
            .Include(x => x.UnmodifiedListResults)
            .FirstOrDefaultAsync(x => x.CountingCircle.BasisCountingCircleId == countingCircleId && x.ProportionalElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(ProportionalElectionResult), new { countingCircleId, electionId });
        result.EVotingSubTotal.TotalCountOfUnmodifiedLists = eventData.CountOfUnmodifiedLists;
        result.EVotingSubTotal.TotalCountOfModifiedLists = eventData.CountOfModifiedLists;
        result.EVotingSubTotal.TotalCountOfListsWithoutParty = eventData.CountOfListsWithoutParty;
        result.EVotingSubTotal.TotalCountOfBlankRowsOnListsWithoutParty = eventData.CountOfBlankRowsOnListsWithoutParty;
        result.CountOfVoters.EVotingReceivedBallots = eventData.CountOfVoters;
        result.CountOfVoters.EVotingBlankBallots = eventData.BlankBallotCount;
        result.CountOfVoters.EVotingInvalidBallots = eventData.InvalidBallotCount;
        result.CountOfVoters.EVotingAccountedBallots = eventData.CountOfVoters - eventData.BlankBallotCount - eventData.InvalidBallotCount;
        result.UpdateVoterParticipation();

        ProcessCandidates(result, eventData.CandidateResults);
        ProcessLists(result, eventData.ListResults);

        await _proportionalElectionResultRepo.Update(result);
    }

    private void ProcessLists(
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

            unmodifiedListResult.EVotingVoteCount = importedListResult.UnmodifiedListsCount;
            listResult.EVotingSubTotal.UnmodifiedListsCount = importedListResult.UnmodifiedListsCount;
            listResult.EVotingSubTotal.UnmodifiedListVotesCount = importedListResult.UnmodifiedListVotesCount;
            listResult.EVotingSubTotal.UnmodifiedListBlankRowsCount = importedListResult.UnmodifiedListBlankRowsCount;
            listResult.EVotingSubTotal.ModifiedListsCount = importedListResult.ModifiedListsCount;
            listResult.EVotingSubTotal.ModifiedListVotesCount = importedListResult.ModifiedListVotesCount;
            listResult.EVotingSubTotal.ListVotesCountOnOtherLists = importedListResult.ListVotesCountOnOtherLists;
            listResult.EVotingSubTotal.ModifiedListBlankRowsCount = importedListResult.ModifiedListBlankRowsCount;
        }
    }

    private void ProcessCandidates(
        ProportionalElectionResult result,
        IEnumerable<ProportionalElectionCandidateResultImportEventData> importedCandidateResults)
    {
        var candidateResultsById = result.ListResults
            .SelectMany(x => x.CandidateResults)
            .ToDictionary(x => x.CandidateId);

        foreach (var importedCandidateResult in importedCandidateResults)
        {
            var candidateResult = candidateResultsById[GuidParser.Parse(importedCandidateResult.CandidateId)];
            candidateResult.EVotingSubTotal.UnmodifiedListVotesCount = importedCandidateResult.UnmodifiedListVotesCount;
            candidateResult.EVotingSubTotal.ModifiedListVotesCount = importedCandidateResult.ModifiedListVotesCount;
            candidateResult.EVotingSubTotal.CountOfVotesOnOtherLists = importedCandidateResult.CountOfVotesOnOtherLists;
            candidateResult.EVotingSubTotal.CountOfVotesFromAccumulations = importedCandidateResult.CountOfVotesFromAccumulations;
            ProcessCandidateVoteSources(candidateResult, importedCandidateResult.VoteSources);
        }
    }

    private void ProcessCandidateVoteSources(
        ProportionalElectionCandidateResult candidateResult,
        IEnumerable<ProportionalElectionCandidateVoteSourceResultImportEventData> importedVoteSources)
    {
        var candidateResultVoteSources = candidateResult.VoteSources.ToDictionary(x => x.ListId ?? Guid.Empty);
        foreach (var voteSource in importedVoteSources)
        {
            var listId = GuidParser.ParseNullable(voteSource.ListId);
            if (candidateResultVoteSources.TryGetValue(listId ?? Guid.Empty, out var endResultVoteSource))
            {
                endResultVoteSource.EVotingVoteCount = voteSource.VoteCount;
                continue;
            }

            endResultVoteSource = new ProportionalElectionCandidateVoteSourceResult
            {
                ListId = listId == Guid.Empty ? null : listId,
                CandidateResultId = candidateResult.Id,
            };
            candidateResult.VoteSources.Add(endResultVoteSource);
        }
    }
}
