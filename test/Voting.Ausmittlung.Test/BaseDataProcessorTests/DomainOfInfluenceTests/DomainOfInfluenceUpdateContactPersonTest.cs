// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.DomainOfInfluenceTests;

public class DomainOfInfluenceUpdateContactPersonTest : DomainOfInfluenceProcessorBaseTest
{
    public DomainOfInfluenceUpdateContactPersonTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestUpdated()
    {
        await DomainOfInfluenceMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new DomainOfInfluenceContactPersonUpdated
            {
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                ContactPerson = new ContactPersonEventData
                {
                    Email = "hans@muster.com",
                    Phone = "071 123 12 12",
                    FamilyName = "muster-updated",
                    FirstName = "hans",
                    MobilePhone = "079 721 21 21",
                },
            });

        var data = await GetData(x => x.Id.ToString() == DomainOfInfluenceMockedData.IdGossau);
        data.MatchSnapshot();
    }

    [Fact]
    public async Task TestSnapshotsUpdated()
    {
        await ContestMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(
            new DomainOfInfluenceContactPersonUpdated
            {
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                ContactPerson = new ContactPersonEventData
                {
                    Email = "hans@muster.com",
                    Phone = "071 123 12 12",
                    FamilyName = "muster-updated",
                    FirstName = "hans",
                    MobilePhone = "079 721 21 21",
                },
            });

        var data = await GetData(doi => doi.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdStGallen));

        foreach (var domainOfInfluence in data)
        {
            domainOfInfluence.CountingCircles = new List<DomainOfInfluenceCountingCircle>();
        }

        data.MatchSnapshot(
            x => x.Id,
            x => x.ParentId!);
    }
}
