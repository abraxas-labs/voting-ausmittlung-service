// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionActiveStateUpdateTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionActiveStateUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Theory]
    [InlineData(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund, false)]
    [InlineData(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot, true)]
    public async Task TestUpdateActiveState(string id, bool onSeparateBallot)
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionActiveStateUpdated
            {
                SecondaryMajorityElectionId = id,
                Active = true,
                IsOnSeparateBallot = onSeparateBallot,
            });

        var result = await GetElection(id, onSeparateBallot);
        result.Active.Should().BeTrue();

        var simpleResult = await RunOnDb(db => db.SimplePoliticalBusinesses
            .FirstAsync(x => x.Id == Guid.Parse(id)));
        simpleResult.Active.Should().BeTrue();

        await TestEventPublisher.Publish(
            1,
            new SecondaryMajorityElectionActiveStateUpdated
            {
                SecondaryMajorityElectionId = id,
                Active = false,
                IsOnSeparateBallot = onSeparateBallot,
            });

        result = await GetElection(id, onSeparateBallot);
        result.Active.Should().BeFalse();

        simpleResult = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(b => b.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(id)),
            Languages.Italian);
        simpleResult.Active.Should().BeFalse();
    }

    private async Task<MajorityElectionBase> GetElection(string id, bool onSeparateBallot)
    {
        if (!onSeparateBallot)
        {
            return await RunOnDb(
                db => db.SecondaryMajorityElections
                    .Include(e => e.Translations)
                    .FirstAsync(x => x.Id == Guid.Parse(id)),
                Languages.German);
        }

        return await RunOnDb(
            db => db.MajorityElections
                .Include(e => e.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(id)),
            Languages.German);
    }
}
