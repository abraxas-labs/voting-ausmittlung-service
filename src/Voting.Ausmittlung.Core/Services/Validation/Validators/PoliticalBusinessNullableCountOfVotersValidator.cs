// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Utils;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class PoliticalBusinessNullableCountOfVotersValidator : IValidator<PoliticalBusinessNullableCountOfVoters>
{
    public IEnumerable<ValidationResult> Validate(PoliticalBusinessNullableCountOfVoters data, ValidationContext context)
    {
        var pbType = context.PoliticalBusinessType;

        yield return ValidateCountOfVotersNotNull(data, pbType);

        yield return ValidateReceivedBallotsLessOrEqualValidVotingCards(
            data,
            context.CurrentContestCountingCircleDetails,
            pbType,
            context.PoliticalBusinessDomainOfInfluence.Type);

        yield return ValidateAccountedBallotsLessOrEqualValidVotingCards(
            data,
            context.CurrentContestCountingCircleDetails,
            pbType,
            context.PoliticalBusinessDomainOfInfluence.Type);

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
            countOfVoters.ConventionalSubTotal is { AccountedBallots: not null, BlankBallots: not null, InvalidBallots: not null, ReceivedBallots: not null },
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

        var currentDoiType = context.PoliticalBusinessDomainOfInfluence.Type;
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
        var (validVotingCards, _) = context.CurrentContestCountingCircleDetails.SumVotingCards(context.PoliticalBusinessDomainOfInfluence.Type);
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
                PoliticalBusinessType = context.PoliticalBusinessType,
            },
            true);
    }

    private Dictionary<DomainOfInfluenceType, List<decimal>> GetVoterParticipationsByDomainOfInfluenceType(ValidationContext context)
    {
        var cc = context.CurrentContestCountingCircleDetails.CountingCircle;

        var ballotVoterParticipations = cc.VoteResults
            .Where(x => x.State.IsSubmissionDone())
            .SelectMany(x => x.Results.Select(y => (x.PoliticalBusiness.DomainOfInfluence.Type, y.CountOfVoters.VoterParticipation)));

        var electionResultVoterParticipations = cc.ProportionalElectionResults.OfType<ElectionResult>()
            .Concat(cc.MajorityElectionResults)
            .Where(x => x.State.IsSubmissionDone())
            .Select(x => (x.PoliticalBusiness.DomainOfInfluence.Type, x.CountOfVoters.VoterParticipation));

        return ballotVoterParticipations.Concat(electionResultVoterParticipations)
            .GroupBy(x => x.Type)
            .ToDictionary(x => x.Key, x => x.Select(y => y.VoterParticipation).ToList());
    }
}
