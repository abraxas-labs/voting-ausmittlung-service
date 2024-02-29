// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class MajorityElectionCandidateResultBuilder
{
    private readonly IDbRepository<DataContext, MajorityElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResultBallotCandidate> _ballotCandidateRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionResultBallotCandidate> _secondaryBallotCandidateRepo;
    private readonly IDbRepository<DataContext, MajorityElectionCandidate> _candidateRepo;
    private readonly IDbRepository<DataContext, MajorityElectionCandidateResult> _candidateResultRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionCandidateResult> _secondaryCandidateResultRepo;
    private readonly DataContext _dataContext;

    public MajorityElectionCandidateResultBuilder(
        IDbRepository<DataContext, MajorityElectionResult> resultRepo,
        IDbRepository<DataContext, MajorityElectionResultBallotCandidate> ballotCandidateRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionResultBallotCandidate> secondaryBallotCandidateRepo,
        IDbRepository<DataContext, MajorityElectionCandidate> candidateRepo,
        IDbRepository<DataContext, MajorityElectionCandidateResult> candidateResultRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionCandidateResult> secondaryCandidateResultRepo,
        DataContext dataContext)
    {
        _resultRepo = resultRepo;
        _ballotCandidateRepo = ballotCandidateRepo;
        _secondaryBallotCandidateRepo = secondaryBallotCandidateRepo;
        _candidateRepo = candidateRepo;
        _candidateResultRepo = candidateResultRepo;
        _secondaryCandidateResultRepo = secondaryCandidateResultRepo;
        _dataContext = dataContext;
    }

    internal async Task Initialize(Guid electionId, Guid candidateId)
    {
        var resultsToUpdate = await _resultRepo.Query()
            .AsTracking()
            .Where(x => x.MajorityElectionId == electionId &&
                        x.CandidateResults.All(y => y.CandidateId != candidateId))
            .Include(x => x.CandidateResults)
            .ToListAsync();

        foreach (var result in resultsToUpdate)
        {
            result.CandidateResults.Add(new MajorityElectionCandidateResult
            {
                CandidateId = candidateId,
            });
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task InitializeSecondaryMajorityElectionCandidate(Guid secondaryElectionId, Guid candidateId)
    {
        var resultsToUpdate = await _resultRepo.Query()
            .AsTracking()
            .SelectMany(x => x.SecondaryMajorityElectionResults)
            .Where(x => x.SecondaryMajorityElectionId == secondaryElectionId
                        && x.CandidateResults.All(c => c.CandidateId != candidateId))
            .Include(x => x.CandidateResults)
            .ToListAsync();

        foreach (var result in resultsToUpdate)
        {
            result.CandidateResults.Add(new SecondaryMajorityElectionCandidateResult
            {
                CandidateId = candidateId,
            });
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task AddMissing(
        Guid electionId,
        IEnumerable<MajorityElectionResult> results)
    {
        var candidateIds = await _candidateRepo.Query()
            .Where(c => c.MajorityElectionId == electionId)
            .Select(c => c.Id)
            .ToListAsync();

        if (candidateIds.Count == 0)
        {
            return;
        }

        foreach (var result in results)
        {
            AddMissing(result, candidateIds);
        }
    }

    internal void SetConventionalVoteCountValues(
        IEnumerable<MajorityElectionCandidateResultBase> candidateResults,
        Dictionary<Guid, int?> updatedResultsByCandidateId)
    {
        foreach (var candidateResult in candidateResults)
        {
            if (updatedResultsByCandidateId.Remove(candidateResult.CandidateId, out var count))
            {
                candidateResult.ConventionalVoteCount = count;
            }
        }

        if (updatedResultsByCandidateId.Count > 0)
        {
            throw new EntityNotFoundException(updatedResultsByCandidateId.First().Key);
        }
    }

    internal async Task AddConventionalVotesFromBundle(Guid electionResultId, Guid bundleId)
    {
        await AdjustConventionalCandidateVotesForBundle(electionResultId, bundleId, 1);
        await AdjustConventionalSecondaryCandidateVotesForBundle(electionResultId, bundleId, 1);
    }

    internal async Task RemoveConventionalVotesFromBundle(Guid electionResultId, Guid bundleId)
    {
        await AdjustConventionalCandidateVotesForBundle(electionResultId, bundleId, -1);
        await AdjustConventionalSecondaryCandidateVotesForBundle(electionResultId, bundleId, -1);
    }

    internal async Task AdjustConventionalVotes(
        Guid electionResultId,
        IReadOnlyDictionary<Guid, int> candidateVoteDeltas,
        int deltaFactor = 1)
    {
        await AdjustConventionalVotes(
            _candidateResultRepo,
            cr => cr.ElectionResultId == electionResultId,
            candidateVoteDeltas,
            deltaFactor);
    }

    internal async Task AdjustConventionalSecondaryMajorityVotes(
        Guid primaryElectionResultId,
        IReadOnlyDictionary<Guid, int> candidateVoteDeltas,
        int deltaFactor = 1)
    {
        await AdjustConventionalVotes(
            _secondaryCandidateResultRepo,
            cr => cr.ElectionResult.PrimaryResultId == primaryElectionResultId,
            candidateVoteDeltas,
            deltaFactor);
    }

    private void AddMissing(MajorityElectionResult result, IEnumerable<Guid> candidateIds)
    {
        var toAdd = candidateIds.Except(result.CandidateResults.Select(x => x.CandidateId))
            .Select(x => new MajorityElectionCandidateResult { CandidateId = x })
            .ToList();
        foreach (var element in toAdd)
        {
            result.CandidateResults.Add(element);
        }
    }

    private async Task AdjustConventionalCandidateVotesForBundle(Guid electionResultId, Guid bundleId, int deltaFactor)
    {
        var candidateVoteCountDelta = await GetBundleConventionalVoteCounts(_ballotCandidateRepo, c => c.Ballot.BundleId == bundleId);
        await AdjustConventionalVotes(electionResultId, candidateVoteCountDelta, deltaFactor);
    }

    private async Task AdjustConventionalSecondaryCandidateVotesForBundle(Guid primaryElectionResultId, Guid bundleId, int deltaFactor)
    {
        var candidateVoteCountDelta = await GetBundleConventionalVoteCounts(_secondaryBallotCandidateRepo, c => c.Ballot.PrimaryBallot.BundleId == bundleId);
        await AdjustConventionalSecondaryMajorityVotes(primaryElectionResultId, candidateVoteCountDelta, deltaFactor);
    }

    private async Task AdjustConventionalVotes<T>(
        IDbRepository<DataContext, T> candidateResultRepo,
        Expression<Func<T, bool>> predicate,
        IReadOnlyDictionary<Guid, int> candidateVoteDeltas,
        int deltaFactor)
        where T : MajorityElectionCandidateResultBase, new()
    {
        if (candidateVoteDeltas.Count == 0)
        {
            return;
        }

        var candidateResultsToUpdate = await candidateResultRepo.Query()
            .Where(predicate)
            .Where(c => candidateVoteDeltas.Keys.Contains(c.CandidateId))
            .ToListAsync();
        foreach (var candidateResult in candidateResultsToUpdate)
        {
            candidateResult.ConventionalVoteCount += candidateVoteDeltas[candidateResult.CandidateId] * deltaFactor;
        }

        await candidateResultRepo.UpdateRange(candidateResultsToUpdate);
    }

    private async Task<Dictionary<Guid, int>> GetBundleConventionalVoteCounts<T>(
        IDbRepository<DataContext, T> repo,
        Expression<Func<T, bool>> predicate)
    where T : MajorityElectionResultBallotCandidateBase, new()
    {
        return await repo.Query()
            .Where(predicate)
            .Where(c => c.Selected)
            .GroupBy(c => c.CandidateId, (candidateId, cc) => new
            {
                CandidateId = candidateId,
                Count = cc.Count(),
            })
            .ToDictionaryAsync(x => x.CandidateId, x => x.Count);
    }
}
