// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class ValidationResultsEnsurerUtilTest
{
    private readonly ValidationResultsEnsurerUtils _ensurer = new ValidationResultsEnsurerUtils();

    [Fact]
    public void TestShouldReturn()
    {
        _ensurer.EnsureIsValid(new List<ValidationResult>
                {
                    new ValidationResult(SharedProto.Validation.PoliticalBusinessAccountedBallotsLessOrEqualValidVotingCards, true),
                    new ValidationResult(SharedProto.Validation.PoliticalBusinessBundlesNotInProcess, true),
                });
    }

    [Fact]
    public void TestShouldThrow()
    {
        var ex = Assert.Throws<ValidationException>(() => _ensurer.EnsureIsValid(
            new List<ValidationResult>
            {
                    new ValidationResult(SharedProto.Validation.PoliticalBusinessAccountedBallotsLessOrEqualValidVotingCards, true),
                    new ValidationResult(SharedProto.Validation.PoliticalBusinessBundlesNotInProcess, false),
            }));

        ex.Message.Contains(nameof(SharedProto.Validation.PoliticalBusinessAccountedBallotsLessOrEqualValidVotingCards), StringComparison.Ordinal)
            .Should().BeFalse();

        ex.Message.Contains(nameof(SharedProto.Validation.PoliticalBusinessBundlesNotInProcess), StringComparison.Ordinal)
                .Should().BeTrue();
    }
}
