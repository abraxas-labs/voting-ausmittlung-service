// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.DomainOfInfluenceTests;

public class DomainOfInfluenceUpdateCountingCirclesTest : BaseDataProcessorTest
{
    public DomainOfInfluenceUpdateCountingCirclesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    [Fact]
    public async Task TestDomainOfInfluenceCountingCirclesUpdated()
    {
        await DomainOfInfluenceMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(new DomainOfInfluenceCountingCircleEntriesUpdated()
        {
            DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
            {
                Id = DomainOfInfluenceMockedData.IdBund,
                CountingCircleIds =
                    {
                        CountingCircleMockedData.IdUzwilKirche,
                    },
            },
        });

        var data = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .OrderBy(x => x.CountingCircleId)
            .Where(x => x.DomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdBund))
            .Include(x => x.CountingCircle)
            .ToListAsync());
        data.MatchSnapshot(x => x.Id);
    }

    [Fact]
    public async Task TestSnapshotsUpdated()
    {
        await ContestMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(new DomainOfInfluenceCountingCircleEntriesUpdated()
        {
            DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
            {
                Id = DomainOfInfluenceMockedData.IdStGallen,
                CountingCircleIds =
                    {
                        CountingCircleMockedData.IdStGallen,
                        CountingCircleMockedData.IdUzwilKirche,
                    },
            },
        });

        var data = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .Include(x => x.CountingCircle)
            .Include(x => x.DomainOfInfluence)
            .Where(x => x.DomainOfInfluence.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdStGallen))
            .OrderBy(x => x.CountingCircle.SnapshotContestId)
            .ThenBy(x => x.CountingCircle.BasisCountingCircleId)
            .ToListAsync());

        data.MatchSnapshot(
            x => x.Id,
            x => x.CountingCircleId,
            x => x.CountingCircle.Id,
            x => x.CountingCircle.ResponsibleAuthority,
            x => x.CountingCircle.ContactPersonAfterEvent!,
            x => x.CountingCircle.ContactPersonDuringEvent,
            x => x.DomainOfInfluenceId,
            x => x.DomainOfInfluence.Id,
            x => x.DomainOfInfluence.ParentId!);
    }

    [Fact]
    public async Task TestProcessorAddCountingCircle()
    {
        await DomainOfInfluenceMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(new DomainOfInfluenceCountingCircleEntriesUpdated
        {
            DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
            {
                Id = DomainOfInfluenceMockedData.IdStGallen,
                CountingCircleIds =
                    {
                        CountingCircleMockedData.IdStGallen,
                        CountingCircleMockedData.IdUzwilKirche,
                    },
            },
        });

        var parentDoiCcs = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .Where(doiCc => doiCc.DomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdBund))
            .ToListAsync());

        var selfDoiCcs = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .Where(doiCc => doiCc.DomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdStGallen))
            .ToListAsync());

        var childDoiUzwilKircheCc = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .Where(doiCc => doiCc.DomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdGossau)
                            && doiCc.CountingCircleId == CountingCircleMockedData.GuidUzwilKirche)
            .FirstOrDefaultAsync());

        var parentDoiUzwilKircheCc = parentDoiCcs.Find(doiCc => doiCc.CountingCircleId == CountingCircleMockedData.GuidUzwilKirche);
        var selfDoiUzwilKircheCc = selfDoiCcs.Find(doiCc => doiCc.CountingCircleId == CountingCircleMockedData.GuidUzwilKirche);

        parentDoiCcs.Should().HaveCount(7);
        selfDoiCcs.Should().HaveCount(6);

        parentDoiUzwilKircheCc.Should().NotBeNull();
        parentDoiUzwilKircheCc!.Inherited.Should().BeTrue();

        selfDoiUzwilKircheCc.Should().NotBeNull();
        selfDoiUzwilKircheCc!.Inherited.Should().BeFalse();

        childDoiUzwilKircheCc.Should().BeNull();
    }

    [Fact]
    public async Task TestProcessorRemoveCountingCircle()
    {
        await DomainOfInfluenceMockedData.Seed(RunScoped);

        await TestEventPublisher.Publish(new DomainOfInfluenceCountingCircleEntriesUpdated
        {
            DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
            {
                Id = DomainOfInfluenceMockedData.IdStGallen,
            },
        });

        var parentDoiCcs = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .Where(doiCc => doiCc.DomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdBund))
            .ToListAsync());

        var selfDoiCcs = await RunOnDb(db => db.DomainOfInfluenceCountingCircles
            .Where(doiCc => doiCc.DomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdStGallen))
            .ToListAsync());

        var parentDoiStGallenCc = parentDoiCcs.FirstOrDefault(doiCc => doiCc.CountingCircleId == CountingCircleMockedData.GuidStGallen);
        var selfDoiStGallenCc = selfDoiCcs.FirstOrDefault(doiCc => doiCc.CountingCircleId == CountingCircleMockedData.GuidStGallen);

        parentDoiCcs.Should().HaveCount(5);
        selfDoiCcs.Should().HaveCount(4);

        parentDoiStGallenCc.Should().BeNull();
        selfDoiStGallenCc.Should().BeNull();
    }

    [Fact]
    public async Task TestProcessorEVotingCountingCircleUpdatesContestDetails()
    {
        await ContestMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var countingCircle = await db.CountingCircles.AsTracking().SingleAsync(x => x.Id == AusmittlungUuidV5.BuildCountingCircleSnapshot(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidUzwilKirche));
            countingCircle.EVoting = true;
            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(new DomainOfInfluenceCountingCircleEntriesUpdated
        {
            DomainOfInfluenceCountingCircleEntries = new DomainOfInfluenceCountingCircleEntriesEventData
            {
                Id = DomainOfInfluenceMockedData.IdStGallen,
                CountingCircleIds =
                {
                    CountingCircleMockedData.IdUzwilKirche,
                },
            },
        });

        var ccDetailsId = AusmittlungUuidV5.BuildContestCountingCircleDetails(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidUzwilKirche, false);
        var contestDetail = await RunOnDb(db => db.ContestCountingCircleDetails
            .SingleAsync(x => x.Id == ccDetailsId));

        contestDetail.EVoting.Should().BeTrue();
    }
}
