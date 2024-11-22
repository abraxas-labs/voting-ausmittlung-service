// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public abstract class ElectionEndResultWriter<TElectionEndResultAvailableLotDecision, TCandidate, TAggregate, TEndResult>
    : PoliticalBusinessEndResultWriter<TAggregate, TEndResult>
    where TElectionEndResultAvailableLotDecision : ElectionEndResultAvailableLotDecision<TCandidate>
    where TCandidate : DataModels.ElectionCandidate
    where TAggregate : BaseEventSourcingAggregate, IPoliticalBusinessEndResultAggregate
    where TEndResult : DataModels.PoliticalBusinessEndResultBase
{
    protected ElectionEndResultWriter(
        ILogger logger,
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        ContestService contestService,
        PermissionService permissionService,
        SecondFactorTransactionWriter secondFactorTransactionWriter)
        : base(logger, aggregateRepository, aggregateFactory, contestService, permissionService, secondFactorTransactionWriter)
    {
    }

    protected void EnsureValidCandidates(
        IReadOnlyCollection<ElectionEndResultLotDecision> lotDecisions,
        IReadOnlyCollection<TElectionEndResultAvailableLotDecision> availableLotDecisions)
    {
        if (lotDecisions.Count == 0)
        {
            throw new ValidationException("must contain at least one lot decision");
        }

        var candidateIdsInLotDecisions = lotDecisions
            .Select(x => x.CandidateId)
            .ToList();
        if (candidateIdsInLotDecisions.Count != candidateIdsInLotDecisions.Distinct().Count())
        {
            throw new ValidationException("a candidate may only appear once in the lot decisions");
        }

        var candidateIdsInAvailableLotDecisions = availableLotDecisions
            .Select(x => x.Candidate.Id)
            .ToList();
        if (candidateIdsInLotDecisions.Any(id => !candidateIdsInAvailableLotDecisions.Contains(id)))
        {
            throw new ValidationException("candidate id found which not exists in available lot decisions");
        }
    }

    protected void EnsureValidRanksInLotDecisions(
        IReadOnlyCollection<ElectionEndResultLotDecision> lotDecisions,
        IReadOnlyCollection<TElectionEndResultAvailableLotDecision> availableLotDecisions)
    {
        var candidateIdsInLotDecisions = lotDecisions.Select(x => x.CandidateId).ToList();

        var relatedAvailableLotDecisionGroups = availableLotDecisions
            .GroupBy(x => x.VoteCount)
            .ToDictionary(x => x.Key, x => new { Count = x.Count(), CandidateIds = x.Select(c => c.Candidate.Id).ToList() });

        foreach (var (relatedVoteCount, relatedAvailabeLotDecisionGroup) in relatedAvailableLotDecisionGroups)
        {
            var relatedLotDecisions = lotDecisions
                .Where(l => relatedAvailabeLotDecisionGroup.CandidateIds.Contains(l.CandidateId))
                .ToList();

            if (relatedLotDecisions.Count == 0)
            {
                continue;
            }

            if (!relatedLotDecisions.All(l => l.Rank == null) && !relatedLotDecisions.All(l => l.Rank != null))
            {
                throw new ValidationException($"Either all related lot decisions of the group with vote count {relatedVoteCount} must have a rank or none");
            }

            if (relatedLotDecisions.Count != relatedAvailabeLotDecisionGroup.Count)
            {
                throw new ValidationException($"A related lot decision of the group with vote count {relatedVoteCount} is missing");
            }
        }

        // min and max rank is determined by the vote count of the candidate and how often the same vote count appears
        // we cannot use max for MaxRank, because per default the candidates with the same vote count will have the same rank
        var lotDecisionMinMaxRankByVoteCount = availableLotDecisions
            .GroupBy(availableLotDecision => availableLotDecision.VoteCount)
            .ToDictionary(x => x.Key, x => new
            {
                MinRank = x.Min(availableLotDecision => availableLotDecision.OriginalRank),
                MaxRank = x.Min(availableLotDecision => availableLotDecision.OriginalRank) + x.Count() - 1,
            });

        var availableLotDecisionsForCandidateInLotDecisions = availableLotDecisions
            .Where(candEndResult => candidateIdsInLotDecisions.Contains(candEndResult.Candidate.Id))
            .ToList();

        // a rank may only be taken once, for all lot decisions in the same election
        var takenRanks = new HashSet<int>();
        foreach (var availableLotDecision in availableLotDecisionsForCandidateInLotDecisions)
        {
            var lotDecisionMinMaxRank = lotDecisionMinMaxRankByVoteCount[availableLotDecision.VoteCount];
            var lotDecision = lotDecisions.Single(x => x.CandidateId == availableLotDecision.Candidate.Id);

            if (lotDecision.Rank < lotDecisionMinMaxRank.MinRank
                || lotDecision.Rank > lotDecisionMinMaxRank.MaxRank
                || (lotDecision.Rank.HasValue && !takenRanks.Add(lotDecision.Rank.Value)))
            {
                throw new ValidationException("bad rank or rank already taken in existing lot decisions");
            }
        }
    }
}
