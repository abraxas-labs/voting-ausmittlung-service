// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class MajorityElectionResultBallotBuilder
{
    private readonly IDbRepository<DataContext, MajorityElectionResultBallot> _ballotRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResultBundle> _bundleRepo;
    private readonly DataContext _dbContext;

    public MajorityElectionResultBallotBuilder(
        IDbRepository<DataContext, MajorityElectionResultBallot> ballotRepo,
        IDbRepository<DataContext, MajorityElectionResultBundle> bundleRepo,
        DataContext dbContext)
    {
        _ballotRepo = ballotRepo;
        _bundleRepo = bundleRepo;
        _dbContext = dbContext;
    }

    internal async Task<bool> CreateBallot(
        Guid bundleId,
        MajorityElectionResultBallotCreated data)
    {
        var ids = await _bundleRepo.Query()
            .AsSingleQuery()
            .Where(b => b.Id == bundleId)
            .Select(x => new
            {
                PrimaryCandidateIds = x.ElectionResult.MajorityElection.MajorityElectionCandidates.Select(c => c.Id),
                SecondaryIds = x.ElectionResult.SecondaryMajorityElectionResults.Select(e => new
                {
                    ElectionId = e.SecondaryMajorityElectionId,
                    ResultId = e.Id,
                    CandidateIds = e.CandidateResults.Select(c => c.CandidateId),
                }),
            })
            .FirstOrDefaultAsync();

        // The bundle may not exist
        if (ids == null)
        {
            return false;
        }

        var selectedCandidates = data.SelectedCandidateIds.Select(Guid.Parse).ToHashSet();
        var ballot = new MajorityElectionResultBallot
        {
            Number = data.BallotNumber,
            BundleId = bundleId,
            EmptyVoteCount = data.EmptyVoteCount,
            IndividualVoteCount = data.IndividualVoteCount,
            InvalidVoteCount = data.InvalidVoteCount,
            CandidateVoteCountExclIndividual = selectedCandidates.Count,
            BallotCandidates = ids.PrimaryCandidateIds
                .Select(x => new MajorityElectionResultBallotCandidate
                {
                    CandidateId = x,
                    Selected = selectedCandidates.Contains(x),
                })
                .ToList(),
        };

        var secondaryResultsById = data.SecondaryMajorityElectionResults.ToDictionary(x => Guid.Parse(x.SecondaryMajorityElectionId));
        foreach (var resultAndCandidate in ids.SecondaryIds)
        {
            if (!secondaryResultsById.TryGetValue(resultAndCandidate.ElectionId, out var result))
            {
                result = new SecondaryMajorityElectionResultBallotEventData();
            }

            var selectedCandidateIds = result.SelectedCandidateIds.Select(Guid.Parse).ToHashSet();
            ballot.SecondaryMajorityElectionBallots.Add(new SecondaryMajorityElectionResultBallot
            {
                SecondaryMajorityElectionResultId = resultAndCandidate.ResultId,
                EmptyVoteCount = result.EmptyVoteCount,
                IndividualVoteCount = result.IndividualVoteCount,
                InvalidVoteCount = result.InvalidVoteCount,
                CandidateVoteCountExclIndividual = selectedCandidateIds.Count,
                BallotCandidates = resultAndCandidate.CandidateIds
                    .Select(cId => new SecondaryMajorityElectionResultBallotCandidate
                    {
                        CandidateId = cId,
                        Selected = selectedCandidateIds.Contains(cId),
                    })
                    .ToList(),
            });
        }

        await _ballotRepo.Create(ballot);
        return true;
    }

    internal async Task UpdateBallot(
        Guid bundleId,
        MajorityElectionResultBallotUpdated data)
    {
        var ballot = await _ballotRepo
            .Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.BallotCandidates)
            .Include(x => x.SecondaryMajorityElectionBallots).ThenInclude(x => x.BallotCandidates)
            .Include(x => x.SecondaryMajorityElectionBallots).ThenInclude(x => x.SecondaryMajorityElectionResult)
            .FirstOrDefaultAsync(x => x.Number == data.BallotNumber && x.BundleId == bundleId)
            ?? throw new EntityNotFoundException(new { bundleId, data.BallotNumber });

        ballot.EmptyVoteCount = data.EmptyVoteCount;
        ballot.IndividualVoteCount = data.IndividualVoteCount;
        ballot.InvalidVoteCount = data.InvalidVoteCount;
        ballot.CandidateVoteCountExclIndividual = ReplaceSelectedCandidates(ballot.BallotCandidates, data.SelectedCandidateIds);
        UpdateSecondaryMajorityElectionResults(ballot, data.SecondaryMajorityElectionResults);
        await _dbContext.SaveChangesAsync();
    }

    private void UpdateSecondaryMajorityElectionResults(
        MajorityElectionResultBallot ballot,
        IEnumerable<SecondaryMajorityElectionResultBallotEventData>? results)
    {
        if (results == null)
        {
            return;
        }

        var secondaryElectionBallotsByElectionId =
            ballot.SecondaryMajorityElectionBallots.ToDictionary(b => b.SecondaryMajorityElectionResult.SecondaryMajorityElectionId);
        foreach (var result in results)
        {
            if (!secondaryElectionBallotsByElectionId.TryGetValue(Guid.Parse(result.SecondaryMajorityElectionId), out var secondaryBallot))
            {
                throw new EntityNotFoundException(result.SecondaryMajorityElectionId);
            }

            secondaryBallot.EmptyVoteCount = result.EmptyVoteCount;
            secondaryBallot.IndividualVoteCount = result.IndividualVoteCount;
            secondaryBallot.InvalidVoteCount = result.InvalidVoteCount;
            secondaryBallot.CandidateVoteCountExclIndividual = ReplaceSelectedCandidates(secondaryBallot.BallotCandidates, result.SelectedCandidateIds);
        }
    }

    private int ReplaceSelectedCandidates(
        IEnumerable<MajorityElectionResultBallotCandidateBase> ballotCandidates,
        IEnumerable<string>? selectedCandidates)
    {
        var selectedCandidateIds = selectedCandidates?.Select(GuidParser.Parse).ToHashSet()
            ?? new HashSet<Guid>();
        foreach (var candidate in ballotCandidates)
        {
            candidate.Selected = selectedCandidateIds.Contains(candidate.CandidateId);
        }

        return selectedCandidateIds.Count;
    }
}
