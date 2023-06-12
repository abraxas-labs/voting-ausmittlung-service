// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Utils;

public class MajorityElectionResultBuilder
{
    private readonly MajorityElectionResultRepo _resultRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _secondaryMajorityElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResultBallot> _ballotRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionResultBallot> _secondaryBallotRepo;
    private readonly MajorityElectionBallotGroupResultBuilder _ballotGroupResultBuilder;
    private readonly MajorityElectionCandidateResultBuilder _candidateResultBuilder;
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;

    public MajorityElectionResultBuilder(
        MajorityElectionResultRepo resultRepo,
        IDbRepository<DataContext, SecondaryMajorityElection> secondaryMajorityElectionRepo,
        IDbRepository<DataContext, MajorityElectionResultBallot> ballotRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionResultBallot> secondaryBallotRepo,
        MajorityElectionBallotGroupResultBuilder ballotGroupResultBuilder,
        MajorityElectionCandidateResultBuilder candidateResultBuilder,
        DataContext dataContext,
        IMapper mapper)
    {
        _resultRepo = resultRepo;
        _secondaryMajorityElectionRepo = secondaryMajorityElectionRepo;
        _ballotRepo = ballotRepo;
        _secondaryBallotRepo = secondaryBallotRepo;
        _ballotGroupResultBuilder = ballotGroupResultBuilder;
        _candidateResultBuilder = candidateResultBuilder;
        _dataContext = dataContext;
        _mapper = mapper;
    }

    internal async Task RebuildForElection(Guid electionId, Guid domainOfInfluenceId, bool testingPhaseEnded)
    {
        await _resultRepo.Rebuild(electionId, domainOfInfluenceId, testingPhaseEnded);
        var results = await _resultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Where(x => x.MajorityElectionId == electionId)
            .Include(x => x.BallotGroupResults)
            .Include(x => x.CandidateResults)
            .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
            .ToListAsync();

        await _ballotGroupResultBuilder.AddMissing(electionId, results);
        await _candidateResultBuilder.AddMissing(electionId, results);
        await AddMissingSecondaryMajorityElectionResults(electionId, results);

        await _dataContext.SaveChangesAsync();
    }

    internal async Task InitializeSecondaryElection(Guid electionId, Guid secondaryElectionId)
    {
        var resultsToUpdate = await _resultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Where(x => x.MajorityElectionId == electionId &&
                        x.SecondaryMajorityElectionResults.All(y => y.SecondaryMajorityElectionId != secondaryElectionId))
            .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
            .ToListAsync();
        await AddMissingSecondaryMajorityElectionResults(electionId, resultsToUpdate);
        await _dataContext.SaveChangesAsync();
    }

    internal async Task ResetForElection(Guid electionId, Guid domainOfInfluenceId)
    {
        var existingElectionResults = await _resultRepo.Query()
            .Include(x => x.CountingCircle)
            .Where(er => er.MajorityElectionId == electionId)
            .ToListAsync();

        await _resultRepo.DeleteRangeByKey(existingElectionResults.Select(x => x.Id));
        await _resultRepo.CreateRange(existingElectionResults.Select(r => new MajorityElectionResult
        {
            Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(electionId, r.CountingCircle.BasisCountingCircleId, true),
            CountingCircleId = r.CountingCircleId,
            MajorityElectionId = r.MajorityElectionId,
        }));

        await RebuildForElection(electionId, domainOfInfluenceId, true);
    }

    internal async Task ResetConventionalResultInTestingPhase(Guid resultId)
    {
        var result = await _resultRepo
            .Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.BallotGroupResults)
            .Include(x => x.Bundles)
            .Include(x => x.CandidateResults)
            .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
            .FirstOrDefaultAsync(x => x.Id == resultId)
            ?? throw new EntityNotFoundException(resultId);

        ResetConventionalResult(result, true);
        await _dataContext.SaveChangesAsync();
    }

    internal async Task UpdateResultEntryAndResetConventionalResult(
        Guid resultId,
        SharedProto.MajorityElectionResultEntry resultEntry,
        MajorityElectionResultEntryParamsEventData? resultEntryParams)
    {
        var electionResult = await _resultRepo
                                 .Query()
                                 .AsTracking()
                                 .AsSplitQuery()
                                 .Include(x => x.BallotGroupResults)
                                 .Include(x => x.Bundles)
                                 .Include(x => x.CandidateResults)
                                 .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
                                 .FirstOrDefaultAsync(x => x.Id == resultId)
                             ?? throw new EntityNotFoundException(resultId);

        electionResult.Entry = _mapper.Map<MajorityElectionResultEntry>(resultEntry);
        if (resultEntryParams == null)
        {
            electionResult.EntryParams = null;
        }
        else
        {
            electionResult.EntryParams = new MajorityElectionResultEntryParams();
            _mapper.Map(resultEntryParams, electionResult.EntryParams);

            // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
            if (electionResult.EntryParams.ReviewProcedure == MajorityElectionReviewProcedure.Unspecified)
            {
                electionResult.EntryParams.ReviewProcedure = MajorityElectionReviewProcedure.Electronically;
            }
        }

        ResetConventionalResult(electionResult, false);
        await _dataContext.SaveChangesAsync();
    }

    internal async Task UpdateConventionalResults(MajorityElectionCandidateResultsEntered data)
    {
        var resultId = GuidParser.Parse(data.ElectionResultId);
        var electionResult = await _resultRepo.Query()
                                 .AsSplitQuery()
                                 .AsTracking()
                                 .Include(x => x.CandidateResults)
                                 .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
                                 .FirstOrDefaultAsync(x => x.Id == resultId)
                             ?? throw new EntityNotFoundException(resultId);
        UpdateConventionalResults(electionResult, data);
        await _dataContext.SaveChangesAsync();
    }

    internal void UpdateConventionalResults(MajorityElectionResult electionResult, MajorityElectionCandidateResultsEntered data)
    {
        electionResult.ConventionalSubTotal.IndividualVoteCount = data.IndividualVoteCount;
        electionResult.ConventionalSubTotal.EmptyVoteCountExclWriteIns = data.EmptyVoteCount;
        electionResult.ConventionalSubTotal.InvalidVoteCount = data.InvalidVoteCount;
        electionResult.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual = data.CandidateResults.Sum(x => x.VoteCount.GetValueOrDefault());
        _candidateResultBuilder.SetConventionalVoteCountValues(
            electionResult.CandidateResults,
            data.CandidateResults.ToDictionary(c => GuidParser.Parse(c.CandidateId), c => c.VoteCount));
        UpdateSecondaryMajorityElectionResults(electionResult.SecondaryMajorityElectionResults, data.SecondaryElectionCandidateResults);
        electionResult.UpdateVoterParticipation();
    }

    internal async Task UpdateTotalCountOfBallotGroupVotes(Guid electionResultId, int sum)
    {
        var result = await _resultRepo.GetByKey(electionResultId)
                     ?? throw new EntityNotFoundException(electionResultId);
        result.ConventionalCountOfBallotGroupVotes = sum;
        await _resultRepo.Update(result);
    }

    internal async Task AddVoteCountsFromBundle(Guid electionResultId, Guid bundleId)
    {
        await AdjustConventionalVoteCountsForBundle(electionResultId, bundleId, 1);
        await AdjustSecondaryVoteCountsForBundle(electionResultId, bundleId, 1);
    }

    internal async Task RemoveVoteCountsFromBundle(Guid electionResultId, Guid bundleId)
    {
        await AdjustConventionalVoteCountsForBundle(electionResultId, bundleId, -1);
        await AdjustSecondaryVoteCountsForBundle(electionResultId, bundleId, -1);
    }

    private async Task AdjustConventionalVoteCountsForBundle(Guid electionResultId, Guid bundleId, int factor)
    {
        var electionResult = await _resultRepo.GetByKey(electionResultId)
                             ?? throw new EntityNotFoundException(electionResultId);

        var individualVoteCountSum = await _ballotRepo.Query()
            .Where(x => x.BundleId == bundleId)
            .SumAsync(x => x.IndividualVoteCount);
        electionResult.ConventionalSubTotal.IndividualVoteCount += individualVoteCountSum * factor;

        var invalidVoteCountSum = await _ballotRepo.Query()
            .Where(x => x.BundleId == bundleId)
            .SumAsync(x => x.InvalidVoteCount);
        electionResult.ConventionalSubTotal.InvalidVoteCount += invalidVoteCountSum * factor;

        var emptyVoteCountSum = await _ballotRepo.Query()
            .Where(x => x.BundleId == bundleId)
            .SumAsync(x => x.EmptyVoteCount);
        electionResult.ConventionalSubTotal.EmptyVoteCountExclWriteIns += emptyVoteCountSum * factor;

        var candidateVoteCount = await _ballotRepo.Query()
            .Where(x => x.BundleId == bundleId)
            .SelectMany(x => x.BallotCandidates)
            .CountAsync(x => x.Selected);
        electionResult.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual += candidateVoteCount * factor;

        await _resultRepo.Update(electionResult);
    }

    private async Task AdjustSecondaryVoteCountsForBundle(Guid electionResultId, Guid bundleId, int factor)
    {
        var electionResults = await _resultRepo.Query()
                                  .AsTracking()
                                  .Where(x => x.Id == electionResultId)
                                  .SelectMany(x => x.SecondaryMajorityElectionResults)
                                  .ToListAsync()
                              ?? throw new EntityNotFoundException(electionResultId);
        var voteCountSums = await _secondaryBallotRepo.Query()
            .Where(x => x.PrimaryBallot.BundleId == bundleId)
            .GroupBy(x => x.SecondaryMajorityElectionResult.SecondaryMajorityElectionId, (electionId, ballots) =>
                new
                {
                    SecondaryElectionId = electionId,
                    CountOfIndividualVotes = ballots.Sum(x => x.IndividualVoteCount),
                    CountOfEmptyVotes = ballots.Sum(x => x.EmptyVoteCount),
                    CountOfInvalidVotes = ballots.Sum(x => x.InvalidVoteCount),
                    CandidateVoteCount = ballots.Sum(b => b.CandidateVoteCountExclIndividual),
                })
            .ToDictionaryAsync(x => x.SecondaryElectionId, x => new { x.CountOfIndividualVotes, x.CountOfEmptyVotes, x.CountOfInvalidVotes, x.CandidateVoteCount });

        foreach (var electionResult in electionResults)
        {
            if (!voteCountSums.TryGetValue(electionResult.SecondaryMajorityElectionId, out var voteCounts))
            {
                continue;
            }

            electionResult.ConventionalSubTotal.IndividualVoteCount += voteCounts.CountOfIndividualVotes * factor;
            electionResult.ConventionalSubTotal.EmptyVoteCountExclWriteIns += voteCounts.CountOfEmptyVotes * factor;
            electionResult.ConventionalSubTotal.InvalidVoteCount += voteCounts.CountOfInvalidVotes * factor;
            electionResult.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual += voteCounts.CandidateVoteCount * factor;
        }

        await _dataContext.SaveChangesAsync();
    }

    private void UpdateSecondaryMajorityElectionResults(
        IEnumerable<SecondaryMajorityElectionResult> secondaryResults,
        IEnumerable<SecondaryMajorityElectionCandidateResultsEventData> updatedSecondaryResults)
    {
        var resultByElectionId = secondaryResults.ToDictionary(r => r.SecondaryMajorityElectionId);
        foreach (var updatedResult in updatedSecondaryResults)
        {
            if (!resultByElectionId.TryGetValue(Guid.Parse(updatedResult.SecondaryMajorityElectionId), out var result))
            {
                throw new EntityNotFoundException(updatedResult.SecondaryMajorityElectionId);
            }

            result.ConventionalSubTotal.IndividualVoteCount = updatedResult.IndividualVoteCount;
            result.ConventionalSubTotal.EmptyVoteCountExclWriteIns = updatedResult.EmptyVoteCount;
            result.ConventionalSubTotal.InvalidVoteCount = updatedResult.InvalidVoteCount;
            result.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual = updatedResult.CandidateResults.Sum(x => x.VoteCount.GetValueOrDefault());
            _candidateResultBuilder.SetConventionalVoteCountValues(
                result.CandidateResults,
                updatedResult.CandidateResults.ToDictionary(x => Guid.Parse(x.CandidateId), x => x.VoteCount));
        }
    }

    private void ResetConventionalResult(MajorityElectionResult electionResult, bool includeCountOfVoters)
    {
        electionResult.ConventionalCountOfDetailedEnteredBallots = 0;
        electionResult.ConventionalCountOfBallotGroupVotes = 0;
        electionResult.CountOfBundlesNotReviewedOrDeleted = 0;
        electionResult.Bundles.Clear();
        electionResult.ResetAllSubTotals(VotingDataSource.Conventional, includeCountOfVoters);
        electionResult.UpdateVoterParticipation();

        foreach (var ballotGroupResult in electionResult.BallotGroupResults)
        {
            ballotGroupResult.VoteCount = 0;
        }
    }

    private async Task AddMissingSecondaryMajorityElectionResults(
        Guid electionId,
        IEnumerable<MajorityElectionResult> results)
    {
        var secondaryElectionIds = await _secondaryMajorityElectionRepo.Query()
            .Where(e => e.PrimaryMajorityElectionId == electionId)
            .Select(e => new
            {
                ElectionId = e.Id,
                CandidateIds = e.Candidates.Select(c => c.Id).ToList(),
            })
            .ToListAsync();

        if (secondaryElectionIds.Count == 0)
        {
            return;
        }

        var secondaryCandidateIdsByElectionId =
            secondaryElectionIds.ToDictionary(x => x.ElectionId, x => x.CandidateIds);
        foreach (var result in results)
        {
            AddMissingItems(
                result.SecondaryMajorityElectionResults,
                secondaryElectionIds.Select(x => x.ElectionId),
                x => x.SecondaryMajorityElectionId,
                id => new SecondaryMajorityElectionResult { SecondaryMajorityElectionId = id });

            foreach (var secondaryResult in result.SecondaryMajorityElectionResults)
            {
                AddMissingItems(
                    secondaryResult.CandidateResults,
                    secondaryCandidateIdsByElectionId[secondaryResult.SecondaryMajorityElectionId],
                    x => x.CandidateId,
                    id => new SecondaryMajorityElectionCandidateResult { CandidateId = id });
            }
        }
    }

    private void AddMissingItems<T>(
        ICollection<T> elements,
        IEnumerable<Guid> allIds,
        Func<T, Guid> idSelector,
        Func<Guid, T> newProvider)
    {
        var toAdd = allIds.Except(elements.Select(idSelector))
            .Select(newProvider)
            .ToList();
        foreach (var element in toAdd)
        {
            elements.Add(element);
        }
    }
}
