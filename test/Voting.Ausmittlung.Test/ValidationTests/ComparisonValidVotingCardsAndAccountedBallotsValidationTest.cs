// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ValidationTests;

public class ComparisonValidVotingCardsAndAccountedBallotsValidationTest : BaseValidationTest<IValidator<PoliticalBusinessNullableCountOfVoters>, PoliticalBusinessNullableCountOfVoters>
{
    public ComparisonValidVotingCardsAndAccountedBallotsValidationTest()
        : base(SharedProto.Validation.ComparisonValidVotingCardsWithAccountedBallots)
    {
    }

    [Fact]
    public void Test()
    {
        var electionResult = BuildProportionalElectionResult(540);
        var context = BuildValidationContext(electionResult.ProportionalElection.DomainOfInfluence);
        var validationResults = Validate(electionResult.CountOfVoters, context);

        EnsureHasCount(validationResults, 1);
        EnsureIsValid(validationResults, true);

        validationResults.Single().MatchSnapshot();
    }

    [Fact]
    public void ShouldReturnEmptyWhenThresholdIsNull()
    {
        var electionResult = BuildProportionalElectionResult(540);
        var context = BuildValidationContext(electionResult.ProportionalElection.DomainOfInfluence, x =>
        {
            x.PlausibilisationConfiguration!.ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = null;
        });
        var validationResults = Validate(electionResult.CountOfVoters, context);

        EnsureHasCount(validationResults, 0);
    }

    [Fact]
    public void ShouldReturnIsNotValidWhenDeviationGreaterThanThreshold()
    {
        var electionResult = BuildProportionalElectionResult(300);
        var context = BuildValidationContext(electionResult.ProportionalElection.DomainOfInfluence);
        var validationResults = Validate(electionResult.CountOfVoters, context);

        EnsureHasCount(validationResults, 1);
        EnsureIsValid(validationResults, false);
    }

    private ValidationContext BuildValidationContext(DomainOfInfluence domainOfInfluence, Action<ValidationContext>? customizer = null)
    {
        return BuildValidationContext(domainOfInfluence, PoliticalBusinessType.ProportionalElection, customizer, null);
    }

    private ProportionalElectionResult BuildProportionalElectionResult(int accountedBallots)
    {
        return new ProportionalElectionResult
        {
            ProportionalElection = new ProportionalElection
            {
                DomainOfInfluence = new DomainOfInfluence
                {
                    Type = DomainOfInfluenceType.Ch,
                },
            },
            CountOfVoters = new PoliticalBusinessNullableCountOfVoters
            {
                ConventionalSubTotal = new PoliticalBusinessCountOfVotersNullableSubTotal
                {
                    AccountedBallots = accountedBallots,
                },
            },
        };
    }
}
