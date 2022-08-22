// (c) Copyright 2022 by Abraxas Informatik AG
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
    where TEndResult : DataModels.PoliticalBusinessEndResult
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
        var requiredLotDecisions = availableLotDecisions.Where(x => x.LotDecisionRequired);
        var candidateIdsInLotDecisions = lotDecisions.Select(x => x.CandidateId).ToList();

        if (requiredLotDecisions.Any(x => !candidateIdsInLotDecisions.Contains(x.Candidate.Id)))
        {
            throw new ValidationException("required lot decision is missing");
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
                || !takenRanks.Add(lotDecision.Rank))
            {
                throw new ValidationException("bad rank or rank already taken in existing lot decisions");
            }
        }
    }
}
