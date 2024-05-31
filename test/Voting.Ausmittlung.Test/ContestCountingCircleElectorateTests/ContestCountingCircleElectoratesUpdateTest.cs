// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ContestCountingCircleElectorateTests;

public class
    ContestCountingCircleElectoratesUpdateTest : BaseTest<
        ContestCountingCircleElectorateService.ContestCountingCircleElectorateServiceClient>
{
    public ContestCountingCircleElectoratesUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await ContestMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
        EventPublisherMock.Clear();
    }

    [Fact]
    public async Task CreateIfNoExistingContestElectoratesShouldWork()
    {
        var client = new ContestCountingCircleElectorateService.ContestCountingCircleElectorateServiceClient(
            CreateGrpcChannel(
                true,
                SecureConnectTestDefaults.MockedTenantStGallen.Id,
                TestDefaults.UserId,
                RolesMockedData.ErfassungElectionAdmin));

        await client.UpdateElectoratesAsync(
            NewValidRequest(x => x.CountingCircleId = CountingCircleMockedData.IdStGallenStFiden));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleElectoratesCreated>();
        eventData.Id
            .Should()
            .Be(AusmittlungUuidV5.BuildCountingCircleSnapshot(Guid.Parse(ContestMockedData.IdBundesurnengang), CountingCircleMockedData.GuidStGallenStFiden).ToString());
        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task UpdateIfExistingContestElectoratesShouldWork()
    {
        await ErfassungElectionAdminClient.UpdateElectoratesAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleElectoratesUpdated>();
        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task ElectorateWithDuplicateDomainofInfluenceTypesShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateElectoratesAsync(NewValidRequest(o =>
                o.Electorates.Add(new CreateUpdateContestCountingCircleElectorateRequest()
                {
                    DomainOfInfluenceTypes =
                    {
                        SharedProto.DomainOfInfluenceType.Og,
                    },
                }))),
            StatusCode.InvalidArgument,
            "A domain of influence type in an electorate must be unique per counting circle");
    }

    [Fact]
    public async Task ElectorateWithNoDomainofInfluenceTypeShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateElectoratesAsync(NewValidRequest(o =>
                o.Electorates.Add(new CreateUpdateContestCountingCircleElectorateRequest()))),
            StatusCode.InvalidArgument,
            "Cannot create an electorate without a domain of influence type");
    }

    [Fact]
    public async Task UpdateElectoratesInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateElectoratesAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestProcessorWithCreated()
    {
        var votingCardCounts = await RunOnDb(db => db.VotingCardResultDetails
                .Where(vc =>
                    vc.ContestCountingCircleDetailsId == AusmittlungUuidV5.BuildContestCountingCircleDetails(
                        Guid.Parse(ContestMockedData.IdBundesurnengang), CountingCircleMockedData.GuidGossau, false))
                .Select(vc => vc.CountOfReceivedVotingCards)
                .ToListAsync());
        votingCardCounts.Should().NotBeEmpty();
        votingCardCounts.Where(x => x != null).Should().NotBeEmpty();

        await TestEventPublisher.Publish(new ContestCountingCircleElectoratesCreated
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            CountingCircleId = CountingCircleMockedData.IdGossau,
            Electorates =
            {
                new ContestCountingCircleElectorateEventData
                {
                    Id = "6f346535-a528-49f6-bda3-a3e8e25a3f78",
                    DomainOfInfluenceTypes = { SharedProto.DomainOfInfluenceType.Ch, SharedProto.DomainOfInfluenceType.Ct },
                },
            },
        });

        var cc = await RunOnDb(db => db.CountingCircles
            .AsSplitQuery()
            .Include(cc => cc.Electorates.OrderBy(e => e.DomainOfInfluenceTypes[0]))
            .Include(cc => cc.ContestElectorates.OrderBy(e => e.DomainOfInfluenceTypes[0]))
            .FirstOrDefaultAsync(cc => cc.BasisCountingCircleId == CountingCircleMockedData.GuidGossau && cc.SnapshotContestId == Guid.Parse(ContestMockedData.IdBundesurnengang)));

        cc.MatchSnapshot();

        // all voting card counts (except e-voting counts) of that contest and counting circle should be null.
        var votingCards = await RunOnDb(db => db.VotingCardResultDetails
            .Where(vc =>
                vc.ContestCountingCircleDetailsId == AusmittlungUuidV5.BuildContestCountingCircleDetails(
                    Guid.Parse(ContestMockedData.IdBundesurnengang), CountingCircleMockedData.GuidGossau, false))
            .ToListAsync());
        votingCards.Where(vc => vc.Channel != VotingChannel.EVoting).Should().NotBeEmpty();
        votingCards.Where(vc => vc.Channel != VotingChannel.EVoting && vc.CountOfReceivedVotingCards != null).Should().BeEmpty();
        votingCards.Where(vc => vc.Channel == VotingChannel.EVoting).Should().NotBeEmpty();
        votingCards.Where(vc => vc.Channel == VotingChannel.EVoting && vc.CountOfReceivedVotingCards == null).Should().BeEmpty();
    }

    [Fact]
    public async Task TestProcessorWithUpdated()
    {
        var ccDetailsId = AusmittlungUuidV5.BuildContestCountingCircleDetails(Guid.Parse(ContestMockedData.IdBundesurnengang), CountingCircleMockedData.GuidStGallenStFiden, false);
        await RunOnDb(async db =>
        {
            db.VotingCardResultDetails.Add(new VotingCardResultDetail
            {
                ContestCountingCircleDetailsId = ccDetailsId,
                CountOfReceivedVotingCards = 5,
                DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                Valid = true,
                Channel = VotingChannel.Paper,
            });
            await db.SaveChangesAsync();
        });
        var votingCardCounts = await RunOnDb(db => db.VotingCardResultDetails
            .Where(vc =>
                vc.ContestCountingCircleDetailsId == ccDetailsId)
            .Select(vc => vc.CountOfReceivedVotingCards)
            .ToListAsync());
        votingCardCounts.Should().NotBeEmpty();
        votingCardCounts.Where(x => x != null).Should().NotBeEmpty();

        await TestEventPublisher.Publish(new ContestCountingCircleElectoratesUpdated
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            CountingCircleId = CountingCircleMockedData.IdStGallenStFiden,
            Electorates =
            {
                new ContestCountingCircleElectorateEventData
                {
                    Id = "6f346535-a528-49f6-bda3-a3e8e25a3f78",
                    DomainOfInfluenceTypes = { SharedProto.DomainOfInfluenceType.Ch },
                },
                new ContestCountingCircleElectorateEventData
                {
                    Id = "6389bc00-c1d5-44b6-81f4-b4d01d0a3cd1",
                    DomainOfInfluenceTypes = { SharedProto.DomainOfInfluenceType.Ct, SharedProto.DomainOfInfluenceType.An },
                },
            },
        });

        var cc = await RunOnDb(db => db.CountingCircles
            .AsSplitQuery()
            .Include(cc => cc.Electorates.OrderBy(e => e.DomainOfInfluenceTypes[0]))
            .Include(cc => cc.ContestElectorates.OrderBy(e => e.DomainOfInfluenceTypes[0]))
            .FirstOrDefaultAsync(cc => cc.BasisCountingCircleId == CountingCircleMockedData.GuidStGallenStFiden && cc.SnapshotContestId == Guid.Parse(ContestMockedData.IdBundesurnengang)));

        cc.MatchSnapshot();

        // all voting card counts of that contest and counting circle should be null.
        votingCardCounts = await RunOnDb(db => db.VotingCardResultDetails
            .Where(vc => vc.ContestCountingCircleDetailsId == ccDetailsId)
            .Select(vc => vc.CountOfReceivedVotingCards)
            .ToListAsync());
        votingCardCounts.Should().NotBeEmpty();
        votingCardCounts.Where(x => x != null).Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateAndUpdateWithSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            var client = new ContestCountingCircleElectorateService.ContestCountingCircleElectorateServiceClient(
                CreateGrpcChannel(
                    true,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    TestDefaults.UserId,
                    RolesMockedData.ErfassungElectionAdmin));

            await client.UpdateElectoratesAsync(
                NewValidRequest(x => x.CountingCircleId = CountingCircleMockedData.IdStGallenStFiden));
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestCountingCircleElectoratesCreated>();
        });

        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await ErfassungElectionAdminClient.UpdateElectoratesAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestCountingCircleElectoratesUpdated>();
        });
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestCountingCircleElectorateService.ContestCountingCircleElectorateServiceClient(channel)
            .UpdateElectoratesAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private UpdateContestCountingCircleElectoratesRequest NewValidRequest(
        Action<UpdateContestCountingCircleElectoratesRequest>? customizer = null)
    {
        var request = new UpdateContestCountingCircleElectoratesRequest
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            CountingCircleId = CountingCircleMockedData.IdGossau,
            Electorates =
            {
                new CreateUpdateContestCountingCircleElectorateRequest
                {
                    DomainOfInfluenceTypes = { SharedProto.DomainOfInfluenceType.Og },
                },
                new CreateUpdateContestCountingCircleElectorateRequest
                {
                    DomainOfInfluenceTypes = { SharedProto.DomainOfInfluenceType.Ct },
                },
            },
        };

        customizer?.Invoke(request);
        return request;
    }
}
