// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils;

public static class ContestCountingCircleElectorateSummaryBuilder
{
    public static ContestCountingCircleElectorateSummary Build(CountingCircle countingCircle, ContestCountingCircleDetails countingCircleDetails, HashSet<DomainOfInfluenceType> requiredDoiTypes)
    {
        var contestElectorates = countingCircle.ContestElectorates.OfType<CountingCircleElectorateBase>().ToList();
        var basisElectorates = countingCircle.Electorates.OfType<CountingCircleElectorateBase>().ToList();

        var effectiveElectoratesBase = BuildEffectiveElectoratesBase(
            contestElectorates.Count > 0
                ? contestElectorates
                : basisElectorates,
            requiredDoiTypes);

        var effectiveElectorates = new List<CountingCircleElectorateBase>();

        foreach (var electorate in effectiveElectoratesBase)
        {
            var electorateVotingCards = countingCircleDetails.VotingCards.Where(vc => electorate.DomainOfInfluenceTypes.Contains(vc.DomainOfInfluenceType)).ToList();

            if (HasEqualVotingCardResultsPerDomainOfInfluenceType(electorateVotingCards))
            {
                effectiveElectorates.Add(electorate);
                continue;
            }

            foreach (var doiType in electorate.DomainOfInfluenceTypes)
            {
                effectiveElectorates.Add(new CountingCircleElectorate()
                {
                    DomainOfInfluenceTypes = new() { doiType },
                });
            }
        }

        effectiveElectorates = effectiveElectorates.OrderBy(e => e.DomainOfInfluenceTypes[0]).ToList();
        return new(effectiveElectorates, contestElectorates);
    }

    private static IReadOnlyCollection<CountingCircleElectorateBase> BuildEffectiveElectoratesBase(IReadOnlyCollection<CountingCircleElectorateBase> electorates, HashSet<DomainOfInfluenceType> requiredDoiTypes)
    {
        var effectiveElectorates = new List<CountingCircleElectorateBase>();

        foreach (var electorate in electorates)
        {
            var effectiveElectorate = new CountingCircleElectorate { DomainOfInfluenceTypes = electorate.DomainOfInfluenceTypes };
            effectiveElectorate.DomainOfInfluenceTypes = effectiveElectorate.DomainOfInfluenceTypes.Where(doiType => requiredDoiTypes.Contains(doiType)).ToList();
            if (effectiveElectorate.DomainOfInfluenceTypes.Count == 0)
            {
                continue;
            }

            effectiveElectorates.Add(effectiveElectorate);
        }

        var usedDoiTypes = effectiveElectorates.SelectMany(e => e.DomainOfInfluenceTypes).ToList();
        var unusedRequiredDoiTypes = requiredDoiTypes
            .Where(doiType => !usedDoiTypes.Contains(doiType))
            .OrderBy(d => d)
            .ToList();

        if (unusedRequiredDoiTypes.Count > 0)
        {
            effectiveElectorates.Add(new CountingCircleElectorate()
            {
                DomainOfInfluenceTypes = unusedRequiredDoiTypes,
            });
        }

        return effectiveElectorates;
    }

    private static bool HasEqualVotingCardResultsPerDomainOfInfluenceType(IReadOnlyCollection<VotingCardResultDetail> votingCards)
    {
        var votingCardsByChannelAndValid = votingCards
            .GroupBy(vc => new { vc.Channel, vc.Valid })
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var (_, groupedVotingCards) in votingCardsByChannelAndValid)
        {
            if (groupedVotingCards.DistinctBy(vc => vc.CountOfReceivedVotingCards).Count() > 1)
            {
                return false;
            }
        }

        return true;
    }
}
