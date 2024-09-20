// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.DomainOfInfluenceTests;

public class DomainOfInfluenceDeleteTest : DomainOfInfluenceProcessorBaseTest
{
    public DomainOfInfluenceDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    [Fact]
    public async Task TestDeleted()
    {
        await DomainOfInfluenceMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(new DomainOfInfluenceDeleted
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });

        var data = await GetData();
        data.MatchSnapshot();
    }

    [Fact]
    public async Task TestDeleteShouldDeleteSnapshotsForContestsInTestingPhase()
    {
        await ContestMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(new DomainOfInfluenceDeleted
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
        });

        var domainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen);
        var countOfDomainOfInfluences = await RunOnDb(db => db.DomainOfInfluences
            .WhereContestIsInTestingPhase()
            .CountAsync(cc => cc.BasisDomainOfInfluenceId == domainOfInfluenceId));

        countOfDomainOfInfluences.Should().Be(0);
    }
}
