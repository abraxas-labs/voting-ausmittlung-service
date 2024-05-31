// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Utils;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class PoliticalBusinessNullableCountOfVotersValidator : IValidator<PoliticalBusinessNullableCountOfVoters>
{
    public IEnumerable<ValidationResult> Validate(PoliticalBusinessNullableCountOfVoters data, ValidationContext context)
    {
        if (context.PoliticalBusinessType == null)
        {
            yield break;
        }

        var pbType = context.PoliticalBusinessType.Value;

        yield return ValidateCountOfVotersNotNull(data, pbType);

        yield return ValidateReceivedBallotsLessOrEqualValidVotingCards(
            data,
            context.CurrentContestCountingCircleDetails,
            pbType,
            context.PoliticalBusinessDomainOfInfluenceType);

        yield return ValidateAccountedBallotsLessOrEqualValidVotingCards(
            data,
            context.CurrentContestCountingCircleDetails,
            pbType,
            context.PoliticalBusinessDomainOfInfluenceType);

        yield return ValidateAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots(
            data,
            pbType);

        foreach (var result in ValidateComparisonVoterParticipations(data, context))
        {
            yield return result;
        }

        if (context.PlausibilisationConfiguration?.ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent != null)
        {
            yield return ValidateComparisonVotingCardsAndValidBallots(data, context);
        }
    }

    private ValidationResult ValidateCountOfVotersNotNull(
        PoliticalBusinessNullableCountOfVoters countOfVoters,
        PoliticalBusinessType pbType)
    {
        return new ValidationResult(
            SharedProto.Validation.PoliticalBusinessCountOfVotersNotNull,
            countOfVoters.ConventionalAccountedBallots.HasValue && countOfVoters.ConventionalBlankBallots.HasValue && countOfVoters.ConventionalInvalidBallots.HasValue && countOfVoters.ConventionalReceivedBallots.HasValue,
            new ValidationPoliticalBusinessData
            {
                PoliticalBusinessType = pbType,
            });
    }

    private ValidationResult ValidateReceivedBallotsLessOrEqualValidVotingCards(
        PoliticalBusinessNullableCountOfVoters countOfVoters,
        ContestCountingCircleDetails ccDetails,
        PoliticalBusinessType pbType,
        DomainOfInfluenceType pbDoiType)
    {
        var (valid, _) = ccDetails.SumVotingCards(pbDoiType);
        return new ValidationResult(
            SharedProto.Validation.PoliticalBusinessReceivedBallotsLessOrEqualValidVotingCards,
            countOfVoters.TotalReceivedBallots <= valid,
            new ValidationPoliticalBusinessData
            {
                PoliticalBusinessType = pbType,
            });
    }

    private ValidationResult ValidateAccountedBallotsLessOrEqualValidVotingCards(
        PoliticalBusinessNullableCountOfVoters countOfVoters,
        ContestCountingCircleDetails ccDetails,
        PoliticalBusinessType pbType,
        DomainOfInfluenceType pbDoiType)
    {
        var (valid, _) = ccDetails.SumVotingCards(pbDoiType);
        return new ValidationResult(
            SharedProto.Validation.PoliticalBusinessAccountedBallotsLessOrEqualValidVotingCards,
            countOfVoters.TotalAccountedBallots <= valid,
            new ValidationPoliticalBusinessData
            {
                PoliticalBusinessType = pbType,
            });
    }

    private ValidationResult ValidateAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots(
        PoliticalBusinessNullableCountOfVoters countOfVoters,
        PoliticalBusinessType pbType)
    {
        return new ValidationResult(
            SharedProto.Validation.PoliticalBusinessAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots,
            countOfVoters.TotalAccountedBallots == countOfVoters.TotalReceivedBallots - countOfVoters.TotalBlankBallots - countOfVoters.TotalInvalidBallots,
            new ValidationPoliticalBusinessData
            {
                PoliticalBusinessType = pbType,
            });
    }

    private IEnumerable<ValidationResult> ValidateComparisonVoterParticipations(
        PoliticalBusinessNullableCountOfVoters countOfVoters,
        ValidationContext context)
    {
        if (context.PlausibilisationConfiguration == null)
        {
            yield break;
        }

        var currentDoiType = context.PoliticalBusinessDomainOfInfluenceType;
        var currentVoterParticipation = countOfVoters.VoterParticipation;

        var comparisonVoterParticipationConfigurations = context.PlausibilisationConfiguration.ComparisonVoterParticipationConfigurations
            .Where(x => x.MainLevel == currentDoiType && x.ThresholdPercent != null)
            .OrderByDescending(x => x.ComparisonLevel)
            .ToList();

        var voterParticipationsByDoiType = GetVoterParticipationsByDomainOfInfluenceType(context);

        foreach (var comparisonVoterParticipationConfiguration in comparisonVoterParticipationConfigurations)
        {
            var thresholdPercent = comparisonVoterParticipationConfiguration.ThresholdPercent!.Value;
            var comparisonDoiType = comparisonVoterParticipationConfiguration.ComparisonLevel;

            if (!voterParticipationsByDoiType.TryGetValue(comparisonDoiType, out var voterParticipations))
            {
                continue;
            }

            var maxDeviationPercent = voterParticipations.Max(vp => RelativeChange.CalculatePercent(vp, currentVoterParticipation));

            yield return new ValidationResult(
                SharedProto.Validation.ComparisonVoterParticipations,
                maxDeviationPercent <= thresholdPercent,
                new ValidationComparisonVoterParticipationsData
                {
                    DeviationPercent = maxDeviationPercent,
                    DomainOfInfluenceType = comparisonDoiType,
                    ThresholdPercent = thresholdPercent,
                },
                true);
        }
    }

    private ValidationResult ValidateComparisonVotingCardsAndValidBallots(
        PoliticalBusinessNullableCountOfVoters countOfVoters,
        ValidationContext context)
    {
        var (validVotingCards, _) = context.CurrentContestCountingCircleDetails.SumVotingCards(context.PoliticalBusinessDomainOfInfluenceType);
        var accountedBallots = countOfVoters.TotalAccountedBallots;

        var deviationPercent = RelativeChange.CalculatePercent(validVotingCards, accountedBallots);
        var thresholdPercent = context.PlausibilisationConfiguration!.ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent!.Value;

        return new ValidationResult(
            SharedProto.Validation.ComparisonValidVotingCardsWithAccountedBallots,
            deviationPercent <= thresholdPercent,
            new ValidationComparisonValidVotingCardsWithAccountedBallotsData
            {
                DeviationPercent = deviationPercent,
                ThresholdPercent = thresholdPercent,
                PoliticalBusinessType = context.PoliticalBusinessType!.Value,
            },
            true);
    }

    private Dictionary<DomainOfInfluenceType, List<decimal>> GetVoterParticipationsByDomainOfInfluenceType(ValidationContext context)
    {
        var cc = context.CurrentContestCountingCircleDetails.CountingCircle;

        var ballotVoterParticipations = cc.VoteResults
            .Where(x => x.SubmissionDone())
            .SelectMany(x => x.Results.Select(y => (x.PoliticalBusiness.DomainOfInfluence.Type, y.CountOfVoters.VoterParticipation)));

        var electionResultVoterParticipations = cc.ProportionalElectionResults.OfType<ElectionResult>()
            .Concat(cc.MajorityElectionResults)
            .Where(x => x.SubmissionDone())
            .Select(x => (x.PoliticalBusiness.DomainOfInfluence.Type, x.CountOfVoters.VoterParticipation));

        return ballotVoterParticipations.Concat(electionResultVoterParticipations)
            .GroupBy(x => x.Type)
            .ToDictionary(x => x.Key, x => x.Select(y => y.VoterParticipation).ToList());
    }
}
