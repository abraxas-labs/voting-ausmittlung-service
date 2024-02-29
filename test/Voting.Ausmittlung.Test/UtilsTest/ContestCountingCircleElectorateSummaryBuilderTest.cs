// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class ContestCountingCircleElectorateSummaryBuilderTest
{
    [Fact]
    public void NoPersistedElectoratesShouldReturnOneElectorateWithRequireDoiTypes()
    {
        var summary = Build(
            new(),
            new[] { DomainOfInfluenceType.Ct, DomainOfInfluenceType.Ch, DomainOfInfluenceType.Bz });
        summary.ContestElectorates.Should().BeEmpty();
        summary.EffectiveElectorates.Should().HaveCount(1);

        summary.EffectiveElectorates[0].SequenceEqual(new[]
        {
            DomainOfInfluenceType.Ch,
            DomainOfInfluenceType.Ct,
            DomainOfInfluenceType.Bz,
        }).Should().BeTrue();
    }

    [Fact]
    public void MissingBasisElectoratesShouldWork()
    {
        var summary = Build(
            new()
            {
                ContestElectorates = new List<ContestCountingCircleElectorate>
                {
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct } },
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.An } },
                },
            },
            new[] { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct });
        summary.ContestElectorates.Should().HaveCount(2);
        summary.ContestElectorates[0].SequenceEqual(new[] { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct }).Should().BeTrue();
        summary.ContestElectorates[1].SequenceEqual(new[] { DomainOfInfluenceType.An }).Should().BeTrue();
        summary.EffectiveElectorates.Should().HaveCount(1);
        summary.EffectiveElectorates[0].SequenceEqual(new[] { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct }).Should().BeTrue();
    }

    [Fact]
    public void MissingContestElectoratesShouldWork()
    {
        var summary = Build(
            new()
            {
                Electorates = new List<CountingCircleElectorate>
                {
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct } },
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Bz } },
                },
            },
            new[] { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct, DomainOfInfluenceType.Bz });
        summary.ContestElectorates.Should().BeEmpty();
        summary.EffectiveElectorates.Should().HaveCount(2);
        summary.EffectiveElectorates[0].SequenceEqual(new[] { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct }).Should().BeTrue();
        summary.EffectiveElectorates[1].SequenceEqual(new[] { DomainOfInfluenceType.Bz }).Should().BeTrue();
    }

    [Fact]
    public void WithBasisAndContestElectoratesShouldWork()
    {
        var summary = Build(
            new()
            {
                Electorates = new List<CountingCircleElectorate>
                {
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct } },
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Bz } },
                },
                ContestElectorates = new List<ContestCountingCircleElectorate>
                {
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Ki } },
                },
            },
            new[] { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct, DomainOfInfluenceType.Bz, DomainOfInfluenceType.Ki });
        summary.ContestElectorates.Should().HaveCount(1);
        summary.ContestElectorates[0].SequenceEqual(new[] { DomainOfInfluenceType.Ki }).Should().BeTrue();
        summary.EffectiveElectorates.Should().HaveCount(2);

        // contest electorate + one electorate with all required unused doi types
        summary.EffectiveElectorates[0].SequenceEqual(new[] { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct, DomainOfInfluenceType.Bz }).Should().BeTrue();
        summary.EffectiveElectorates[1].SequenceEqual(new[] { DomainOfInfluenceType.Ki }).Should().BeTrue();
    }

    [Fact]
    public void EffectiveElectoratesShouldOnlyContainRequiredDoiTypes()
    {
        var summary = Build(
            new()
            {
                Electorates = new List<CountingCircleElectorate>
                {
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct, DomainOfInfluenceType.Bz } },
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.An } },
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Sc } },
                },
            },
            new[] { DomainOfInfluenceType.Ct, DomainOfInfluenceType.Bz, DomainOfInfluenceType.An, DomainOfInfluenceType.Ki });
        summary.EffectiveElectorates.Should().HaveCount(3);
        summary.EffectiveElectorates[0].SequenceEqual(new[] { DomainOfInfluenceType.Ct, DomainOfInfluenceType.Bz }).Should().BeTrue();
        summary.EffectiveElectorates[1].SequenceEqual(new[] { DomainOfInfluenceType.Ki }).Should().BeTrue();
        summary.EffectiveElectorates[2].SequenceEqual(new[] { DomainOfInfluenceType.An }).Should().BeTrue();
    }

    [Fact]
    public void VotingCardsWithDifferentRequiredCountsButSameChannelValidStateAndElectorateShouldHaveSeparatedElectorates()
    {
        var summary = Build(
            new()
            {
                Electorates = new List<CountingCircleElectorate>
                {
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct } },
                    new() { DomainOfInfluenceTypes = new() { DomainOfInfluenceType.Bz, DomainOfInfluenceType.Sk } },
                },
            },
            new[] { DomainOfInfluenceType.Ch, DomainOfInfluenceType.Ct, DomainOfInfluenceType.Bz, DomainOfInfluenceType.Sk },
            new()
            {
                VotingCards = new List<VotingCardResultDetail>
                {
                    new()
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 10,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new()
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 9,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new()
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 5,
                        DomainOfInfluenceType = DomainOfInfluenceType.Bz,
                    },
                    new()
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 5,
                        DomainOfInfluenceType = DomainOfInfluenceType.Sk,
                    },
                    new()
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 1,
                        DomainOfInfluenceType = DomainOfInfluenceType.Bz,
                    },
                    new()
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 1,
                        DomainOfInfluenceType = DomainOfInfluenceType.Sk,
                    },
                },
            });
        summary.EffectiveElectorates.Should().HaveCount(3);
        summary.EffectiveElectorates[0].SequenceEqual(new[] { DomainOfInfluenceType.Ch }).Should().BeTrue();
        summary.EffectiveElectorates[1].SequenceEqual(new[] { DomainOfInfluenceType.Ct }).Should().BeTrue();
        summary.EffectiveElectorates[2].SequenceEqual(new[] { DomainOfInfluenceType.Bz, DomainOfInfluenceType.Sk }).Should().BeTrue();
    }

    private (List<List<DomainOfInfluenceType>> EffectiveElectorates, List<List<DomainOfInfluenceType>> ContestElectorates) Build(
        CountingCircle cc,
        IReadOnlyCollection<DomainOfInfluenceType> requiredVotingCards,
        ContestCountingCircleDetails? ccDetails = null)
    {
        var summary = ContestCountingCircleElectorateSummaryBuilder.Build(cc, ccDetails ?? new(), requiredVotingCards.ToHashSet());

        return (
            summary.EffectiveElectorates.Select(e => e.DomainOfInfluenceTypes).ToList(),
            summary.ContestCountingCircleElectorates.Select(e => e.DomainOfInfluenceTypes).ToList());
    }
}
