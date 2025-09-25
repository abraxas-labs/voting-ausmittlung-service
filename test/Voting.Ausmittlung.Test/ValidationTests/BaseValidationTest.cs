// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ValidationTests;

public abstract class BaseValidationTest<TValidator, TEntity>
    where TValidator : IValidator<TEntity>, new()
{
    private readonly TValidator _validator;

    protected BaseValidationTest(SharedProto.Validation validation)
    {
        _validator = new TValidator();
        Validation = validation;
    }

    protected SharedProto.Validation Validation { get; }

    protected List<ValidationResult> Validate(TEntity data, ValidationContext context)
    {
        return _validator.Validate(data, context)
            .Where(x => x.Validation == Validation)
            .ToList();
    }

    protected ValidationContext BuildValidationContext(
        Action<ValidationContext>? contextCustomizer = null,
        Action<DomainOfInfluence>? responsibleForPlausibilisationDoiCustomizer = null,
        bool hasPreviousContest = true)
    {
        var pbDoi = new DomainOfInfluence
        {
            Type = DomainOfInfluenceType.Ch,
        };

        return BuildValidationContext(
            pbDoi,
            PoliticalBusinessType.ProportionalElection,
            contextCustomizer,
            responsibleForPlausibilisationDoiCustomizer,
            hasPreviousContest);
    }

    protected ValidationContext BuildValidationContext(
        DomainOfInfluence pbDoi,
        PoliticalBusinessType pbType,
        Action<ValidationContext>? contextCustomizer = null,
        Action<DomainOfInfluence>? responsibleForPlausibilisationDoiCustomizer = null,
        bool hasPreviousContest = true)
    {
        var ccId = Guid.Parse("bf2c4c85-e05e-4242-a324-667f3d2dbcbb");
        var basisCcId = Guid.Parse("1bc5e5d0-3e45-46f6-97d9-c16372fceace");

        var responsibleForPlausibilisationDoi = new DomainOfInfluence
        {
            PlausibilisationConfiguration = new PlausibilisationConfiguration
            {
                ComparisonVoterParticipationConfigurations = new List<ComparisonVoterParticipationConfiguration>
                    {
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Ch, ComparisonLevel = DomainOfInfluenceType.Ch, ThresholdPercent = 30 },
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Ct, ComparisonLevel = DomainOfInfluenceType.Ch, ThresholdPercent = 30 },
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Ct, ComparisonLevel = DomainOfInfluenceType.Ct, ThresholdPercent = 30 },
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Mu, ComparisonLevel = DomainOfInfluenceType.Ch, ThresholdPercent = 30 },
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Mu, ComparisonLevel = DomainOfInfluenceType.Ct, ThresholdPercent = 30 },
                        new ComparisonVoterParticipationConfiguration { MainLevel = DomainOfInfluenceType.Mu, ComparisonLevel = DomainOfInfluenceType.Mu, ThresholdPercent = 30 },
                    },
                ComparisonCountOfVotersConfigurations = new List<ComparisonCountOfVotersConfiguration>()
                    {
                        new ComparisonCountOfVotersConfiguration { Category = ComparisonCountOfVotersCategory.A, ThresholdPercent = 5 },
                        new ComparisonCountOfVotersConfiguration { Category = ComparisonCountOfVotersCategory.B, ThresholdPercent = 3.5M },
                        new ComparisonCountOfVotersConfiguration { Category = ComparisonCountOfVotersCategory.C, ThresholdPercent = 2 },
                    },
                ComparisonVotingChannelConfigurations = new List<ComparisonVotingChannelConfiguration>()
                    {
                        new ComparisonVotingChannelConfiguration { VotingChannel = VotingChannel.BallotBox, ThresholdPercent = 4 },
                        new ComparisonVotingChannelConfiguration { VotingChannel = VotingChannel.ByMail, ThresholdPercent = 4 },
                        new ComparisonVotingChannelConfiguration { VotingChannel = VotingChannel.EVoting, ThresholdPercent = 10 },
                        new ComparisonVotingChannelConfiguration { VotingChannel = VotingChannel.Paper, ThresholdPercent = 4 },
                    },
                ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = 20,
            },
            CountingCircles = new List<DomainOfInfluenceCountingCircle>
                {
                    new DomainOfInfluenceCountingCircle { CountingCircleId = ccId, ComparisonCountOfVotersCategory = ComparisonCountOfVotersCategory.A },
                },
        };

        responsibleForPlausibilisationDoiCustomizer?.Invoke(responsibleForPlausibilisationDoi);

        var currentCcDetails = new ContestCountingCircleDetails
        {
            CountingCircle = new CountingCircle
            {
                Id = ccId,
                BasisCountingCircleId = basisCcId,
            },
            Contest = new Contest
            {
                Date = new DateTime(2029, 2, 12, 0, 0, 0, DateTimeKind.Utc),
                PreviousContestId = hasPreviousContest ? Guid.Parse("e86a8cff-b3d4-415f-9850-34e5325a73c7") : (Guid?)null,
            },
            VotingCards = new List<VotingCardResultDetail>
            {
                    new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfReceivedVotingCards = 400 },
                    new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = false, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfReceivedVotingCards = 20 },
                    new VotingCardResultDetail { Channel = VotingChannel.EVoting, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfReceivedVotingCards = 100 },
                    new VotingCardResultDetail { Channel = VotingChannel.Paper, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfReceivedVotingCards = 10 },
                    new VotingCardResultDetail { Channel = VotingChannel.BallotBox, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfReceivedVotingCards = 90 },
                    new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfReceivedVotingCards = 390 },
                    new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = false, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfReceivedVotingCards = 10 },
                    new VotingCardResultDetail { Channel = VotingChannel.EVoting, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfReceivedVotingCards = 50 },
                    new VotingCardResultDetail { Channel = VotingChannel.Paper, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfReceivedVotingCards = 50 },
                    new VotingCardResultDetail { Channel = VotingChannel.BallotBox, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfReceivedVotingCards = 100 },
                    new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.An, CountOfReceivedVotingCards = 400 },
                    new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = false, DomainOfInfluenceType = DomainOfInfluenceType.An, CountOfReceivedVotingCards = 20 },
                    new VotingCardResultDetail { Channel = VotingChannel.EVoting, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.An, CountOfReceivedVotingCards = 50 },
                    new VotingCardResultDetail { Channel = VotingChannel.Paper, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.An, CountOfReceivedVotingCards = 50 },
                    new VotingCardResultDetail { Channel = VotingChannel.BallotBox, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.An, CountOfReceivedVotingCards = 100 },
            },
            CountOfVotersInformationSubTotals = new List<CountOfVotersInformationSubTotal>
            {
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Swiss, Sex = SexType.Male, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfVoters = 500 },
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Swiss, Sex = SexType.Female, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfVoters = 460 },
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Swiss, Sex = SexType.Male, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfVoters = 450 },
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Swiss, Sex = SexType.Female, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfVoters = 400 },
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Swiss, Sex = SexType.Male, DomainOfInfluenceType = DomainOfInfluenceType.An, CountOfVoters = 450 },
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Swiss, Sex = SexType.Female, DomainOfInfluenceType = DomainOfInfluenceType.An, CountOfVoters = 400 },
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Foreigner, Sex = SexType.Male, DomainOfInfluenceType = DomainOfInfluenceType.An, CountOfVoters = 10 },
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Foreigner, Sex = SexType.Female, DomainOfInfluenceType = DomainOfInfluenceType.An, CountOfVoters = 10 },
            },
        };

        var previousCcDetails = new ContestCountingCircleDetails
        {
            CountingCircle = new CountingCircle
            {
                BasisCountingCircleId = basisCcId,
            },
            Contest = new Contest
            {
                Date = new DateTime(2017, 5, 10, 0, 0, 0, DateTimeKind.Utc),
            },
            VotingCards = new List<VotingCardResultDetail>
                {
                    new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfReceivedVotingCards = 416 },
                    new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = false, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfReceivedVotingCards = 20 },
                    new VotingCardResultDetail { Channel = VotingChannel.EVoting, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfReceivedVotingCards = 90 },
                    new VotingCardResultDetail { Channel = VotingChannel.Paper, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfReceivedVotingCards = 10 },
                    new VotingCardResultDetail { Channel = VotingChannel.BallotBox, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfReceivedVotingCards = 110 },
                    new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfReceivedVotingCards = 390 },
                    new VotingCardResultDetail { Channel = VotingChannel.ByMail, Valid = false, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfReceivedVotingCards = 10 },
                    new VotingCardResultDetail { Channel = VotingChannel.EVoting, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfReceivedVotingCards = 50 },
                    new VotingCardResultDetail { Channel = VotingChannel.Paper, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfReceivedVotingCards = 50 },
                    new VotingCardResultDetail { Channel = VotingChannel.BallotBox, Valid = true, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfReceivedVotingCards = 100 },
                },
            CountOfVotersInformationSubTotals = new List<CountOfVotersInformationSubTotal>
            {
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Swiss, Sex = SexType.Male, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfVoters = 600 },
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Swiss, Sex = SexType.Female, DomainOfInfluenceType = DomainOfInfluenceType.Ch, CountOfVoters = 400 },
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Swiss, Sex = SexType.Male, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfVoters = 500 },
                new CountOfVotersInformationSubTotal { VoterType = VoterType.Swiss, Sex = SexType.Female, DomainOfInfluenceType = DomainOfInfluenceType.Ct, CountOfVoters = 400 },
            },
        };

        var context = new ValidationContext(
            responsibleForPlausibilisationDoi,
            pbDoi,
            pbType,
            currentCcDetails,
            hasPreviousContest ? previousCcDetails : null);

        contextCustomizer?.Invoke(context);
        return context;
    }

    protected void EnsureIsValid(IEnumerable<ValidationResult> validationResults, bool isValid)
    {
        validationResults.All(x => x.IsValid == isValid).Should().BeTrue();
    }

    protected void EnsureHasCount(IEnumerable<ValidationResult> validationResults, int count)
    {
        validationResults.Should().HaveCount(count);
    }
}
