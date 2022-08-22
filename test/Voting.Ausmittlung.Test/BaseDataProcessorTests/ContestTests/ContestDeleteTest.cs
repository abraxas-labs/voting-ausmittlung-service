// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ContestTests;

public class ContestDeleteTest : ContestProcessorBaseTest
{
    public ContestDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestDeleted()
    {
        var id = ContestMockedData.IdGossau;
        var idGuid = Guid.Parse(id);
        await TestEventPublisher.Publish(new ContestDeleted { ContestId = id });

        var count = await RunOnDb(db => db.Contests.CountAsync(c => c.Id == idGuid));
        count.Should().Be(0);
    }

    [Fact]
    public async Task TestDeletedHostedFilteredCatchUp()
    {
        var id = ContestMockedData.IdGossau;
        await TestEventPublisher.Publish(false, new ContestDeleted { ContestId = id });

        ContestCache.GetAll().Where(c => c.Id == Guid.Parse(id)).Should().BeEmpty();
        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyDeleted>().Any().Should().BeFalse();
    }
}
