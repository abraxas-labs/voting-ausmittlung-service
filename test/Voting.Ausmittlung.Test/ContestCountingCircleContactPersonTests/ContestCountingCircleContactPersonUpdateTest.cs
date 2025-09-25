// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using ContactPersonEventData = Abraxas.Voting.Ausmittlung.Events.V1.Data.ContactPersonEventData;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Test.ContestCountingCircleContactPersonTests;

public class ContestCountingCircleContactPersonUpdateTest : ContestCountingCircleContactPersonBaseTest
{
    private Guid _contactPersonId;

    public ContestCountingCircleContactPersonUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);

        await RunScoped<IServiceProvider>(async sp =>
        {
            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", "test", "test", Enumerable.Empty<string>());

            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();

            var aggregate = aggregateFactory.New<ContestCountingCircleContactPersonAggregate>();
            aggregate.Create(
                Guid.Parse(ContestMockedData.IdBundesurnengang),
                CountingCircleMockedData.GuidGossau,
                new DomainModels.ContactPerson
                {
                    FirstName = "first name",
                    Phone = "0783214567",
                },
                true,
                null);
            await aggregateRepository.Save(aggregate);
            _contactPersonId = aggregate.Id;
        });

        await RunOnDb(async db =>
        {
            var countingCircle = await db
                .CountingCircles
                .Where(cc => cc.SnapshotContestId == Guid.Parse(ContestMockedData.IdBundesurnengang)
                             && cc.BasisCountingCircleId == CountingCircleMockedData.GuidGossau)
                .AsTracking()
                .SingleAsync();

            countingCircle.MustUpdateContactPersons = true;
            countingCircle.ContestCountingCircleContactPersonId = _contactPersonId;
            await db.SaveChangesAsync();
        });
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
            x => x.ContestCountingCircleContactPersonId!,
            x => x.ResponsibleAuthority.Id,
            x => x.ResponsibleAuthority.CountingCircleId,
            x => x.ContactPersonDuringEvent.Id,
            x => x.ContactPersonDuringEvent.CountingCircleDuringEventId!,
            x => x.ContactPersonAfterEvent!.Id,
            x => x.ContactPersonAfterEvent!.CountingCircleAfterEventId!);
    }

    [Fact]
    public async Task TestShouldWorkWithBasisEventInBetween()
    {
        // Contact person has been created in Ausmittlung
        // Now, the counting circle is updated in Basis
        await TestEventPublisher.Publish(
            new CountingCircleUpdated
            {
                CountingCircle = new CountingCircleEventData
                {
                    Id = CountingCircleMockedData.IdGossau,
                    NameForProtocol = "Stadt Gossau",
                    Name = "Gossau",
                    Bfs = "3443",
                    Code = "3443-GOSSAU",
                    SortNumber = 9800,
                    ResponsibleAuthority = new AuthorityEventData
                    {
                        SecureConnectId = SecureConnectTestDefaults.MockedTenantGossau.Id,
                    },
                    ContactPersonDuringEvent = new Abraxas.Voting.Basis.Events.V1.Data.ContactPersonEventData
                    {
                        FirstName = "updated first name",
                        FamilyName = "updated family name",
                        Email = "update@test.invalid",
                        Phone = "test phone",
                    },
                    ContactPersonSameDuringEventAsAfter = true,
                    EVoting = true,
                },
            });

        // Updating the contact person again should work
        await TestEventPublisher.Publish(1, NewValidEvent());

        var countingCircle = await RunOnDb(db => db
            .CountingCircles
            .Where(cc => cc.SnapshotContestId == Guid.Parse(ContestMockedData.IdBundesurnengang)
                         && cc.BasisCountingCircleId == CountingCircleMockedData.GuidGossau)
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ResponsibleAuthority)
            .SingleAsync());
        countingCircle.ContestCountingCircleContactPersonId.Should().NotBeNull();
        countingCircle.MustUpdateContactPersons.Should().BeFalse();

        countingCircle.MatchSnapshot(
            x => x.Id,
            x => x.ContestCountingCircleContactPersonId!,
            x => x.ResponsibleAuthority.Id,
            x => x.ResponsibleAuthority.CountingCircleId,
            x => x.ContactPersonDuringEvent.Id,
            x => x.ContactPersonDuringEvent.CountingCircleDuringEventId!,
            x => x.ContactPersonAfterEvent!.Id,
            x => x.ContactPersonAfterEvent!.CountingCircleAfterEventId!);
    }

    [Fact]
    public async Task UpdateContactPersonShouldBeOk()
    {
        await ErfassungElectionAdminClient.UpdateAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleContactPersonUpdated>();
        eventData.MatchSnapshot(x => x.ContestCountingCircleContactPersonId!);
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await ErfassungElectionAdminClient.UpdateAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestCountingCircleContactPersonUpdated>();
        });
    }

    [Fact]
    public async Task UpdateContactPersonShouldBeOkAsContestManagerDuringTestingPhase()
    {
        await BundErfassungElectionAdminClient.UpdateAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleContactPersonUpdated>();
        eventData.MatchSnapshot(x => x.ContestCountingCircleContactPersonId!);
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundErfassungElectionAdminClient.UpdateAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task UpdateContactPersonNotSameButAfterNotProvidedShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateAsync(
                NewValidRequest(x => x.ContactPersonAfterEvent = null)),
            StatusCode.InvalidArgument,
            "ContactPersonAfterEvent cannot be null if ContactPersonSameDuringEventAsAfter is false");
    }

    [Fact]
    public async Task UpdateContactPersonNotFoundShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateAsync(NewValidRequest(x => x.Id = "5187fa1c-f9e1-4e78-b2a5-d843094760df")),
            StatusCode.NotFound,
            "Aggregate 5187fa1c-f9e1-4e78-b2a5-d843094760df not found");
    }

    [Fact]
    public async Task UpdateContactPersonInLockedContestShouldThrow()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestCountingCircleContactPersonService.ContestCountingCircleContactPersonServiceClient(channel)
            .UpdateAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private UpdateContestCountingCircleContactPersonRequest NewValidRequest(
        Action<UpdateContestCountingCircleContactPersonRequest>? customizer = null)
    {
        var request = new UpdateContestCountingCircleContactPersonRequest
        {
            Id = _contactPersonId.ToString(),
            ContactPersonDuringEvent = new EnterContactPersonRequest
            {
                FirstName = "updated first name",
                FamilyName = "updated family name",
                Email = "test-updated@example.com",
                Phone = "+41795212222",
                MobilePhone = "+41795212221",
            },
            ContactPersonAfterEvent = new EnterContactPersonRequest
            {
                FirstName = "after updated first name",
                FamilyName = "after updated family name",
                Email = "after-test-updated@example.com",
                Phone = "+41795212999",
                MobilePhone = "+41795212998",
            },
        };

        customizer?.Invoke(request);
        return request;
    }

    private ContestCountingCircleContactPersonUpdated NewValidEvent()
    {
        return new ContestCountingCircleContactPersonUpdated
        {
            ContestCountingCircleContactPersonId = _contactPersonId.ToString(),
            ContactPersonDuringEvent = new ContactPersonEventData
            {
                FirstName = "updated first name",
                FamilyName = "updated family name",
                Email = "update@example.com",
                Phone = "+41791112233",
            },
            ContactPersonSameDuringEventAsAfter = false,
            ContactPersonAfterEvent = new ContactPersonEventData
            {
                FirstName = "updated first name (after)",
                FamilyName = "updated family name (after)",
                Email = "update-after@example.com",
                Phone = "+41791118899",
            },
            EventInfo = GetMockedEventInfo(),
        };
    }
}
