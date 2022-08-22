// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ContestCountingCircleContactPersonTests;

public class ContestCountingCircleContactPersonCreateTest
    : BaseTest<ContestCountingCircleContactPersonService.ContestCountingCircleContactPersonServiceClient>
{
    public ContestCountingCircleContactPersonCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(NewValidEvent());

        var countingCircle = await RunOnDb(db => db
            .CountingCircles
            .Where(cc => cc.SnapshotContestId == Guid.Parse(ContestMockedData.IdBundesurnengang)
                         && cc.BasisCountingCircleId == CountingCircleMockedData.GuidGossau)
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ResponsibleAuthority)
            .SingleOrDefaultAsync());

        countingCircle!.MatchSnapshot(
            x => x.Id,
            x => x.ResponsibleAuthority.Id,
            x => x.ResponsibleAuthority.CountingCircleId,
            x => x.ContactPersonDuringEvent.Id,
            x => x.ContactPersonDuringEvent.CountingCircleDuringEventId!,
            x => x.ContactPersonAfterEvent!.Id,
            x => x.ContactPersonAfterEvent!.CountingCircleAfterEventId!);
    }

    [Fact]
    public async Task CreateContactPersonShouldBeOk()
    {
        var response = await ErfassungElectionAdminClient.CreateAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleContactPersonCreated>();
        response.Id.Should().Be(eventData.ContestCountingCircleContactPersonId);
        eventData.ContestCountingCircleContactPersonId = string.Empty;
        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            var response = await ErfassungElectionAdminClient.CreateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestCountingCircleContactPersonCreated>();
        });
    }

    [Fact]
    public async Task CreateContactPersonNotSameButAfterNotProvidedShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CreateAsync(
                NewValidRequest(x => x.ContactPersonAfterEvent = null)),
            StatusCode.InvalidArgument,
            "ContactPersonAfterEvent cannot be null if ContactPersonSameDuringEventAsAfter is false");
    }

    [Fact]
    public async Task CreateContactPersonDuringInvalidEmailShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CreateAsync(
                NewValidRequest(x => x.ContactPersonDuringEvent.Email = "invalid")),
            StatusCode.InvalidArgument,
            "'Email'");
    }

    [Fact]
    public async Task CreateContactPersonAfterInvalidEmailShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CreateAsync(
                NewValidRequest(x => x.ContactPersonAfterEvent.Email = "invalid")),
            StatusCode.InvalidArgument,
            "'Email'");
    }

    [Fact]
    public async Task CreateContactPersonDuringWithoutPhoneShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CreateAsync(
                NewValidRequest(x => x.ContactPersonDuringEvent.Phone = string.Empty)),
            StatusCode.InvalidArgument,
            "'Phone'");
    }

    [Fact]
    public async Task CreateContactPersonAfterWithoutPhoneShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CreateAsync(
                NewValidRequest(x => x.ContactPersonAfterEvent.Phone = string.Empty)),
            StatusCode.InvalidArgument,
            "'Phone'");
    }

    [Fact]
    public async Task CreateContactPersonInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CreateAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task CreateContactPersonDuplicateShouldThrow()
    {
        await ErfassungElectionAdminClient.CreateAsync(NewValidRequest());
        await RunEvents<ContestCountingCircleContactPersonCreated>();

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CreateAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "A contest counting circle contact person exists already");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestCountingCircleContactPersonService.ContestCountingCircleContactPersonServiceClient(channel)
            .CreateAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private CreateContestCountingCircleContactPersonRequest NewValidRequest(
        Action<CreateContestCountingCircleContactPersonRequest>? customizer = null)
    {
        var request = new CreateContestCountingCircleContactPersonRequest
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            CountingCircleId = CountingCircleMockedData.IdGossau,
            ContactPersonDuringEvent = new EnterContactPersonRequest
            {
                FirstName = "first name",
                FamilyName = "family name",
                Email = "test@example.com",
                Phone = "+41795214623",
            },
            ContactPersonAfterEvent = new EnterContactPersonRequest
            {
                FirstName = "after first name",
                FamilyName = "after family name",
                Email = "after-test@example.com",
                Phone = "+41795212222",
            },
        };

        customizer?.Invoke(request);
        return request;
    }

    private ContestCountingCircleContactPersonCreated NewValidEvent()
    {
        return new ContestCountingCircleContactPersonCreated
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            CountingCircleId = CountingCircleMockedData.IdGossau,
            ContestCountingCircleContactPersonId = "f0cb7299-44e5-4652-9893-8805541995e7",
            ContactPersonDuringEvent = new ContactPersonEventData
            {
                FirstName = "first name",
                FamilyName = "family name",
                Email = "test@example.com",
                Phone = "+41795214623",
            },
            ContactPersonSameDuringEventAsAfter = true,
            EventInfo = GetMockedEventInfo(),
        };
    }
}
