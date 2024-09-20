// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Core.Services.Write.Import;

public class ProportionalElectionResultImportWriter
    : PoliticalBusinessResultImportWriter<ProportionalElectionResultAggregate, ProportionalElectionResult>
{
    private readonly IDbRepository<DataContext, ProportionalElection> _proportionalElectionRepo;

    public ProportionalElectionResultImportWriter(
        IDbRepository<DataContext, ProportionalElection> proportionalElectionRepo,
        IAggregateRepository aggregateRepository)
        : base(aggregateRepository)
    {
        _proportionalElectionRepo = proportionalElectionRepo;
    }

    internal async IAsyncEnumerable<ProportionalElectionResultImport> BuildImports(
        Guid contestId,
        IReadOnlyCollection<EVotingElectionResult> results)
    {
        var electionIds = results.Select(x => x.PoliticalBusinessId).ToHashSet();
        var elections = await _proportionalElectionRepo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == contestId && electionIds.Contains(x.Id))
            .Include(x => x.ProportionalElectionLists)
            .ThenInclude(x => x.ProportionalElectionCandidates)
            .ToListAsync();

        var electionsById = elections.ToDictionary(x => x.Id);
        var listsById = electionsById.Values.SelectMany(x => x.ProportionalElectionLists).ToDictionary(x => x.Id);
        var candidatesById = listsById.Values.SelectMany(x => x.ProportionalElectionCandidates).ToDictionary(x => x.Id);

        foreach (var result in results)
        {
            if (!electionsById.TryGetValue(result.PoliticalBusinessId, out var election))
            {
                throw new EntityNotFoundException(nameof(ProportionalElection), result.PoliticalBusinessId);
            }

            yield return ProcessResult(result, election, listsById, candidatesById);
        }
    }

    protected override IQueryable<ProportionalElectionResult> BuildResultsQuery(Guid contestId)
        => _proportionalElectionRepo.Query()
            .Where(x => x.ContestId == contestId)
            .SelectMany(x => x.Results);

    private ProportionalElectionResultImport ProcessResult(
        EVotingElectionResult result,
        ProportionalElection election,
        IReadOnlyDictionary<Guid, ProportionalElectionList> listsById,
        IReadOnlyDictionary<Guid, ProportionalElectionCandidate> candidatesById)
    {
        var importResult = new ProportionalElectionResultImport(result.PoliticalBusinessId, Guid.Parse(result.BasisCountingCircleId), new(result.CountOfVotersInformation!.CountOfVotersTotal));
        importResult.CountOfVoters = result.Ballots.Count;

        foreach (var ballot in result.Ballots)
        {
            var (list, listResult) = GetList(importResult, ballot, listsById);

            if (ballot.Positions.All(p => p.IsEmpty))
            {
                if (list == null)
                {
                    // If all positions and the list are empty, treat the whole ballot as blank.
                    importResult.BlankBallotCount++;
                }
                else
                {
                    // If all positions are empty but a list is specified, the whole ballot is invalid.
                    importResult.InvalidBallotCount++;
                }

                continue;
            }

            if (ballot.Unmodified)
            {
                // Note that completely empty lists can also be unmodified (the "empty list") and thus need to be handled before this.
                if (listResult == null || list == null)
                {
                    throw new ValidationException("an unmodified ballot does not have a list assigned");
                }

                ProcessUnmodifiedBallot(importResult, list, listResult);
                continue;
            }

            if (election.NumberOfMandates < ballot.Positions.Count)
            {
                throw new ValidationException(
                    $"the number of ballot positions exceeds the number of mandates ({ballot.Positions.Count} vs {election.NumberOfMandates})");
            }

            var missingPositions = Math.Max(election.NumberOfMandates - ballot.Positions.Count, 0);
            ProcessModifiedBallot(missingPositions, importResult, listResult, ballot, candidatesById);
        }

        return importResult;
    }

    private (ProportionalElectionList? List, ProportionalElectionListResultImport? ListResult) GetList(
        ProportionalElectionResultImport importData,
        EVotingElectionBallot electionBallot,
        IReadOnlyDictionary<Guid, ProportionalElectionList> listsById)
    {
        var emptyListId = AusmittlungUuidV5.BuildProportionalElectionEmptyList(importData.ProportionalElectionId);
        if (!electionBallot.ListId.HasValue || electionBallot.ListId.Value == emptyListId)
        {
            return (null, null);
        }

        if (!listsById.TryGetValue(electionBallot.ListId.Value, out var list) ||
            list.ProportionalElectionId != importData.ProportionalElectionId)
        {
            throw new EntityNotFoundException(nameof(ProportionalElectionList), electionBallot.ListId);
        }

        var listResult = importData.GetOrAddListResult(electionBallot.ListId.Value);
        return (list, listResult);
    }

    private void ProcessModifiedBallot(
        int missingPositions,
        ProportionalElectionResultImport importData,
        ProportionalElectionListResultImport? listResult,
        EVotingElectionBallot electionBallot,
        IReadOnlyDictionary<Guid, ProportionalElectionCandidate> candidatesById)
    {
        var candidatesVoteCountOnThisBallot = new Dictionary<Guid, int>();

        var emptyPositions = missingPositions;
        foreach (var position in electionBallot.Positions)
        {
            if (position.IsEmpty)
            {
                emptyPositions++;
                continue;
            }

            ProcessModifiedBallotPosition(importData, electionBallot, position, candidatesVoteCountOnThisBallot, candidatesById);
        }

        // Note: Only lists with a party should be counted towards CountOfModifiedLists.
        // This was a misunderstanding in an earlier version and is kept for backwards compatibility.
        // The correct data is calculated in the event processor.
        importData.CountOfModifiedLists++;
        if (listResult == null)
        {
            importData.CountOfListsWithoutParty++;
            importData.CountOfBlankRowsOnListsWithoutParty += emptyPositions;
        }
        else
        {
            listResult.ModifiedListsCount++;
            listResult.ModifiedListBlankRowsCount += emptyPositions;
        }
    }

    private void ProcessModifiedBallotPosition(
        ProportionalElectionResultImport importData,
        EVotingElectionBallot electionBallot,
        EVotingElectionBallotPosition position,
        IDictionary<Guid, int> candidatesVoteCountOnThisBallot,
        IReadOnlyDictionary<Guid, ProportionalElectionCandidate> candidatesById)
    {
        if (position.IsWriteIn)
        {
            throw new ValidationException("proportional election ballot position cannot contain write-ins");
        }

        if (!position.CandidateId.HasValue)
        {
            throw new ValidationException("encountered non-empty proportional election ballot position without a candidate id");
        }

        var candidateId = position.CandidateId.Value;
        if (!candidatesById.TryGetValue(candidateId, out var candidate)
            || candidate.ProportionalElectionList.ProportionalElectionId != importData.ProportionalElectionId)
        {
            throw new EntityNotFoundException(nameof(ProportionalElectionCandidate), candidateId);
        }

        var listResult = importData.GetOrAddListResult(candidate.ProportionalElectionListId);
        listResult.ModifiedListVotesCount++;

        var candidateResult = importData.GetOrAddCandidateResult(candidateId);
        candidateResult.ModifiedListVotesCount++;
        var voteSourceListId = electionBallot.ListId;
        var emptyListId = AusmittlungUuidV5.BuildProportionalElectionEmptyList(importData.ProportionalElectionId);

        // vote source list id should be an empty guid for an empty list
        if (voteSourceListId == emptyListId)
        {
            voteSourceListId = Guid.Empty;
        }

        candidateResult.AddVoteSourceVote(voteSourceListId);

        // votes on ballots without a list don't count
        if (candidate.ProportionalElectionListId != electionBallot.ListId && electionBallot.ListId.HasValue && electionBallot.ListId.Value != emptyListId)
        {
            candidateResult.CountOfVotesOnOtherLists++;
            listResult.ListVotesCountOnOtherLists++;
        }

        var newCandidateVoteCountOnThisBallot = candidatesVoteCountOnThisBallot.AddOrUpdate(candidateId, () => 1, x => x + 1);
        if (newCandidateVoteCountOnThisBallot == 2)
        {
            candidateResult.CountOfVotesFromAccumulations++;
        }
        else if (newCandidateVoteCountOnThisBallot > 2)
        {
            throw new ValidationException($"candidate with id {candidateId} was found more than 2 times on a single ballot");
        }
    }

    private void ProcessUnmodifiedBallot(
        ProportionalElectionResultImport importData,
        ProportionalElectionList list,
        ProportionalElectionListResultImport listResult)
    {
        importData.CountOfUnmodifiedLists++;
        listResult.UnmodifiedListsCount++;
        listResult.UnmodifiedListBlankRowsCount += list.BlankRowCount;

        foreach (var candidate in list.ProportionalElectionCandidates)
        {
            var candidateResult = importData.GetOrAddCandidateResult(candidate.Id);
            listResult.UnmodifiedListVotesCount++;
            candidateResult.UnmodifiedListVotesCount++;

            if (candidate.Accumulated)
            {
                listResult.UnmodifiedListVotesCount++;
                candidateResult.CountOfVotesFromAccumulations++;
                candidateResult.UnmodifiedListVotesCount++;
            }
        }
    }
}
