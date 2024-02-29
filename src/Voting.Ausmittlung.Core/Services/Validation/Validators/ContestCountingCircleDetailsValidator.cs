// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Utils;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class ContestCountingCircleDetailsValidator : IValidator<ContestCountingCircleDetails>
{
    public IEnumerable<ValidationResult> Validate(ContestCountingCircleDetails data, ValidationContext context)
    {
        yield return ValidateCountOfVotersNotNull(data);
        yield return ValidateVotingCardsReceivedNotNull(data);
        yield return ValidateVotingCardsLessOrEqualThanCountOfVoters(data);

        if (context.PoliticalBusinessType.HasValue || context.PreviousContestCountingCircleDetails == null || context.PlausibilisationConfiguration == null)
        {
            yield break;
        }

        if (context.ComparisonCountOfVotersConfiguration?.ThresholdPercent != null)
        {
            yield return ValidateComparisonCountOfVoters(
                context.CurrentContestCountingCircleDetails,
                context.PreviousContestCountingCircleDetails,
                context.ComparisonCountOfVotersConfiguration);
        }

        foreach (var result in ValidateComparisonVotingChannels(
            context.CurrentContestCountingCircleDetails,
            context.PreviousContestCountingCircleDetails,
            context.PlausibilisationConfiguration.ComparisonVotingChannelConfigurations))
        {
            yield return result;
        }
    }

    private ValidationResult ValidateCountOfVotersNotNull(ContestCountingCircleDetails details)
    {
        return new ValidationResult(
            SharedProto.Validation.ContestCountingCircleDetailsCountOfVotersNotNull,
            details.CountOfVotersInformationSubTotals.Count > 0 && details.CountOfVotersInformationSubTotals.All(x => x.CountOfVoters.HasValue));
    }

    private ValidationResult ValidateVotingCardsReceivedNotNull(ContestCountingCircleDetails details)
    {
        return new ValidationResult(
            SharedProto.Validation.ContestCountingCircleDetailsVotingCardsReceivedNotNull,
            details.VotingCards.Count > 0 && details.VotingCards.All(x => x.CountOfReceivedVotingCards.HasValue));
    }

    private ValidationResult ValidateVotingCardsLessOrEqualThanCountOfVoters(ContestCountingCircleDetails details)
    {
        return new ValidationResult(
            SharedProto.Validation.ContestCountingCircleDetailsVotingCardsLessOrEqualCountOfVoters,
            details.GetMaxSumOfVotingCards(x => x.Valid + x.Invalid) <= details.TotalCountOfVoters);
    }

    private ValidationResult ValidateComparisonCountOfVoters(
        ContestCountingCircleDetails currentDetails,
        ContestCountingCircleDetails previousDetails,
        ComparisonCountOfVotersConfiguration comparisonCountOfVotersConfig)
    {
        var thresholdPercent = comparisonCountOfVotersConfig.ThresholdPercent!.Value;
        var deviation = Math.Abs(currentDetails.TotalCountOfVoters - previousDetails.TotalCountOfVoters);
        var deviationPercent = RelativeChange.CalculatePercent(previousDetails.TotalCountOfVoters, currentDetails.TotalCountOfVoters);

        return new ValidationResult(
            SharedProto.Validation.ComparisonCountOfVoters,
            deviationPercent <= thresholdPercent,
            new ValidationComparisonCountOfVotersData
            {
                CurrentCount = currentDetails.TotalCountOfVoters,
                PreviousCount = previousDetails.TotalCountOfVoters,
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
        IEnumerable<ComparisonVotingChannelConfiguration> comparisonVotingChannelConfigs)
    {
        var currentCountByVotingChannel = GetCountOfReceivedVotingCardsByVotingChannel(currentDetails);
        var previousCountByVotingChannel = GetCountOfReceivedVotingCardsByVotingChannel(previousDetails);

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

    private IDictionary<VotingChannel, int> GetCountOfReceivedVotingCardsByVotingChannel(ContestCountingCircleDetails ccDetails)
    {
        return ccDetails.VotingCards
            .GroupBy(x => x.Channel, x => x.CountOfReceivedVotingCards)
            .ToDictionary(x => x.Key, x => x.Sum() ?? 0);
    }
}
