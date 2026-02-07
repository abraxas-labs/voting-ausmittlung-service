// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionResultBallotBuilder
{
    private readonly ProportionalElectionResultBallotRepo _ballotRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionResultBundle> _bundleRepo;
    private readonly ProportionalElectionResultBallotCandidateRepo _ballotCandidateRepo;
    private readonly DataContext _dbContext;

    public ProportionalElectionResultBallotBuilder(
        ProportionalElectionResultBallotRepo ballotRepo,
        IDbRepository<DataContext, ProportionalElectionResultBundle> bundleRepo,
        ProportionalElectionResultBallotCandidateRepo ballotCandidateRepo,
        DataContext dbContext)
    {
        _ballotRepo = ballotRepo;
        _bundleRepo = bundleRepo;
        _ballotCandidateRepo = ballotCandidateRepo;
        _dbContext = dbContext;
    }

    internal async Task CreateBallot(
        Guid bundleId,
        ProportionalElectionResultBallotCreated eventData)
    {
        var ballotId = Guid.NewGuid();
        var ballotIndex = eventData.Index ?? eventData.BallotNumber;

        // Since this is a hot path in the event processing, we resort to creating SQL manually.
        // In our benchmarks, this improved performance quite a bit (up to 5-10 times faster than using EF).
        // Note that we are using raw SQL here without parametrized queries.
        // This is fine as long as we do not have any untrusted string data inputs.
        // Since we only insert Guids, numbers and booleans, we are safe.
        var sqlStringBuilder = new StringBuilder();
        sqlStringBuilder.Append(
            $"""
             INSERT INTO {_ballotRepo.DelimitedTableName}
                 ({_ballotRepo.IdColumnName}, {_ballotRepo.BundleIdColumnName}, {_ballotRepo.NumberColumnName},
                 {_ballotRepo.EmptyVoteCountColumnName}, {_ballotRepo.MarkedForReviewColumnName}, {_ballotRepo.IndexColumnName})
             VALUES ('{ballotId}', '{bundleId}', {eventData.BallotNumber}, {eventData.EmptyVoteCount}, False, {ballotIndex});
             """);

        await AppendBallotCandidatesSql(sqlStringBuilder, bundleId, eventData.Candidates, ballotId);
        await _dbContext.Database.ExecuteSqlRawAsync(sqlStringBuilder.ToString());
    }

    internal async Task UpdateBallot(Guid bundleId, ProportionalElectionResultBallotUpdated eventData)
    {
        var ballot = await _ballotRepo
            .Query()
            .AsTracking()
            .Include(x => x.Bundle)
            .FirstOrDefaultAsync(x => x.Number == eventData.BallotNumber && x.BundleId == bundleId)
            ?? throw new EntityNotFoundException(new { bundleId, eventData.BallotNumber });
        ballot.EmptyVoteCount = eventData.EmptyVoteCount;

        if (ballot.Bundle.State > BallotBundleState.InProcess)
        {
            ballot.Logs.Add(new ProportionalElectionResultBallotLog
            {
                User = eventData.EventInfo.User.ToDataUser(),
                Timestamp = eventData.EventInfo.Timestamp.ToDateTime(),
            });
        }

        await _dbContext.SaveChangesAsync();

        var sqlStringBuilder = new StringBuilder();
        sqlStringBuilder.Append(
            $"""
             DELETE FROM {_ballotCandidateRepo.DelimitedTableName}
             WHERE {_ballotCandidateRepo.BallotIdColumnName} = '{ballot.Id}';
             """);
        await AppendBallotCandidatesSql(sqlStringBuilder, bundleId, eventData.Candidates, ballot.Id);
        await _dbContext.Database.ExecuteSqlRawAsync(sqlStringBuilder.ToString());
    }

    private async Task AppendBallotCandidatesSql(
        StringBuilder sqlStringBuilder,
        Guid bundleId,
        ICollection<ProportionalElectionResultBallotUpdatedCandidateEventData> candidates,
        Guid ballotId)
    {
        var ballotCandidates = await BuildBallotCandidates(bundleId, candidates);
        if (ballotCandidates.Count == 0)
        {
            return;
        }

        // Note that we are using raw SQL here without parametrized queries.
        // This is fine as long as we do not have any untrusted string data inputs.
        // Since we only insert Guids, numbers and booleans, we are safe.
        // Using parametrized queries is not easy here, as we are dynamically creating a list of values.
        sqlStringBuilder.Append(
            $"""
             INSERT INTO {_ballotCandidateRepo.DelimitedTableName}
             ({_ballotCandidateRepo.IdColumnName}, {_ballotCandidateRepo.BallotIdColumnName}, {_ballotCandidateRepo.PositionColumnName},
             {_ballotCandidateRepo.OnListVoteCountColumnName}, {_ballotCandidateRepo.RemovedFromListColumnName},
             {_ballotCandidateRepo.CandidateIdColumnName})
             VALUES
            """);

        for (var i = 0; i < ballotCandidates.Count; i++)
        {
            sqlStringBuilder.Append(i == 0
                ? ' '
                : ',');

            var b = ballotCandidates[i];
            sqlStringBuilder.Append($"('{Guid.NewGuid()}', '{ballotId}', {b.Position}, {b.OnList}, {b.RemovedFromList}, '{b.CandidateId}')");
        }
    }

    private async Task<List<ProportionalElectionResultBallotCandidate>> BuildBallotCandidates(
        Guid bundleId,
        ICollection<ProportionalElectionResultBallotUpdatedCandidateEventData> candidates)
    {
        var listCandidates = await _bundleRepo.Query()
            .Where(x => x.Id == bundleId)
            .SelectMany(x => x.List!.ProportionalElectionCandidates.Select(c => new
            {
                c.Id,
                c.Accumulated,
                c.Position,
                c.AccumulatedPosition,
            }))
            .ToListAsync();

        var originalListCandidatesByPositions = listCandidates.ToDictionary(x => x.Position);
        foreach (var accumulatedListCandidate in listCandidates.Where(x => x.Accumulated))
        {
            originalListCandidatesByPositions[accumulatedListCandidate.AccumulatedPosition] = accumulatedListCandidate;
        }

        var ballotCandidates = new List<ProportionalElectionResultBallotCandidate>();
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

            ballotCandidates.Add(new ProportionalElectionResultBallotCandidate
            {
                CandidateId = cId,
                Position = candidate.Position,
                OnList = onList,
            });
        }

        // All positions that remain were removed/replaced by the user
        foreach (var (position, candidate) in originalListCandidatesByPositions)
        {
            ballotCandidates.Add(new ProportionalElectionResultBallotCandidate
            {
                CandidateId = candidate.Id,
                Position = position,
                OnList = true,
                RemovedFromList = true,
            });
        }

        return ballotCandidates;
    }
}
