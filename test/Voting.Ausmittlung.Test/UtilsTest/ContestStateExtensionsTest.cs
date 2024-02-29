// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using FluentAssertions;
using Voting.Ausmittlung.Data.Models;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class ContestStateExtensionsTest
{
    [Theory]
    [InlineData(ContestState.Unspecified, false)]
    [InlineData(ContestState.TestingPhase, false)]
    [InlineData(ContestState.Active, false)]
    [InlineData(ContestState.PastLocked, true)]
    [InlineData(ContestState.PastUnlocked, false)]
    [InlineData(ContestState.Archived, true)]
    public void TestIsLocked(ContestState state, bool expectedResult)
    {
        state.IsLocked().Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(ContestState.Unspecified, false)]
    [InlineData(ContestState.TestingPhase, false)]
    [InlineData(ContestState.Active, true)]
    [InlineData(ContestState.PastLocked, false)]
    [InlineData(ContestState.PastUnlocked, true)]
    [InlineData(ContestState.Archived, false)]
    public void TestIsActiveOrUnlocked(ContestState state, bool expectedResult)
    {
        state.IsActiveOrUnlocked().Should().Be(expectedResult);
    }
}
