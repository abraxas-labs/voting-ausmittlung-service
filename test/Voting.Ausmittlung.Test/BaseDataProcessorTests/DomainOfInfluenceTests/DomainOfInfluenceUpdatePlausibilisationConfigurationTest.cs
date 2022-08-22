// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.DomainOfInfluenceTests;

public class DomainOfInfluenceUpdatePlausibilisationConfigurationTest : DomainOfInfluenceProcessorBaseTest
{
    public DomainOfInfluenceUpdatePlausibilisationConfigurationTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestUpdated()
    {
        await DomainOfInfluenceMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new DomainOfInfluencePlausibilisationConfigurationUpdated
            {
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                PlausibilisationConfiguration = DomainOfInfluenceMockedData.BuildPlausibilisationConfiguration(x =>
                {
                    x.ComparisonVoterParticipationConfigurations.Add(new ComparisonVoterParticipationConfigurationEventData
                    {
                        MainLevel = SharedProto.DomainOfInfluenceType.Ch,
                        ComparisonLevel = SharedProto.DomainOfInfluenceType.Ch,
                        ThresholdPercent = 3,
                    });
                    x.ComparisonVoterParticipationConfigurations.Add(new ComparisonVoterParticipationConfigurationEventData
                    {
                        MainLevel = SharedProto.DomainOfInfluenceType.Ch,
                        ComparisonLevel = SharedProto.DomainOfInfluenceType.Ct,
                        ThresholdPercent = 7,
                    });

                    x.ComparisonCountOfVotersCountingCircleEntries.Add(new ComparisonCountOfVotersCountingCircleEntryEventData
                    {
                        Category = SharedProto.ComparisonCountOfVotersCategory.B,
                        CountingCircleId = CountingCircleMockedData.IdStGallen,
                    });

                    x.ComparisonVotingChannelConfigurations[0].ThresholdPercent = 99;
                }),
            });

        var data = await GetData(x => x.Id.ToString() == DomainOfInfluenceMockedData.IdStGallen);

        // should delete a comparison voter participation and create 2.
        data[0].PlausibilisationConfiguration!.ComparisonVoterParticipationConfigurations.Should().HaveCount(2);
        data.MatchSnapshot();
    }

    [Fact]
    public async Task TestSnapshotsUpdated()
    {
        await ContestMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new DomainOfInfluencePlausibilisationConfigurationUpdated
            {
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                PlausibilisationConfiguration = DomainOfInfluenceMockedData.BuildPlausibilisationConfiguration(x =>
                {
                    x.ComparisonVoterParticipationConfigurations.Add(new ComparisonVoterParticipationConfigurationEventData
                    {
                        MainLevel = SharedProto.DomainOfInfluenceType.Ch,
                        ComparisonLevel = SharedProto.DomainOfInfluenceType.Ch,
                        ThresholdPercent = 3,
                    });

                    x.ComparisonVoterParticipationConfigurations.Add(new ComparisonVoterParticipationConfigurationEventData
                    {
                        MainLevel = SharedProto.DomainOfInfluenceType.An,
                        ComparisonLevel = SharedProto.DomainOfInfluenceType.An,
                        ThresholdPercent = 150,
                    });

                    x.ComparisonCountOfVotersCountingCircleEntries.Add(new ComparisonCountOfVotersCountingCircleEntryEventData
                    {
                        Category = SharedProto.ComparisonCountOfVotersCategory.C,
                        CountingCircleId = CountingCircleMockedData.IdStGallen,
                    });

                    x.ComparisonVotingChannelConfigurations[0].ThresholdPercent = 99;
                }),
            });

        var data = await GetData(doi => doi.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdStGallen));
        data.SelectMany(doi => doi.CountingCircles).Any(doi => doi.ComparisonCountOfVotersCategory == ComparisonCountOfVotersCategory.C).Should().BeTrue();

        foreach (var domainOfInfluence in data)
        {
            domainOfInfluence.CountingCircles = new List<DomainOfInfluenceCountingCircle>();
        }

        data.MatchSnapshot(
            x => x.Id,
            x => x.ParentId!);
    }
}
