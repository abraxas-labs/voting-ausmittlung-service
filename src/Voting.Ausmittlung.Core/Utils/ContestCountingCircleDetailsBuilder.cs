// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ContestCountingCircleDetailsBuilder
{
    private readonly DomainOfInfluenceCountingCircleRepo _doiCountingCirclesRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;
    private readonly AggregatedContestCountingCircleDetailsBuilder _aggregatedContestCountingCircleDetailsBuilder;

    public ContestCountingCircleDetailsBuilder(
        DomainOfInfluenceCountingCircleRepo doiCountingCirclesRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        AggregatedContestCountingCircleDetailsBuilder aggregatedContestCountingCircleDetailsBuilder)
    {
        _doiCountingCirclesRepo = doiCountingCirclesRepo;
        _contestRepo = contestRepo;
        _ccDetailsRepo = ccDetailsRepo;
        _aggregatedContestCountingCircleDetailsBuilder = aggregatedContestCountingCircleDetailsBuilder;
    }

    internal async Task SyncForDomainOfInfluences(IEnumerable<Guid> doiIds)
    {
        var contests = await _contestRepo.Query()
            .AsSplitQuery()
            .WhereInTestingPhase()
            .Where(x => doiIds.Contains(x.DomainOfInfluenceId))
            .Include(x => x.CountingCircleDetails)
            .Include(x => x.DomainOfInfluence.CountingCircles)
            .ThenInclude(x => x.CountingCircle)
            .ToListAsync();

        foreach (var contest in contests)
        {
            await Sync(contest, contest.DomainOfInfluence.CountingCircles);
        }
    }

    internal async Task<IReadOnlyCollection<ContestCountingCircleDetails>> SyncAndResetEVoting(Contest contest)
    {
        var countingCircles = await _doiCountingCirclesRepo.Query()
            .Where(c => c.DomainOfInfluenceId == contest.DomainOfInfluenceId)
            .Include(x => x.CountingCircle)
            .ToListAsync();

        return await Sync(contest, countingCircles);
    }

    internal async Task ResetEVotingVotingCards(Guid contestId)
    {
        var ccDetails = await _ccDetailsRepo
            .Query()
            .AsSplitQuery()
            .Include(x => x.VotingCards.Where(vc => vc.Channel == VotingChannel.EVoting))
            .Where(x => x.ContestId == contestId && x.VotingCards.Any(vc => vc.Channel == VotingChannel.EVoting))
            .ToListAsync();

        await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedVotingCards(contestId, ccDetails, true);

        foreach (var votingCard in ccDetails.SelectMany(x => x.VotingCards))
        {
            votingCard.CountOfReceivedVotingCards = 0;
        }

        await _ccDetailsRepo.UpdateRange(ccDetails);
    }

    /// <summary>
    /// Syncs the ContestCountingCircleDetails (adds missing ones, removes odd, updates existing).
    /// </summary>
    /// <param name="contest">The contest with the existing CountingCircleDetails loaded.</param>
    /// <param name="doiCountingCircles">The counting circles of the doi of the contest.</param>
    /// <param name="eVoting">
    /// If null, existing values are used if present or false if no value exists yet.
    /// If a value is set all options eVoting flag is set according to the provided value.
    /// </param>
    /// <returns>A task which resolves to all removed details.</returns>
    private async Task<List<ContestCountingCircleDetails>> Sync(
        Contest contest,
        IEnumerable<DomainOfInfluenceCountingCircle> doiCountingCircles,
        bool? eVoting = null)
    {
        var existingValues = contest.CountingCircleDetails.ToDictionary(x => x.CountingCircleId);
        var toAdd = new List<ContestCountingCircleDetails>();

        foreach (var cc in doiCountingCircles.Select(x => x.CountingCircle))
        {
            if (existingValues.TryGetValue(cc.Id, out var detail))
            {
                existingValues.Remove(cc.Id);

                if (eVoting.HasValue && detail.EVoting != eVoting)
                {
                    detail.EVoting = eVoting.Value;
                    await _ccDetailsRepo.UpdateIgnoreRelations(detail);
                }

                continue;
            }

            toAdd.Add(new ContestCountingCircleDetails
            {
                Id = AusmittlungUuidV5.BuildContestCountingCircleDetails(contest.Id, cc.BasisCountingCircleId, contest.TestingPhaseEnded),
                ContestId = contest.Id,
                CountingCircleId = cc.Id,
                EVoting = eVoting == true,
            });
        }

        var toRemove = existingValues.Values.ToList();

        await _ccDetailsRepo.DeleteRangeByKey(toRemove.Select(x => x.Id));
        await _ccDetailsRepo.CreateRange(toAdd);
        return toRemove;
    }
}
