// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionResultBuilder
{
    private readonly ProportionalElectionResultRepo _resultRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionList> _listRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionListResult> _listResultRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResultBallot> _resultBallotRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly IMapper _mapper;
    private readonly DataContext _dataContext;
    private readonly ProportionalElectionCandidateResultBuilder _candidateResultBuilder;

    public ProportionalElectionResultBuilder(
        ProportionalElectionResultRepo resultRepo,
        IDbRepository<DataContext, ProportionalElectionList> listRepo,
        IDbRepository<DataContext, ProportionalElectionListResult> listResultRepo,
        IDbRepository<DataContext, ProportionalElectionResultBallot> resultBallotRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        ProportionalElectionCandidateResultBuilder candidateResultBuilder,
        DataContext dataContext,
        IMapper mapper)
    {
        _resultRepo = resultRepo;
        _listRepo = listRepo;
        _listResultRepo = listResultRepo;
        _resultBallotRepo = resultBallotRepo;
        _simpleResultRepo = simpleResultRepo;
        _candidateResultBuilder = candidateResultBuilder;
        _dataContext = dataContext;
        _mapper = mapper;
    }

    internal async Task RebuildForElection(Guid electionId, Guid domainOfInfluenceId, bool testingPhaseEnded, Guid contestId)
    {
        await _resultRepo.Rebuild(electionId, domainOfInfluenceId, testingPhaseEnded, contestId);
        var results = await _resultRepo.Query()
            .AsSplitQuery()
            .Where(x => x.ProportionalElectionId == electionId)
            .Select(x => new
            {
                x.Id,
                UnmodifiedListResultListIds = x.UnmodifiedListResults.Select(r => r.ListId),
                ListResult = x.ListResults.Select(r => new
                {
                    r.Id,
                    r.ListId,
                    CandidateIds = r.CandidateResults.Select(cr => cr.CandidateId).ToList(),
                })
                .ToList(),
            })
            .ToListAsync();

        var listAndCandidateIds = await _listRepo.Query()
            .Where(l => l.ProportionalElectionId == electionId)
            .Select(x => new
            {
                x.Id,
                CandidateIds = x.ProportionalElectionCandidates.Select(c => c.Id),
            })
            .ToListAsync();

        if (listAndCandidateIds.Count == 0)
        {
            return;
        }

        var listIds = listAndCandidateIds.ConvertAll(l => l.Id);
        var candidateIdsByListId = listAndCandidateIds.ToDictionary(l => l.Id, l => l.CandidateIds.ToList());
        foreach (var result in results)
        {
            var unmodifiedResultsToAdd = listIds
                .Except(result.UnmodifiedListResultListIds)
                .Select(listId => new ProportionalElectionUnmodifiedListResult { ListId = listId, ResultId = result.Id });
            _dataContext.ProportionalElectionUnmodifiedListResults.AddRange(unmodifiedResultsToAdd);

            var listResultsToAdd = listIds
                .Except(result.ListResult.Select(x => x.ListId))
                .Select(listId => new ProportionalElectionListResult { ListId = listId, ResultId = result.Id })
                .ToList();
            _dataContext.ProportionalElectionListResults.AddRange(listResultsToAdd);
            result.ListResult.AddRange(listResultsToAdd.Select(r => new { r.Id, r.ListId, CandidateIds = new List<Guid>() }));

            foreach (var listResult in result.ListResult)
            {
                var candidateIds = candidateIdsByListId[listResult.ListId];
                var toAdd = candidateIds
                    .Except(listResult.CandidateIds)
                    .Select(candidateId => new ProportionalElectionCandidateResult { CandidateId = candidateId, ListResultId = listResult.Id });
                _dataContext.ProportionalElectionCandidateResults.AddRange(toAdd);
            }
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task InitializeForList(Guid electionId, Guid listId)
    {
        var resultIds = await _resultRepo.Query()
            .Where(x => x.ProportionalElectionId == electionId)
            .Select(x => x.Id)
            .ToListAsync();

        var unmodifiedResultsToAdd = resultIds.Select(x => new ProportionalElectionUnmodifiedListResult { ListId = listId, ResultId = x });
        _dataContext.ProportionalElectionUnmodifiedListResults.AddRange(unmodifiedResultsToAdd);

        var listResultsToAdd = resultIds.Select(x => new ProportionalElectionListResult { ListId = listId, ResultId = x });
        _dataContext.ProportionalElectionListResults.AddRange(listResultsToAdd);

        await _dataContext.SaveChangesAsync();
    }

    internal async Task ResetForElection(Guid electionId, Guid domainOfInfluenceId, Guid contestId)
    {
        var existingElectionResults = await _resultRepo.Query()
            .Where(er => er.ProportionalElectionId == electionId)
            .Select(x => new
            {
                x.Id,
                x.CountingCircle.BasisCountingCircleId,
                x.CountingCircleId,
            })
            .ToListAsync();

        await _resultRepo.DeleteRangeByKey(existingElectionResults.Select(x => x.Id));
        await _resultRepo.CreateRange(existingElectionResults.Select(r => new ProportionalElectionResult
        {
            Id = AusmittlungUuidV5.BuildPoliticalBusinessResult(electionId, r.BasisCountingCircleId, true),
            CountingCircleId = r.CountingCircleId,
            ProportionalElectionId = electionId,
        }));

        await RebuildForElection(electionId, domainOfInfluenceId, true, contestId);
    }

    internal async Task ResetConventionalResultInTestingPhase(Guid resultId)
    {
        var electionResult = await _resultRepo
            .Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.UnmodifiedListResults)
            .Include(x => x.ListResults).ThenInclude(x => x.CandidateResults).ThenInclude(x => x.VoteSources)
            .Include(x => x.Bundles)
            .FirstOrDefaultAsync(x => x.Id == resultId)
            ?? throw new EntityNotFoundException(resultId);

        await ResetConventionalResult(electionResult, true);
        await _dataContext.SaveChangesAsync();
    }

    internal async Task UpdateResultEntryAndResetConventionalResults(
        Guid resultId,
        ProportionalElectionResultEntryParamsEventData resultEntryParams)
    {
        var electionResult = await _resultRepo
                                 .Query()
                                 .AsTracking()
                                 .AsSplitQuery()
                                 .Include(x => x.UnmodifiedListResults)
                                 .Include(x => x.ListResults).ThenInclude(x => x.CandidateResults).ThenInclude(x => x.VoteSources)
                                 .Include(x => x.Bundles)
                                 .FirstOrDefaultAsync(x => x.Id == resultId)
                             ?? throw new EntityNotFoundException(resultId);

        _mapper.Map(resultEntryParams, electionResult.EntryParams);

        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (electionResult.EntryParams.ReviewProcedure == ProportionalElectionReviewProcedure.Unspecified)
        {
            electionResult.EntryParams.ReviewProcedure = ProportionalElectionReviewProcedure.Electronically;
        }

        await ResetConventionalResult(electionResult, false);
        await _dataContext.SaveChangesAsync();
    }

    internal async Task UpdateVotesFromUnmodifiedListResult(ProportionalElectionListResult listResult, int voteCount, int voteCountDeltaToPrevious)
    {
        foreach (var candidateResult in listResult.CandidateResults)
        {
            var accumulationModifier = candidateResult.Candidate.Accumulated ? 2 : 1;
            candidateResult.ConventionalSubTotal.UnmodifiedListVotesCount = voteCount * accumulationModifier;

            if (candidateResult.Candidate.Accumulated)
            {
                // Since accumulations are also possible with modified list results, we cannot simply set this value to the vote count
                // Instead, we add the delta vote count from the previous "unmodified list result" that was entered
                candidateResult.ConventionalSubTotal.CountOfVotesFromAccumulations += voteCountDeltaToPrevious;
            }
        }

        listResult.ConventionalSubTotal.UnmodifiedListsCount = voteCount;
        listResult.ConventionalSubTotal.UnmodifiedListBlankRowsCount = listResult.List.BlankRowCount * voteCount;
        listResult.ConventionalSubTotal.UnmodifiedListVotesCount = listResult.CandidateResults.Sum(c => c.ConventionalSubTotal.UnmodifiedListVotesCount);

        await _listResultRepo.Update(listResult);
    }

    internal async Task AddVotesFromBundle(ProportionalElectionResultBundle resultBundle)
    {
        await _candidateResultBuilder.AdjustConventionalCandidateResultForBundle(resultBundle, 1);
        await AdjustListResultForBundle(resultBundle, 1);
    }

    internal async Task RemoveVotesFromBundle(ProportionalElectionResultBundle resultBundle)
    {
        await _candidateResultBuilder.AdjustConventionalCandidateResultForBundle(resultBundle, -1);
        await AdjustListResultForBundle(resultBundle, -1);
    }

    private async Task ResetConventionalResult(ProportionalElectionResult electionResult, bool includeCountOfVoters)
    {
        electionResult.ResetAllSubTotals(VotingDataSource.Conventional, includeCountOfVoters);
        electionResult.CountOfBundlesNotReviewedOrDeleted = 0;
        electionResult.Bundles.Clear();
        electionResult.UpdateVoterParticipation();

        if (includeCountOfVoters)
        {
            await ResetSimpleResult(electionResult.Id);
        }
    }

    private async Task AdjustListResultForBundle(ProportionalElectionResultBundle resultBundle, int deltaFactor)
    {
        var blankRowCounts = await _resultBallotRepo.Query()
            .Where(r => r.BundleId == resultBundle.Id)
            .SumAsync(r => (int?)r.EmptyVoteCount) ?? 0;

        if (resultBundle.ListId == null)
        {
            var result = await _resultRepo.GetByKey(resultBundle.ElectionResultId)
                ?? throw new EntityNotFoundException(resultBundle.ElectionResultId);

            // TotalCountOfListsWithoutParty is calculated in UpdateTotalCountOfBallots
            result.ConventionalSubTotal.TotalCountOfBlankRowsOnListsWithoutParty += blankRowCounts * deltaFactor;

            await _resultRepo.Update(result);
            return;
        }

        var listResult = await _listResultRepo.Query()
            .FirstOrDefaultAsync(l => l.ListId == resultBundle.ListId && l.ResultId == resultBundle.ElectionResultId)
            ?? throw new EntityNotFoundException(resultBundle.Id);

        // ModifiedListVotesCount are calculated in AdjustCandidateResultForBundle
        listResult.ConventionalSubTotal.ModifiedListsCount += resultBundle.CountOfBallots * deltaFactor;
        listResult.ConventionalSubTotal.ModifiedListBlankRowsCount += blankRowCounts * deltaFactor;

        await _listResultRepo.Update(listResult);
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
}
