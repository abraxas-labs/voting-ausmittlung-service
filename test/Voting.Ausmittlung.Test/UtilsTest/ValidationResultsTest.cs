// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using FluentAssertions;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class ValidationResultsTest
{
    [Fact]
    public void ShouldReturnValid()
    {
        var validationResults = new List<ValidationResult>
            {
                new ValidationResult(SharedProto.Validation.PoliticalBusinessBundlesNotInProcess, true),
                new ValidationResult(SharedProto.Validation.PoliticalBusinessReceivedBallotsLessOrEqualValidVotingCards, true),
                new ValidationResult(SharedProto.Validation.PoliticalBusinessAccountedBallotsLessOrEqualValidVotingCards, true),
                new ValidationResult(SharedProto.Validation.ComparisonVoterParticipations, false, null, true),
            };

        var result = validationResults.FirstInvalidOrElseFirstValid();
        result.Validation.Should().Be(SharedProto.Validation.ComparisonVoterParticipations);
    }

    [Fact]
    public void ShouldReturnInvalid()
    {
        var validationResults = new List<ValidationResult>
            {
                new ValidationResult(SharedProto.Validation.PoliticalBusinessBundlesNotInProcess, true),
                new ValidationResult(SharedProto.Validation.PoliticalBusinessReceivedBallotsLessOrEqualValidVotingCards, false),
                new ValidationResult(SharedProto.Validation.PoliticalBusinessAccountedBallotsLessOrEqualValidVotingCards, true),
            };

        var result = validationResults.FirstInvalidOrElseFirstValid();
        result.Validation.Should().Be(SharedProto.Validation.PoliticalBusinessReceivedBallotsLessOrEqualValidVotingCards);
    }
}
