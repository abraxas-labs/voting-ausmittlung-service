// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionResultBallotBuilder
{
    private readonly IDbRepository<DataContext, ProportionalElectionResultBallot> _ballotRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResultBundle> _bundleRepo;
    private readonly DataContext _dbContext;

    public ProportionalElectionResultBallotBuilder(
        IDbRepository<DataContext, ProportionalElectionResultBallot> ballotRepo,
        IDbRepository<DataContext, ProportionalElectionResultBundle> bundleRepo,
        DataContext dbContext)
    {
        _ballotRepo = ballotRepo;
        _bundleRepo = bundleRepo;
        _dbContext = dbContext;
    }

    internal async Task<bool> CreateBallot(
        Guid bundleId,
        int ballotNumber,
        int emptyVoteCount,
        ICollection<ProportionalElectionResultBallotUpdatedCandidateEventData> candidates)
    {
        var ballot = new ProportionalElectionResultBallot
        {
            Number = ballotNumber,
            BundleId = bundleId,
            EmptyVoteCount = emptyVoteCount,
        };

        var listCandidates = await _bundleRepo.Query()
            .Where(x => x.Id == bundleId)
            .Select(x => new
            {
                Candidates = x.List!.ProportionalElectionCandidates,
            })
            .FirstOrDefaultAsync();

        if (listCandidates == null)
        {
            return false;
        }

        ReplaceCandidates(ballot, listCandidates.Candidates, candidates);
        await _ballotRepo.Create(ballot);
        return true;
    }

    internal async Task UpdateBallot(
        Guid bundleId,
        int ballotNumber,
        int emptyVoteCount,
        ICollection<ProportionalElectionResultBallotUpdatedCandidateEventData> candidates)
    {
        var ballot = await _ballotRepo
                         .Query()
                         .AsTracking()
                         .AsSplitQuery()
                         .Include(x => x.BallotCandidates)
                         .Include(x => x.Bundle.List!.ProportionalElectionCandidates)
                         .FirstOrDefaultAsync(x => x.Number == ballotNumber && x.BundleId == bundleId)
                     ?? throw new EntityNotFoundException(new { bundleId, ballotNumber });
        ballot.EmptyVoteCount = emptyVoteCount;
        ReplaceCandidates(
            ballot,
            ballot.Bundle.List?.ProportionalElectionCandidates ?? new List<ProportionalElectionCandidate>(),
            candidates);
        await _dbContext.SaveChangesAsync();
    }

    private void ReplaceCandidates(
        ProportionalElectionResultBallot ballot,
        ICollection<ProportionalElectionCandidate> listCandidates,
        ICollection<ProportionalElectionResultBallotUpdatedCandidateEventData> candidates)
    {
        var originalListCandidatesByPositions = listCandidates.ToDictionary(x => x.Position);
        foreach (var accumulatedListCandidate in listCandidates.Where(x => x.Accumulated))
        {
            originalListCandidatesByPositions[accumulatedListCandidate.AccumulatedPosition] = accumulatedListCandidate;
        }

        ballot.BallotCandidates.Clear();
        foreach (var candidate in candidates)
        {
            var cId = GuidParser.Parse(candidate.CandidateId);
            var onList = false;

            // Check whether this position matches the "original list position", meaning that this position was not modified.
            // Note: if candidate.OnList is false, but the candidate would still match, then the candidate was removed and re-added
            if (candidate.OnList && originalListCandidatesByPositions.TryGetValue(candidate.Position, out var originalCandidate)
                                 && cId == originalCandidate.Id)
            {
                onList = true;

                // Remove it from this dictionary, so that only positions remain that were removed.
                originalListCandidatesByPositions.Remove(candidate.Position);
            }

            ballot.BallotCandidates.Add(new ProportionalElectionResultBallotCandidate
            {
                CandidateId = cId,
                Position = candidate.Position,
                OnList = onList,
            });
        }

        // All positions that remain were removed/replaced by the user
        foreach (var (position, candidate) in originalListCandidatesByPositions)
        {
            ballot.BallotCandidates.Add(new ProportionalElectionResultBallotCandidate
            {
                CandidateId = candidate.Id,
                Position = position,
                OnList = true,
                RemovedFromList = true,
            });
        }
    }
}
