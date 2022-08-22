// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ContestTests;

public class ContestUpdateTest : ContestProcessorBaseTest
{
    public ContestUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestUpdated()
    {
        var ev = NewValidUpdatedEvent();

        await RunOnDb(async db =>
        {
            var contest = await db.Contests.SingleAsync(x => x.Id == Guid.Parse(ev.Contest.Id));

            // should be preserved through updates
            contest.EVotingResultsImported = true;
            db.Update(contest);
            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(ev);

        var data = await GetData(c => c.Id == Guid.Parse(ContestMockedData.IdGossau));
        data[0].EVotingResultsImported.Should().BeTrue();
        SetDynamicIdToDefaultValue(data.SelectMany(x => x.Translations));
        foreach (var contest in data)
        {
            contest.DomainOfInfluenceId = Guid.Empty;
        }

        data.MatchSnapshot();
    }

    [Fact]
    public async Task TestUpdatedHostedFilteredCatchUp()
    {
        var ev = NewValidUpdatedEvent();
        await TestEventPublisher.Publish(true, ev);

        var entry = ContestCache.GetAll().Where(c => c.Id == Guid.Parse(ev.Contest.Id) && c.KeyData == null).Single();
        entry.MatchSnapshot();
    }

    private ContestUpdated NewValidUpdatedEvent()
    {
        return new ContestUpdated
        {
            Contest = new ContestEventData
            {
                Id = ContestMockedData.IdGossau,
                Date = new DateTime(2020, 8, 23, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Description = { LanguageUtil.MockAllLanguages("test-UPDATED") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                EndOfTestingPhase = new DateTime(2019, 01, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                State = SharedProto.ContestState.TestingPhase,
            },
        };
    }
}
