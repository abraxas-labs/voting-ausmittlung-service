// (c) Copyright 2022 by Abraxas Informatik AG
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

    internal async Task CreateBallot(
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
            .SelectMany(x => x.List!.ProportionalElectionCandidates)
            .ToListAsync();

        ReplaceCandidates(ballot, listCandidates, candidates);
        await _ballotRepo.Create(ballot);
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
        var listCandidatesById = listCandidates.ToDictionary(x => x.Id);
        var listAccumulatedCandidatesById = listCandidates.Where(x => x.Accumulated).ToDictionary(x => x.Id);

        ballot.BallotCandidates.Clear();
        foreach (var candidate in candidates)
        {
            var cId = GuidParser.Parse(candidate.CandidateId);
            if (candidate.OnList && !listAccumulatedCandidatesById.Remove(cId))
            {
                listCandidatesById.Remove(cId);
            }

            ballot.BallotCandidates.Add(new ProportionalElectionResultBallotCandidate
            {
                CandidateId = cId,
                Position = candidate.Position,
                OnList = candidate.OnList,
            });
        }

        foreach (var removedCandidate in listCandidatesById.Values)
        {
            ballot.BallotCandidates.Add(new ProportionalElectionResultBallotCandidate
            {
                CandidateId = removedCandidate.Id,
                Position = removedCandidate.Position,
                OnList = true,
                RemovedFromList = true,
            });
        }

        foreach (var removedCandidate in listAccumulatedCandidatesById.Values)
        {
            ballot.BallotCandidates.Add(new ProportionalElectionResultBallotCandidate
            {
                CandidateId = removedCandidate.Id,
                Position = removedCandidate.AccumulatedPosition,
                OnList = true,
                RemovedFromList = true,
            });
        }
    }
}
