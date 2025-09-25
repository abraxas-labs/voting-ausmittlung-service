// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ValidationTests;

public class ComparisonCountOfVotersValidationTest : BaseValidationTest<ContestCountingCircleDetailsValidator, ContestCountingCircleDetails>
{
    public ComparisonCountOfVotersValidationTest()
        : base(SharedProto.Validation.ComparisonCountOfVoters)
    {
    }

    [Fact]
    public void Test()
    {
        var context = BuildValidationContext();
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);

        EnsureHasCount(validationResults, 1);
        EnsureIsValid(validationResults, true);
        validationResults.MatchSnapshot();
    }

    [Fact]
    public void ShouldReturnEmptyWhenThresholdIsNull()
    {
        var context = BuildValidationContext(x =>
        {
            x.PlausibilisationConfiguration!.ComparisonCountOfVotersConfigurations.Single(y => y.Category == ComparisonCountOfVotersCategory.A)
                .ThresholdPercent = null;
        });
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);

        EnsureHasCount(validationResults, 0);
    }

    [Fact]
    public void ShouldReturnIsNotValidWhenDeviationGreaterThanThreshold()
    {
        var context = BuildValidationContext(x =>
            x.PlausibilisationConfiguration!.ComparisonCountOfVotersConfigurations.Single(y => y.Category == ComparisonCountOfVotersCategory.A)
                .ThresholdPercent = 0.1M);
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);

        EnsureHasCount(validationResults, 1);
        EnsureIsValid(validationResults, false);
    }

    [Fact]
    public void ShouldReturnEmptyWhenNoPreviousContestOrCountingCircleDidNotExist()
    {
        var context = BuildValidationContext(null, null, false);
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);

        EnsureHasCount(validationResults, 0);
    }

    [Fact]
    public void ShouldReturnEmptyWhenCountingCircleHasNoCategory()
    {
        var context = BuildValidationContext(
            null,
            x => x.CountingCircles.Single().ComparisonCountOfVotersCategory = ComparisonCountOfVotersCategory.Unspecified);

        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);

        EnsureHasCount(validationResults, 0);
    }
}
