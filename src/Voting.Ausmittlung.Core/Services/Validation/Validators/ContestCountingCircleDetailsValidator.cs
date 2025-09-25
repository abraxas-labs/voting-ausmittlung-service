// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Utils;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class ContestCountingCircleDetailsValidator : IValidator<ContestCountingCircleDetails>
{
    public IEnumerable<ValidationResult> Validate(ContestCountingCircleDetails data, ValidationContext context)
    {
        yield return ValidateCountOfVotersNotNull(data, context.PoliticalBusinessDomainOfInfluence.Type);
        yield return ValidateVotingCardsReceivedNotNull(data, context.PoliticalBusinessDomainOfInfluence.Type);
        yield return ValidateVotingCardsLessOrEqualThanCountOfVoters(data, context.PoliticalBusinessDomainOfInfluence);

        if (context.PreviousContestCountingCircleDetails == null || context.PlausibilisationConfiguration == null)
        {
            yield break;
        }

        if (context.ComparisonCountOfVotersConfiguration?.ThresholdPercent != null)
        {
            yield return ValidateComparisonCountOfVoters(
                context.CurrentContestCountingCircleDetails,
                context.PreviousContestCountingCircleDetails,
                context.ComparisonCountOfVotersConfiguration,
                context.PoliticalBusinessDomainOfInfluence);
        }

        foreach (var result in ValidateComparisonVotingChannels(
            context.CurrentContestCountingCircleDetails,
            context.PreviousContestCountingCircleDetails,
            context.PlausibilisationConfiguration.ComparisonVotingChannelConfigurations,
            context.PoliticalBusinessDomainOfInfluence.Type))
        {
            yield return result;
        }
    }

    private ValidationResult ValidateCountOfVotersNotNull(ContestCountingCircleDetails details, DomainOfInfluenceType domainOfInfluenceType)
    {
        var subTotals = details.CountOfVotersInformationSubTotals.Where(st => st.DomainOfInfluenceType == domainOfInfluenceType).ToList();

        return new ValidationResult(
            SharedProto.Validation.ContestCountingCircleDetailsCountOfVotersNotNull,
            subTotals.Count > 0 && subTotals.All(x => x.CountOfVoters.HasValue));
    }

    private ValidationResult ValidateVotingCardsReceivedNotNull(ContestCountingCircleDetails details, DomainOfInfluenceType domainOfInfluenceType)
    {
        var votingCards = details.VotingCards.Where(vc => vc.DomainOfInfluenceType == domainOfInfluenceType).ToList();

        return new ValidationResult(
            SharedProto.Validation.ContestCountingCircleDetailsVotingCardsReceivedNotNull,
            votingCards.Count > 0 && votingCards.All(x => x.CountOfReceivedVotingCards.HasValue));
    }

    private ValidationResult ValidateVotingCardsLessOrEqualThanCountOfVoters(ContestCountingCircleDetails details, DomainOfInfluence domainOfInfluence)
    {
        return new ValidationResult(
            SharedProto.Validation.ContestCountingCircleDetailsVotingCardsLessOrEqualCountOfVoters,
            GetTotalSumVotingCards(details, domainOfInfluence.Type) <= details.GetTotalCountOfVotersForDomainOfInfluence(domainOfInfluence));
    }

    private ValidationResult ValidateComparisonCountOfVoters(
        ContestCountingCircleDetails currentDetails,
        ContestCountingCircleDetails previousDetails,
        ComparisonCountOfVotersConfiguration comparisonCountOfVotersConfig,
        DomainOfInfluence domainOfInfluence)
    {
        var thresholdPercent = comparisonCountOfVotersConfig.ThresholdPercent!.Value;

        var currentDetailsTotalCountOfVoters = currentDetails.GetTotalCountOfVotersForDomainOfInfluence(domainOfInfluence);
        var previousDetailsTotalCountOfVoters = previousDetails.GetTotalCountOfVotersForDomainOfInfluence(domainOfInfluence);

        var deviation = Math.Abs(currentDetailsTotalCountOfVoters - previousDetailsTotalCountOfVoters);
        var deviationPercent = RelativeChange.CalculatePercent(previousDetailsTotalCountOfVoters, currentDetailsTotalCountOfVoters);

        return new ValidationResult(
            SharedProto.Validation.ComparisonCountOfVoters,
            deviationPercent <= thresholdPercent,
            new ValidationComparisonCountOfVotersData
            {
                CurrentCount = currentDetailsTotalCountOfVoters,
                PreviousCount = previousDetailsTotalCountOfVoters,
                PreviousDate = previousDetails.Contest.Date,
                ThresholdPercent = thresholdPercent,
                Deviation = deviation,
                DeviationPercent = deviationPercent,
            },
            true);
    }

    private IEnumerable<ValidationResult> ValidateComparisonVotingChannels(
        ContestCountingCircleDetails currentDetails,
        ContestCountingCircleDetails previousDetails,
        IEnumerable<ComparisonVotingChannelConfiguration> comparisonVotingChannelConfigs,
        DomainOfInfluenceType domainOfInfluenceType)
    {
        var currentCountByVotingChannel = GetCountOfReceivedVotingCardsByVotingChannel(currentDetails, domainOfInfluenceType);
        var previousCountByVotingChannel = GetCountOfReceivedVotingCardsByVotingChannel(previousDetails, domainOfInfluenceType);

        foreach (var comparisonVotingChannelConfig in comparisonVotingChannelConfigs)
        {
            if (comparisonVotingChannelConfig.ThresholdPercent == null)
            {
                continue;
            }

            var votingChannel = comparisonVotingChannelConfig.VotingChannel;

            var hasCurrentCount = currentCountByVotingChannel.TryGetValue(votingChannel, out var currentCount);
            var hasPreviousCount = previousCountByVotingChannel.TryGetValue(votingChannel, out var previousCount);

            if (!hasCurrentCount || !hasPreviousCount)
            {
                continue;
            }

            var thresholdPercent = comparisonVotingChannelConfig.ThresholdPercent.Value;
            var deviation = Math.Abs(currentCount - previousCount);
            var deviationPercent = RelativeChange.CalculatePercent(previousCount, currentCount);

            yield return new ValidationResult(
                SharedProto.Validation.ComparisonVotingChannels,
                deviationPercent <= thresholdPercent,
                new ValidationComparisonVotingChannelsData
                {
                    CurrentCount = currentCount,
                    PreviousCount = previousCount,
                    Deviation = deviation,
                    DeviationPercent = deviationPercent,
                    VotingChannel = votingChannel,
                    PreviousDate = previousDetails.Contest.Date,
                    ThresholdPercent = thresholdPercent,
                },
                true);
        }
    }

    private IDictionary<VotingChannel, int> GetCountOfReceivedVotingCardsByVotingChannel(ContestCountingCircleDetails ccDetails, DomainOfInfluenceType domainOfInfluenceType)
    {
        return ccDetails.VotingCards
            .Where(vc => vc.DomainOfInfluenceType == domainOfInfluenceType)
            .GroupBy(x => x.Channel, x => x.CountOfReceivedVotingCards)
            .ToDictionary(x => x.Key, x => x.Sum() ?? 0);
    }

    private int GetTotalSumVotingCards(ContestCountingCircleDetails ccDetails, DomainOfInfluenceType doiType)
    {
        var (valid, invalid) = ccDetails.SumVotingCards(doiType);
        return valid + invalid;
    }
}
