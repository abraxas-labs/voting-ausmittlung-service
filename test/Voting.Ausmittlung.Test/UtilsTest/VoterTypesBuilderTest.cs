// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class VoterTypesBuilderTest
{
    [Fact]
    public void BuildVoterTypesForNullDoiShouldReturnEmpty()
    {
        var voterTypes = VoterTypesBuilder.BuildEnabledVoterTypes((DomainOfInfluence?)null);
        voterTypes.Should().HaveCount(0);
    }

    [Fact]
    public void BuildVoterTypesWithAllVoterTypes()
    {
        var voterTypes = VoterTypesBuilder.BuildEnabledVoterTypes(new DomainOfInfluence
        {
            SwissAbroadVotingRight = SwissAbroadVotingRight.OnEveryCountingCircle,
            HasForeignerVoters = true,
            HasMinorVoters = true,
        });

        voterTypes.SequenceEqual(new[] { VoterType.Swiss, VoterType.SwissAbroad, VoterType.Foreigner, VoterType.Minor });
    }

    [Theory]
    [InlineData(SwissAbroadVotingRight.SeparateCountingCircle, false)]
    [InlineData(SwissAbroadVotingRight.NoRights, false)]
    [InlineData(SwissAbroadVotingRight.OnEveryCountingCircle, true)]
    public void BuildVoterTypesWithSwissAbroadDoi(SwissAbroadVotingRight swissAbroadVotingRight, bool shouldContainSwissAbroadVoterType)
    {
        var voterTypes = VoterTypesBuilder.BuildEnabledVoterTypes(new DomainOfInfluence { SwissAbroadVotingRight = swissAbroadVotingRight });
        voterTypes.Any(v => v == VoterType.SwissAbroad).Should().Be(shouldContainSwissAbroadVoterType);
        voterTypes.Count.Should().Be(shouldContainSwissAbroadVoterType ? 2 : 1);
    }

    [Fact]
    public void BuildVoterTypesWithForeigners()
    {
        var voterTypes = VoterTypesBuilder.BuildEnabledVoterTypes(new DomainOfInfluence { HasForeignerVoters = true });
        voterTypes.SequenceEqual(new[] { VoterType.Swiss, VoterType.Foreigner }).Should().BeTrue();
    }

    [Fact]
    public void BuildVoterTypesWithMinors()
    {
        var voterTypes = VoterTypesBuilder.BuildEnabledVoterTypes(new DomainOfInfluence { HasMinorVoters = true });
        voterTypes.SequenceEqual(new[] { VoterType.Swiss, VoterType.Minor }).Should().BeTrue();
    }
}
