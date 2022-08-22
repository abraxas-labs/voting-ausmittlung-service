// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ValidationTests;

public class ComparisonVotingChannelsValidationTest : BaseValidationTest<ContestCountingCircleDetailsValidator, ContestCountingCircleDetails>
{
    public ComparisonVotingChannelsValidationTest()
        : base(SharedProto.Validation.ComparisonVotingChannels)
    {
    }

    [Fact]
    public void Test()
    {
        var context = BuildValidationContext();
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);

        EnsureHasCount(validationResults, 4);
        validationResults.MatchSnapshot();
    }

    [Fact]
    public void ShouldReturnEmptyWhenThresholdIsNull()
    {
        var context = BuildValidationContext(x =>
        {
            x.PlausibilisationConfiguration!.ComparisonVotingChannelConfigurations.Single(y => y.VotingChannel == VotingChannel.BallotBox)
                .ThresholdPercent = null;
        });
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);

        EnsureHasCount(validationResults, 3);
    }

    [Fact]
    public void ShouldReturnIsNotValidWhenDeviationGreaterThanThreshold()
    {
        var context = BuildValidationContext(x =>
        {
            x.PlausibilisationConfiguration!.ComparisonVotingChannelConfigurations.Single(y => y.VotingChannel == VotingChannel.BallotBox)
                .ThresholdPercent = 0.1M;
        });
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);

        validationResults.Single(x => ((ValidationComparisonVotingChannelsData)x.Data!).VotingChannel == VotingChannel.BallotBox)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void ShouldReturnEmptyWhenNoPreviousContestOrCountingCircleDidNotExist()
    {
        var context = BuildValidationContext(null, null, null, false);
        var validationResults = Validate(context.CurrentContestCountingCircleDetails, context);

        EnsureHasCount(validationResults, 0);
    }
}
