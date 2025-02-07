// (c) Copyright by Abraxas Informatik AG
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
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionResult> _secondaryResultRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _secondaryMajorityElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResultBallot> _ballotRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionResultBallot> _secondaryBallotRepo;
    private readonly SimpleCountingCircleResultRepo _simpleResultRepo;
    private readonly MajorityElectionBallotGroupResultBuilder _ballotGroupResultBuilder;
    private readonly MajorityElectionCandidateResultBuilder _candidateResultBuilder;
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;

    public MajorityElectionResultBuilder(
        MajorityElectionResultRepo resultRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionResult> secondaryResultRepo,
        IDbRepository<DataContext, SecondaryMajorityElection> secondaryMajorityElectionRepo,
        IDbRepository<DataContext, MajorityElectionResultBallot> ballotRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionResultBallot> secondaryBallotRepo,
        SimpleCountingCircleResultRepo simpleResultRepo,
        MajorityElectionBallotGroupResultBuilder ballotGroupResultBuilder,
        MajorityElectionCandidateResultBuilder candidateResultBuilder,
        DataContext dataContext,
        IMapper mapper)
    {
        _resultRepo = resultRepo;
        _secondaryResultRepo = secondaryResultRepo;
        _secondaryMajorityElectionRepo = secondaryMajorityElectionRepo;
        _ballotRepo = ballotRepo;
        _secondaryBallotRepo = secondaryBallotRepo;
        _simpleResultRepo = simpleResultRepo;
        _ballotGroupResultBuilder = ballotGroupResultBuilder;
        _candidateResultBuilder = candidateResultBuilder;
        _dataContext = dataContext;
        _mapper = mapper;
    }

    internal async Task RebuildForElection(Guid electionId, Guid domainOfInfluenceId, bool testingPhaseEnded, Guid contestId)
    {
        await _resultRepo.Rebuild(electionId, domainOfInfluenceId, testingPhaseEnded, contestId);
        var results = await _resultRepo.Query()
            .AsSplitQuery()
            .Where(x => x.MajorityElectionId == electionId)
            .Select(x => new
            {
                x.Id,
                BallotGroupIds = x.BallotGroupResults.Select(r => r.BallotGroupId),
                CandidateIds = x.CandidateResults.Select(r => r.CandidateId),
                SecondaryResults = x.SecondaryMajorityElectionResults.Select(r => new
                {
                    r.Id,
                    r.SecondaryMajorityElectionId,
                    CandidateIds = r.CandidateResults.Select(c => c.CandidateId).ToList(),
                })
                .ToList(),
            })
            .ToListAsync();

        var missingBallotGroupResults = await _ballotGroupResultBuilder.BuildMissing(electionId, results.ToDictionary(x => x.Id, x => x.BallotGroupIds));
        _dataContext.MajorityElectionBallotGroupResults.AddRange(missingBallotGroupResults);

        var missingCandidateResults = await _candidateResultBuilder.BuildMissing(electionId, results.ToDictionary(x => x.Id, x => x.CandidateIds));
        _dataContext.MajorityElectionCandidateResults.AddRange(missingCandidateResults);

        var existingSecondaryResultByResultId = results.ToDictionary(
            x => x.Id,
            x => x.SecondaryResults.ConvertAll(r => new SecondaryResultIds(r.SecondaryMajorityElectionId, r.Id, r.CandidateIds)));
        var (missingSecondaryResults, missingSecondaryCandidateResults) = await BuildMissingSecondaryMajorityElectionResults(electionId, existingSecondaryResultByResultId);
        _dataContext.SecondaryMajorityElectionResults.AddRange(missingSecondaryResults);
        _dataContext.SecondaryMajorityElectionCandidateResults.AddRange(missingSecondaryCandidateResults);

        await _dataContext.SaveChangesAsync();

        if (missingSecondaryResults.Count == 0)
        {
            return;
        }

        var secondaryElectionIds = missingSecondaryResults.Select(r => r.SecondaryMajorityElectionId).ToHashSet();
        var secondaryMajorityElections = await _secondaryMajorityElectionRepo.Query()
            .Include(x => x.PrimaryMajorityElection)
            .Where(x => secondaryElectionIds.Contains(x.Id))
            .ToListAsync();

        foreach (var secondaryMajorityElection in secondaryMajorityElections)
        {
            await _simpleResultRepo.Sync(secondaryMajorityElection.Id, secondaryMajorityElection.DomainOfInfluenceId, false);
        }
    }

    internal async Task InitializeSecondaryElection(Guid electionId, Guid secondaryElectionId)
    {
        var resultsToUpdate = await _resultRepo.Query()
            .AsSplitQuery()
            .Where(x => x.MajorityElectionId == electionId &&
                        x.SecondaryMajorityElectionResults.All(y => y.SecondaryMajorityElectionId != secondaryElectionId))
            .Select(x => new
            {
                x.Id,
                SecondaryResults = x.SecondaryMajorityElectionResults.Select(r => new
                {
                    r.Id,
                    r.SecondaryMajorityElectionId,
                    CandidateIds = r.CandidateResults.Select(c => c.CandidateId),
                }),
            })
            .ToListAsync();

        var existingSecondaryResultByResultId = resultsToUpdate.ToDictionary(
            x => x.Id,
            x => x.SecondaryResults.Select(r => new SecondaryResultIds(r.SecondaryMajorityElectionId, r.Id, r.CandidateIds)).ToList());
        var (missingSecondaryResults, missingSecondaryCandidateResults) = await BuildMissingSecondaryMajorityElectionResults(electionId, existingSecondaryResultByResultId);
        _dataContext.SecondaryMajorityElectionResults.AddRange(missingSecondaryResults);
        _dataContext.SecondaryMajorityElectionCandidateResults.AddRange(missingSecondaryCandidateResults);
        await _dataContext.SaveChangesAsync();
    }

    internal async Task ResetForElection(Guid electionId, Guid domainOfInfluenceId, Guid contestId)
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

        await RebuildForElection(electionId, domainOfInfluenceId, true, contestId);
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

        await ResetConventionalResult(result, true);
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

        await ResetConventionalResult(electionResult, false);
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

        await UpdateSimpleResult(electionResult.Id, electionResult.CountOfVoters);
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

    internal async Task ResetIndividualVoteCounts(Guid electionId)
    {
        var ccResults = await _resultRepo.Query()
            .AsSplitQuery()
            .AsTracking()
            .Where(r => r.MajorityElectionId == electionId)
            .Include(r => r.Bundles)
            .ThenInclude(b => b.Ballots)
            .ToListAsync();

        foreach (var ccResult in ccResults)
        {
            ccResult.ConventionalSubTotal.IndividualVoteCount = null;
            ccResult.EVotingSubTotal.IndividualVoteCount = 0;
        }

        foreach (var ballot in ccResults.SelectMany(r => r.Bundles).SelectMany(b => b.Ballots))
        {
            ballot.IndividualVoteCount = 0;
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task ResetSecondaryIndividualVoteCounts(Guid electionId)
    {
        var ccResults = await _secondaryResultRepo.Query()
            .AsSplitQuery()
            .AsTracking()
            .Where(r => r.SecondaryMajorityElectionId == electionId)
            .Include(r => r.ResultBallots)
            .ToListAsync();

        foreach (var ccResult in ccResults)
        {
            ccResult.ConventionalSubTotal.IndividualVoteCount = null;
            ccResult.EVotingSubTotal.IndividualVoteCount = 0;
        }

        foreach (var ballot in ccResults.SelectMany(r => r.ResultBallots))
        {
            ballot.IndividualVoteCount = 0;
        }

        await _dataContext.SaveChangesAsync();
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

    private async Task ResetConventionalResult(MajorityElectionResult electionResult, bool includeCountOfVoters)
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

        if (includeCountOfVoters)
        {
            await ResetSimpleResult(electionResult.Id);
        }
    }

    private async Task<MissingSecondaryResults> BuildMissingSecondaryMajorityElectionResults(
        Guid electionId,
        Dictionary<Guid, List<SecondaryResultIds>> secondaryResultIdsByResultId)
    {
        var secondaryElectionIds = await _secondaryMajorityElectionRepo.Query()
            .Where(e => e.PrimaryMajorityElectionId == electionId)
            .Select(e => new
            {
                ElectionId = e.Id,
                CandidateIds = e.Candidates.Select(c => c.Id).ToList(),
            })
            .ToListAsync();

        var missingSecondaryResults = new List<SecondaryMajorityElectionResult>();
        var missingSecondaryCandidateResults = new List<SecondaryMajorityElectionCandidateResult>();
        if (secondaryElectionIds.Count == 0)
        {
            return new MissingSecondaryResults(missingSecondaryResults, missingSecondaryCandidateResults);
        }

        var secondaryCandidateIdsByElectionId =
            secondaryElectionIds.ToDictionary(x => x.ElectionId, x => x.CandidateIds);
        foreach (var (resultId, secondaryResultIdList) in secondaryResultIdsByResultId)
        {
            var secondaryResultsToAdd = secondaryElectionIds.Select(x => x.ElectionId).Except(secondaryResultIdList.Select(x => x.SecondaryElectionId))
                .Select(x => new SecondaryMajorityElectionResult { SecondaryMajorityElectionId = x, PrimaryResultId = resultId, Id = Guid.NewGuid() })
                .ToList();
            missingSecondaryResults.AddRange(secondaryResultsToAdd);
            secondaryResultIdList.AddRange(secondaryResultsToAdd.Select(x => new SecondaryResultIds(x.SecondaryMajorityElectionId, x.Id, new List<Guid>())));

            foreach (var secondaryResultIds in secondaryResultIdList)
            {
                var toAdd = secondaryCandidateIdsByElectionId[secondaryResultIds.SecondaryElectionId].Except(secondaryResultIds.CandidateIds)
                    .Select(x => new SecondaryMajorityElectionCandidateResult { CandidateId = x, ElectionResultId = secondaryResultIds.SecondaryResultId });
                missingSecondaryCandidateResults.AddRange(toAdd);
            }
        }

        return new MissingSecondaryResults(missingSecondaryResults, missingSecondaryCandidateResults);
    }

    private async Task UpdateSimpleResult(Guid resultId, PoliticalBusinessNullableCountOfVoters countOfVoters)
    {
        var simpleResult = await _simpleResultRepo.GetByKey(resultId)
                           ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), resultId);

        simpleResult.CountOfVoters = countOfVoters;
        await _simpleResultRepo.Update(simpleResult);
    }

    private async Task ResetSimpleResult(Guid resultId)
    {
        var simpleResult = await _simpleResultRepo.GetByKey(resultId)
                           ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), resultId);

        if (simpleResult.CountOfVoters == null)
        {
            return;
        }

        simpleResult.CountOfVoters.ConventionalReceivedBallots = 0;
        simpleResult.CountOfVoters.ConventionalBlankBallots = 0;
        simpleResult.CountOfVoters.ConventionalInvalidBallots = 0;
        simpleResult.CountOfVoters.ConventionalAccountedBallots = 0;

        await _simpleResultRepo.Update(simpleResult);
    }

    private record SecondaryResultIds(Guid SecondaryElectionId, Guid SecondaryResultId, IEnumerable<Guid> CandidateIds);

    private record MissingSecondaryResults(
        List<SecondaryMajorityElectionResult> SecondaryResults,
        List<SecondaryMajorityElectionCandidateResult> CandidateResults);
}
