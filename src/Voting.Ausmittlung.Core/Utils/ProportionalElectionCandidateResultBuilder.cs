// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionCandidateResultBuilder
{
    private readonly IDbRepository<DataContext, ProportionalElectionListResult> _listResultRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResultBallotCandidate> _resultBallotCandidatesRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionCandidateResult> _candidateResultRepo;
    private readonly DataContext _dataContext;

    public ProportionalElectionCandidateResultBuilder(
        IDbRepository<DataContext, ProportionalElectionListResult> listResultRepo,
        IDbRepository<DataContext, ProportionalElectionResultBallotCandidate> resultBallotCandidatesRepo,
        IDbRepository<DataContext, ProportionalElectionCandidateResult> candidateResultRepo,
        DataContext dataContext)
    {
        _listResultRepo = listResultRepo;
        _resultBallotCandidatesRepo = resultBallotCandidatesRepo;
        _candidateResultRepo = candidateResultRepo;
        _dataContext = dataContext;
    }

    internal async Task Initialize(Guid listId, Guid candidateId)
    {
        var listResultsToUpdate = await _listResultRepo.Query()
            .AsTracking()
            .Where(x => x.ListId == listId)
            .Include(x => x.CandidateResults)
            .ToListAsync();

        foreach (var listResult in listResultsToUpdate)
        {
            listResult.CandidateResults.Add(new ProportionalElectionCandidateResult
            {
                CandidateId = candidateId,
            });
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task AdjustConventionalCandidateResultForBundle(ProportionalElectionResultBundle resultBundle, int deltaFactor)
    {
        var ballotCandidates = await _resultBallotCandidatesRepo.Query()
            .Where(x => x.Ballot.BundleId == resultBundle.Id && !x.RemovedFromList)
            .ToListAsync();

        var accumulatedCandidateIdsByBallotId = ballotCandidates
            .GroupBy(bc => new { bc.BallotId, bc.CandidateId })
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .GroupBy(x => x.BallotId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.CandidateId).ToHashSet());

        var candidateIds = ballotCandidates.Select(c => c.CandidateId).ToHashSet();
        var candidateResults = await _candidateResultRepo.Query()
            .AsTracking()
            .Include(c => c.ListResult)
            .Include(c => c.VoteSources)
            .Where(r => r.ListResult.ResultId == resultBundle.ElectionResultId && candidateIds.Contains(r.CandidateId))
            .ToListAsync();

        var candidateResultsByCandidateId = candidateResults.ToDictionary(x => x.CandidateId);
        foreach (var ballotCandidate in ballotCandidates)
        {
            var candidateResult = candidateResultsByCandidateId[ballotCandidate.CandidateId];
            candidateResult.ConventionalSubTotal.ModifiedListVotesCount += deltaFactor;

            // Each candidate vote results in a vote for the corresponding list
            candidateResult.ListResult.ConventionalSubTotal.ModifiedListVotesCount += deltaFactor;
            AdjustConventionalVoteSource(candidateResult, resultBundle, deltaFactor);

            // votes on bundles without a list don't count
            if (candidateResult.ListResult.ListId != resultBundle.ListId && resultBundle.ListId.HasValue)
            {
                candidateResult.ConventionalSubTotal.CountOfVotesOnOtherLists += deltaFactor;
                candidateResult.ListResult.ConventionalSubTotal.ListVotesCountOnOtherLists += deltaFactor;
            }

            if (accumulatedCandidateIdsByBallotId.TryGetValue(ballotCandidate.BallotId, out var accumulatedCandidateIds)
                && accumulatedCandidateIds.Remove(ballotCandidate.CandidateId))
            {
                candidateResult.ConventionalSubTotal.CountOfVotesFromAccumulations += deltaFactor;
            }
        }

        await _dataContext.SaveChangesAsync();
    }

    private void AdjustConventionalVoteSource(
        ProportionalElectionCandidateResult candidateResult,
        ProportionalElectionResultBundle bundle,
        int deltaFactor)
    {
        var voteSource = candidateResult.VoteSources.FirstOrDefault(x => x.ListId == bundle.ListId);
        if (voteSource == null)
        {
            voteSource = new ProportionalElectionCandidateVoteSourceResult
            {
                ListId = bundle.ListId,
                CandidateResult = candidateResult,
                CandidateResultId = candidateResult.Id,
            };
            candidateResult.VoteSources.Add(voteSource);
        }

        voteSource.ConventionalVoteCount += deltaFactor;
    }
}
