// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class MajorityElectionBallotGroupResultBuilder
{
    private readonly IDbRepository<DataContext, MajorityElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, MajorityElectionBallotGroup> _ballotGroupRepo;
    private readonly DataContext _dataContext;
    private readonly MajorityElectionCandidateResultBuilder _candidateResultBuilder;

    public MajorityElectionBallotGroupResultBuilder(
        IDbRepository<DataContext, MajorityElectionResult> resultRepo,
        IDbRepository<DataContext, MajorityElectionBallotGroup> ballotGroupRepo,
        DataContext dataContext,
        MajorityElectionCandidateResultBuilder candidateResultBuilder)
    {
        _resultRepo = resultRepo;
        _ballotGroupRepo = ballotGroupRepo;
        _dataContext = dataContext;
        _candidateResultBuilder = candidateResultBuilder;
    }

    internal async Task Initialize(Guid electionId, Guid ballotGroupId)
    {
        var resultsToUpdate = await _resultRepo.Query()
            .AsTracking()
            .Where(x => x.MajorityElectionId == electionId &&
                        x.BallotGroupResults.All(y => y.BallotGroupId != ballotGroupId))
            .Include(x => x.BallotGroupResults)
            .ToListAsync();

        foreach (var result in resultsToUpdate)
        {
            result.BallotGroupResults.Add(new MajorityElectionBallotGroupResult
            {
                BallotGroupId = ballotGroupId,
            });
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task UpdateBallotGroupAndCandidateResults(
        Guid resultId,
        IDictionary<Guid, int> ballotGroupCounts)
    {
        var electionResult = await _resultRepo.Query()
                                 .AsTracking()
                                 .AsSplitQuery()
                                 .Include(x => x.SecondaryMajorityElectionResults)
                                 .Include(x => x.BallotGroupResults).ThenInclude(x => x.BallotGroup.Entries).ThenInclude(c => c.Candidates)
                                 .FirstOrDefaultAsync(x => x.Id == resultId)
                             ?? throw new EntityNotFoundException(resultId);

        var byBallotGroupId = electionResult.BallotGroupResults
            .ToDictionary(x => x.BallotGroupId);
        var secondaryResultsByElectionId = electionResult.SecondaryMajorityElectionResults
            .ToDictionary(x => x.SecondaryMajorityElectionId);

        var primaryCandidateDeltas = new Dictionary<Guid, int>();
        var secondaryMajorityCandidateDeltas = new Dictionary<Guid, int>();

        foreach (var (ballotGroupId, count) in ballotGroupCounts)
        {
            if (!byBallotGroupId.TryGetValue(ballotGroupId, out var ballotGroupResult))
            {
                throw new EntityNotFoundException(ballotGroupId);
            }

            var countDelta = count - ballotGroupResult.VoteCount;
            ballotGroupResult.VoteCount = count;

            AdjustResultVoteCounts(ballotGroupResult, electionResult, secondaryResultsByElectionId, countDelta);

            SumCandidateDeltas(primaryCandidateDeltas, ballotGroupResult.BallotGroup, c => c.PrimaryElectionCandidateId, countDelta);
            SumCandidateDeltas(secondaryMajorityCandidateDeltas, ballotGroupResult.BallotGroup, c => c.SecondaryElectionCandidateId, countDelta);
        }

        await _dataContext.SaveChangesAsync();

        await _candidateResultBuilder.AdjustConventionalVotes(resultId, primaryCandidateDeltas);
        await _candidateResultBuilder.AdjustConventionalSecondaryMajorityVotes(resultId, secondaryMajorityCandidateDeltas);
    }

    internal async Task UpdateCandidates(
        Guid ballotGroupId,
        IReadOnlyDictionary<Guid, int> individualVoteCountsByEntryId,
        IReadOnlyDictionary<Guid, List<Guid>> candidatesByEntryId)
    {
        var ballotGroupEntries = await _dataContext
            .MajorityElectionBallotGroupEntries
            .AsTracking()
            .Include(e => e.Candidates)
            .Where(e => e.BallotGroupId == ballotGroupId)
            .ToListAsync();

        foreach (var ballotGroupEntry in ballotGroupEntries)
        {
            ballotGroupEntry.IndividualCandidatesVoteCount = individualVoteCountsByEntryId.GetValueOrDefault(ballotGroupEntry.Id, 0);

            if (!candidatesByEntryId.TryGetValue(ballotGroupEntry.Id, out var candidates))
            {
                ballotGroupEntry.Candidates.Clear();
                continue;
            }

            var existingCandidatesById = ballotGroupEntry.Candidates
                .ToDictionary(
                    x => x.PrimaryElectionCandidateId
                    ?? x.SecondaryElectionCandidateId
                    ?? throw new InvalidOperationException($"Failed to build candidates dictionary for majority election ballot group entry with id '{ballotGroupEntry.Id}' because primary and secondary election candidate id is null"));

            var candidateEntriesToAdd = candidates
                .Where(cId => !existingCandidatesById.Remove(cId))
                .Select(cId => new MajorityElectionBallotGroupEntryCandidate
                {
                    BallotGroupEntryId = ballotGroupEntry.Id,
                    PrimaryElectionCandidateId = ballotGroupEntry.PrimaryMajorityElectionId.HasValue
                        ? cId
                        : null,
                    SecondaryElectionCandidateId = ballotGroupEntry.PrimaryMajorityElectionId.HasValue
                        ? null
                        : cId,
                });

            foreach (var candidateToAdd in candidateEntriesToAdd)
            {
                ballotGroupEntry.Candidates.Add(candidateToAdd);
            }

            foreach (var candidateToRemove in existingCandidatesById.Values)
            {
                ballotGroupEntry.Candidates.Remove(candidateToRemove);
            }
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task AddMissing(Guid electionId, IEnumerable<MajorityElectionResult> results)
    {
        var ballotGroupIds = await _ballotGroupRepo.Query()
            .Where(l => l.MajorityElectionId == electionId)
            .Select(l => l.Id)
            .ToListAsync();

        if (ballotGroupIds.Count == 0)
        {
            return;
        }

        foreach (var result in results)
        {
            AddMissing(result, ballotGroupIds);
        }
    }

    private void SumCandidateDeltas(
        IDictionary<Guid, int> deltasByCandidateId,
        MajorityElectionBallotGroup ballotGroup,
        Func<MajorityElectionBallotGroupEntryCandidate, Guid?> candidateIdSelector,
        int countDelta)
    {
        var candidateIds = ballotGroup.Entries
            .SelectMany(x => x.Candidates)
            .Select(candidateIdSelector)
            .Where(cId => cId.HasValue && cId.Value != Guid.Empty)
            .Select(cId => cId!.Value);
        foreach (var cId in candidateIds)
        {
            if (!deltasByCandidateId.TryAdd(cId, countDelta))
            {
                deltasByCandidateId[cId] += countDelta;
            }
        }
    }

    private void AddMissing(MajorityElectionResult result, IEnumerable<Guid> ballotGroupIds)
    {
        var toAdd = ballotGroupIds.Except(result.BallotGroupResults.Select(x => x.BallotGroupId))
            .Select(x => new MajorityElectionBallotGroupResult { BallotGroupId = x })
            .ToList();
        foreach (var element in toAdd)
        {
            result.BallotGroupResults.Add(element);
        }
    }

    private void AdjustResultVoteCounts(
        MajorityElectionBallotGroupResult ballotGroupResult,
        MajorityElectionResult electionResult,
        IDictionary<Guid, SecondaryMajorityElectionResult> secondaryResultsByElectionId,
        int countDelta)
    {
        foreach (var entry in ballotGroupResult.BallotGroup.Entries)
        {
            if (entry.PrimaryMajorityElectionId.HasValue && entry.PrimaryMajorityElectionId != Guid.Empty)
            {
                electionResult.ConventionalSubTotal.IndividualVoteCount += countDelta * entry.IndividualCandidatesVoteCount;
                electionResult.ConventionalSubTotal.EmptyVoteCountExclWriteIns += countDelta * entry.BlankRowCount;
                electionResult.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual += countDelta * entry.Candidates.Count;
            }
            else
            {
                if (!secondaryResultsByElectionId.TryGetValue(entry.SecondaryMajorityElectionId!.Value, out var secondaryResult))
                {
                    throw new EntityNotFoundException(entry.SecondaryMajorityElectionId);
                }

                secondaryResult.ConventionalSubTotal.IndividualVoteCount += countDelta * entry.IndividualCandidatesVoteCount;
                secondaryResult.ConventionalSubTotal.EmptyVoteCountExclWriteIns += countDelta * entry.BlankRowCount;
                secondaryResult.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual += countDelta * entry.Candidates.Count;
            }
        }
    }
}
