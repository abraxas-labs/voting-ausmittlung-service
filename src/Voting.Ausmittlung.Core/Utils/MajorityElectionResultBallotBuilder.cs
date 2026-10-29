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
using Voting.Ausmittlung.Core.Extensions;
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

    internal async Task CreateBallot(
        Guid bundleId,
        MajorityElectionResultBallotCreated data)
    {
        var bundle = await _bundleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ElectionResult.MajorityElection.MajorityElectionCandidates)
            .Include(x => x.BallotGroup!.Entries)
            .ThenInclude(x => x.Candidates)
            .ThenInclude(x => x.PrimaryElectionCandidate)
            .Include(x => x.BallotGroup!.Entries)
            .ThenInclude(x => x.Candidates)
            .ThenInclude(x => x.SecondaryElectionCandidate)
            .Include(x => x.ElectionResult.SecondaryMajorityElectionResults)
            .ThenInclude(x => x.CandidateResults)
            .ThenInclude(x => x.Candidate)
            .FirstAsync(x => x.Id == bundleId);

        var selectedCandidatesById = data.Candidates.ToDictionary(x => Guid.Parse(x.CandidateId));
        var ballotCandidates = BuildPrimaryElectionBallotCandidates(bundle, selectedCandidatesById);
        var ballot = new MajorityElectionResultBallot
        {
            Number = data.BallotNumber,
            Index = data.Index ?? data.BallotNumber,
            BundleId = bundleId,
            EmptyVoteCount = data.EmptyVoteCount,
            IndividualVoteCount = data.IndividualVoteCount,
            InvalidVoteCount = data.InvalidVoteCount,
            CandidateVoteCountExclIndividual = data.Candidates.Count,
            BallotCandidates = ballotCandidates,
        };

        var secondaryResultsById = data.SecondaryMajorityElectionResults.ToDictionary(x => Guid.Parse(x.SecondaryMajorityElectionId));
        foreach (var secondaryMajorityElectionResult in bundle.ElectionResult.SecondaryMajorityElectionResults)
        {
            if (!secondaryResultsById.TryGetValue(secondaryMajorityElectionResult.SecondaryMajorityElectionId, out var result))
            {
                result = new SecondaryMajorityElectionResultBallotEventData();
            }

            var secondaryElectionSelectedCandidatesById = result.Candidates.ToDictionary(x => Guid.Parse(x.CandidateId));
            var secondaryElectionBallotCandidates = BuildSecondaryElectionBallotCandidates(bundle, secondaryElectionSelectedCandidatesById, secondaryMajorityElectionResult);
            ballot.SecondaryMajorityElectionBallots.Add(new SecondaryMajorityElectionResultBallot
            {
                SecondaryMajorityElectionResultId = secondaryMajorityElectionResult.Id,
                EmptyVoteCount = result.EmptyVoteCount,
                IndividualVoteCount = result.IndividualVoteCount,
                InvalidVoteCount = result.InvalidVoteCount,
                CandidateVoteCountExclIndividual = result.Candidates.Count,
                BallotCandidates = secondaryElectionBallotCandidates,
            });
        }

        await _ballotRepo.Create(ballot);
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
            .Include(x => x.Bundle.ElectionResult)
            .FirstOrDefaultAsync(x => x.Number == data.BallotNumber && x.BundleId == bundleId)
            ?? throw new EntityNotFoundException(new { bundleId, data.BallotNumber });

        ballot.EmptyVoteCount = data.EmptyVoteCount;
        ballot.IndividualVoteCount = data.IndividualVoteCount;
        ballot.InvalidVoteCount = data.InvalidVoteCount;
        ballot.CandidateVoteCountExclIndividual = data.Candidates.Count;
        var bundlesWithBallotGroupEnabled = ballot.Bundle.ElectionResult.EntryParams?.BundlesWithBallotGroupEnabled == true;
        ballot.BallotCandidates = ReplacePrimaryElectionCandidates(ballot.BallotCandidates, data.Candidates, bundlesWithBallotGroupEnabled);
        UpdateSecondaryMajorityElectionResults(ballot, data.SecondaryMajorityElectionResults, bundlesWithBallotGroupEnabled);

        if (ballot.Bundle.State > BallotBundleState.InProcess)
        {
            ballot.Logs.Add(new MajorityElectionResultBallotLog
            {
                User = data.EventInfo.User.ToDataUser(),
                Timestamp = data.EventInfo.Timestamp.ToDateTime(),
            });
        }

        if (ballot.Bundle.State == BallotBundleState.ReadyForReview)
        {
            ballot.ModifiedDuringReview = true;
        }

        await _dbContext.SaveChangesAsync();
    }

    private static List<MajorityElectionResultBallotCandidate> BuildPrimaryElectionBallotCandidates(MajorityElectionResultBundle bundle, Dictionary<Guid, MajorityElectionResultBallotUpdatedCandidateEventData> selectedCandidatesById)
    {
        if (bundle.ElectionResult.EntryParams?.BundlesWithBallotGroupEnabled != true)
        {
            return
            [
                .. bundle.ElectionResult.MajorityElection.MajorityElectionCandidates
                    .Select(c => new MajorityElectionResultBallotCandidate
                    {
                        CandidateId = c.Id,
                        Selected = selectedCandidatesById.ContainsKey(c.Id),
                        Position = c.Position,
                        OnBallotGroup = false,
                    })
            ];
        }

        var ballotCandidates = new List<MajorityElectionResultBallotCandidate>();
        if (bundle.BallotGroupId.HasValue)
        {
            var ballotGroupCandidateIds = bundle.BallotGroup!.Entries
                .Where(x => x.BallotGroupId == bundle.BallotGroupId.Value
                            && x.PrimaryMajorityElectionId == bundle.ElectionResult.MajorityElectionId)
                .SelectMany(x => x.Candidates)
                .Where(x => x.PrimaryElectionCandidateId.HasValue)
                .OrderBy(x => x.PrimaryElectionCandidate!.Position)
                .Select(x => x.PrimaryElectionCandidateId!.Value)
                .ToList();

            ballotCandidates.AddRange(ballotGroupCandidateIds.Select((id, index) => new MajorityElectionResultBallotCandidate
            {
                CandidateId = id,
                Selected = selectedCandidatesById.TryGetValue(id, out var candidate) && candidate.Position == index + 1 && candidate.OnBallotGroup,
                OnBallotGroup = true,
                Position = index + 1,
            }));
        }

        ballotCandidates.AddRange(selectedCandidatesById.Values.Where(x => !x.OnBallotGroup).Select(candidate => new MajorityElectionResultBallotCandidate
        {
            CandidateId = Guid.Parse(candidate.CandidateId),
            Selected = true,
            OnBallotGroup = false,
            Position = candidate.Position,
        }));

        return ballotCandidates;
    }

    private static List<SecondaryMajorityElectionResultBallotCandidate> BuildSecondaryElectionBallotCandidates(MajorityElectionResultBundle bundle, Dictionary<Guid, MajorityElectionResultBallotUpdatedCandidateEventData> selectedCandidatesById, SecondaryMajorityElectionResult secondaryMajorityElectionResult)
    {
        if (bundle.ElectionResult.EntryParams?.BundlesWithBallotGroupEnabled != true)
        {
            return secondaryMajorityElectionResult.CandidateResults
                .Select(x => new SecondaryMajorityElectionResultBallotCandidate
                {
                    CandidateId = x.CandidateId,
                    Selected = selectedCandidatesById.ContainsKey(x.CandidateId),
                    Position = x.CandidatePosition,
                    OnBallotGroup = false,
                })
                .ToList();
        }

        var ballotCandidates = new List<SecondaryMajorityElectionResultBallotCandidate>();
        if (bundle.BallotGroupId.HasValue)
        {
            var ballotGroupCandidateIds = bundle.BallotGroup!.Entries
                .Where(x => x.BallotGroupId == bundle.BallotGroupId.Value
                            && x.SecondaryMajorityElectionId == secondaryMajorityElectionResult.SecondaryMajorityElectionId)
                .SelectMany(x => x.Candidates)
                .Where(x => x.SecondaryElectionCandidateId.HasValue)
                .OrderBy(x => x.SecondaryElectionCandidate!.Position)
                .Select(x => x.SecondaryElectionCandidateId!.Value)
                .ToList();

            ballotCandidates.AddRange(ballotGroupCandidateIds.Select((id, index) => new SecondaryMajorityElectionResultBallotCandidate
            {
                CandidateId = id,
                Selected = selectedCandidatesById.TryGetValue(id, out var candidate) && candidate.Position == index + 1 && candidate.OnBallotGroup,
                OnBallotGroup = true,
                Position = index + 1,
            }));
        }

        ballotCandidates.AddRange(selectedCandidatesById.Values.Where(x => !x.OnBallotGroup).Select(candidate => new SecondaryMajorityElectionResultBallotCandidate
        {
            CandidateId = Guid.Parse(candidate.CandidateId),
            Selected = true,
            OnBallotGroup = false,
            Position = candidate.Position,
        }));

        return ballotCandidates;
    }

    private static void UpdateSecondaryMajorityElectionResults(
        MajorityElectionResultBallot ballot,
        IEnumerable<SecondaryMajorityElectionResultBallotEventData>? results,
        bool bundlesWithBallotGroupEnabled)
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
            secondaryBallot.CandidateVoteCountExclIndividual = result.Candidates.Count;
            secondaryBallot.BallotCandidates = ReplaceSecondaryElectionCandidates(secondaryBallot.BallotCandidates, result.Candidates, bundlesWithBallotGroupEnabled);
        }
    }

    private static List<MajorityElectionResultBallotCandidate> ReplacePrimaryElectionCandidates(
        ICollection<MajorityElectionResultBallotCandidate> ballotCandidates,
        ICollection<MajorityElectionResultBallotUpdatedCandidateEventData>? selectedCandidates,
        bool bundlesWithBallotGroupEnabled)
    {
        var selectedCandidateIds = selectedCandidates?.Select(x => GuidParser.Parse(x.CandidateId)).ToHashSet() ?? [];
        if (!bundlesWithBallotGroupEnabled)
        {
            foreach (var candidate in ballotCandidates)
            {
                candidate.Selected = selectedCandidateIds.Contains(candidate.CandidateId);
            }

            return ballotCandidates.ToList();
        }

        // remove candidates not on ballot group (which were added by the user)
        ballotCandidates = ballotCandidates.Where(x => x.OnBallotGroup).ToList();

        foreach (var candidate in ballotCandidates)
        {
            candidate.Selected = selectedCandidates?.Any(x => Guid.Parse(x.CandidateId) == candidate.CandidateId && x.Position == candidate.Position && x.OnBallotGroup) == true;
        }

        var addedCandidates = selectedCandidates?.Where(x => !x.OnBallotGroup).ToList() ?? [];
        foreach (var addedCandidate in addedCandidates)
        {
            ballotCandidates.Add(new MajorityElectionResultBallotCandidate
            {
                CandidateId = Guid.Parse(addedCandidate.CandidateId),
                Selected = true,
                Position = addedCandidate.Position,
                OnBallotGroup = false,
            });
        }

        return ballotCandidates.ToList();
    }

    private static List<SecondaryMajorityElectionResultBallotCandidate> ReplaceSecondaryElectionCandidates(
        ICollection<SecondaryMajorityElectionResultBallotCandidate> ballotCandidates,
        ICollection<MajorityElectionResultBallotUpdatedCandidateEventData>? selectedCandidates,
        bool bundlesWithBallotGroupEnabled)
    {
        var selectedCandidateIds = selectedCandidates?.Select(x => GuidParser.Parse(x.CandidateId)).ToHashSet() ?? [];
        if (!bundlesWithBallotGroupEnabled)
        {
            foreach (var candidate in ballotCandidates)
            {
                candidate.Selected = selectedCandidateIds.Contains(candidate.CandidateId);
            }

            return ballotCandidates.ToList();
        }

        // remove candidates not on ballot group (which were added by the user)
        ballotCandidates = ballotCandidates.Where(x => x.OnBallotGroup).ToList();

        foreach (var candidate in ballotCandidates)
        {
            candidate.Selected = selectedCandidates?.Any(x => Guid.Parse(x.CandidateId) == candidate.CandidateId && x.Position == candidate.Position && x.OnBallotGroup) == true;
        }

        var addedCandidates = selectedCandidates?.Where(x => !x.OnBallotGroup).ToList() ?? [];
        foreach (var addedCandidate in addedCandidates)
        {
            ballotCandidates.Add(new SecondaryMajorityElectionResultBallotCandidate
            {
                CandidateId = Guid.Parse(addedCandidate.CandidateId),
                Selected = true,
                Position = addedCandidate.Position,
                OnBallotGroup = false,
            });
        }

        return ballotCandidates.ToList();
    }
}
