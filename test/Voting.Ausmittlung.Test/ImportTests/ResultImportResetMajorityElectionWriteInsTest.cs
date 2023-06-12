// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultImportResetMajorityElectionWriteInsTest : BaseTest<ResultImportService.ResultImportServiceClient>
{
    public ResultImportResetMajorityElectionWriteInsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ResultImportMockedData.Seed(RunScoped);
        await PermissionMockedData.Seed(RunScoped);

        // activate e voting for all for easier testing
        await ModifyDbEntities((ContestCountingCircleDetails _) => true, details => details.EVoting = true);
        await ModifyDbEntities((Contest _) => true, contest => contest.EVoting = true);

        await EVotingMockedData.Seed(RunScoped, CreateHttpClient);
    }

    [Fact]
    public async Task ShouldWorkAsElectionAdmin()
    {
        var (_, primaryMappings, secondaryMappings) = await FetchMappings();
        var (primaryEvent, secondaryEvent) = await ResetMappings(primaryMappings, secondaryMappings);

        await TestEventPublisher.Publish(primaryEvent);
        await TestEventPublisher.Publish(1, secondaryEvent);

        primaryEvent.ShouldMatchChildSnapshot("primary");
        secondaryEvent.ShouldMatchChildSnapshot("secondary");

        var primaryResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryEvent.MajorityElectionId)));
        primaryResult.CountOfElectionsWithUnmappedWriteIns.Should().Be(2);
        primaryResult.HasUnmappedWriteIns.Should().BeTrue();
        primaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        primaryResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified)
            .Should()
            .BeTrue();

        var secondaryResult = await RunOnDb(db => db.SecondaryMajorityElectionResults
            .Include(x => x.WriteInMappings)
            .SingleAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.SecondaryMajorityElectionId == Guid.Parse(secondaryEvent.SecondaryMajorityElectionId)));
        secondaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        secondaryResult.WriteInMappings.All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldWorkMultipleTimes()
    {
        var (_, primaryMappings, secondaryMappings) = await FetchMappings();
        var (primaryEvent, secondaryEvent) = await ResetMappings(primaryMappings, secondaryMappings);

        await TestEventPublisher.Publish(primaryEvent);
        await TestEventPublisher.Publish(1, secondaryEvent);

        EventPublisherMock.Clear();
        var (primaryEvent2, secondaryEvent2) = await ResetMappings(primaryMappings, secondaryMappings);
        await TestEventPublisher.Publish(2, primaryEvent2);
        await TestEventPublisher.Publish(3, secondaryEvent2);

        var primaryResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryEvent.MajorityElectionId)));
        primaryResult.CountOfElectionsWithUnmappedWriteIns.Should().Be(2);
        primaryResult.HasUnmappedWriteIns.Should().BeTrue();
        primaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        primaryResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified)
            .Should()
            .BeTrue();

        var secondaryResult = await RunOnDb(db => db.SecondaryMajorityElectionResults
            .Include(x => x.WriteInMappings)
            .SingleAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.SecondaryMajorityElectionId == Guid.Parse(secondaryEvent.SecondaryMajorityElectionId)));
        secondaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        secondaryResult.WriteInMappings.All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified).Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            var (_, primaryMappings, secondaryMappings) = await FetchMappings();

            await ResetMappings(primaryMappings, secondaryMappings);

            return new[]
            {
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionWriteInsReset>(),
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionWriteInsReset>(),
            };
        });
    }

    [Fact]
    public async Task ShouldWorkAsContestManagerDuringTestingPhase()
    {
        var (_, primaryMappings, secondaryMappings) = await FetchMappings();
        var (primaryEvent, secondaryEvent) = await ResetMappings(
            primaryMappings,
            secondaryMappings,
            StGallenErfassungElectionAdminClient);

        await TestEventPublisher.Publish(primaryEvent);
        await TestEventPublisher.Publish(1, secondaryEvent);

        primaryEvent.ShouldMatchChildSnapshot("primary");
        secondaryEvent.ShouldMatchChildSnapshot("secondary");

        var primaryResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryEvent.MajorityElectionId)));
        primaryResult.CountOfElectionsWithUnmappedWriteIns.Should().Be(2);
        primaryResult.HasUnmappedWriteIns.Should().BeTrue();
        primaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        primaryResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified)
            .Should()
            .BeTrue();

        var secondaryResult = await RunOnDb(db => db.SecondaryMajorityElectionResults
            .Include(x => x.WriteInMappings)
            .SingleAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.SecondaryMajorityElectionId == Guid.Parse(secondaryEvent.SecondaryMajorityElectionId)));
        secondaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        secondaryResult.WriteInMappings.All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified).Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        var (_, primaryMappings, _) = await FetchMappings();
        await AssertStatus(
            async () => await ResetMappings(primaryMappings, StGallenErfassungElectionAdminClient),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldThrowWithNonMajorityPoliticalBusinessType()
    {
        var (_, primaryMappings, _) = await FetchMappings();
        primaryMappings.Election.BusinessType = ProtoModels.PoliticalBusinessType.Vote;

        await AssertStatus(
            async () => await ResetMappings(primaryMappings),
            StatusCode.InvalidArgument,
            "Write-Ins are only available for majority elections!");
    }

    [Fact]
    public async Task ShouldThrowWithResultInImmutableState()
    {
        var (importId, primaryMappings, _) = await FetchMappings();
        var result = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.CountingCircle)
            .Include(x => x.MajorityElection)
            .Where(x =>
                x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil
                && x.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen))
            .SingleAsync());
        var contestId = result.MajorityElection.ContestId;

        // needed to create aggregates, since they access user/tenant information
        var authStore = GetService<IAuthStore>();
        authStore.SetValues("mock-token", SecureConnectTestDefaults.MockedUserDefault.Loginid, "test", Enumerable.Empty<string>());

        var aggFactory = GetService<IAggregateFactory>();
        var aggRepo = GetService<IAggregateRepository>();
        var resultAggregate = aggFactory.New<MajorityElectionResultAggregate>();
        resultAggregate.StartSubmission(result.CountingCircle.BasisCountingCircleId, result.MajorityElectionId, contestId, false);
        resultAggregate.SubmissionFinished(contestId);
        await aggRepo.Save(resultAggregate);

        await AssertStatus(
            async () => await ResetMappings(primaryMappings),
            StatusCode.InvalidArgument,
            "WriteIns are only possible if the result is in a mutable state");
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultImportService.ResultImportServiceClient(channel)
            .ResetMajorityElectionWriteInsAsync(new ResetMajorityElectionWriteInMappingsRequest
            {
                ElectionId = "eebc9095-8ba3-4dbb-b2ae-99e0a5e1b965",
                ContestId = "5649dc51-9558-4aef-9c1b-41f37868809e",
                CountingCircleId = "ae636acd-6467-42af-9e41-6c8e79cde95d",
                PoliticalBusinessType = ProtoModels.PoliticalBusinessType.MajorityElection,
            });
    }

    private async Task<(string ImportId, ProtoModels.MajorityElectionWriteInMappings PrimaryMappings, ProtoModels.MajorityElectionWriteInMappings
            SecondaryMappings)>
        FetchMappings()
    {
        var writeIns = await ErfassungElectionAdminClient.GetMajorityElectionWriteInMappingsAsync(
            new GetMajorityElectionWriteInMappingsRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            });

        writeIns.ElectionWriteInMappings.Should().HaveCount(2);
        var primaryMappings = writeIns.ElectionWriteInMappings[0];
        var secondaryMappings = writeIns.ElectionWriteInMappings[1];

        primaryMappings.Election.BusinessType.Should().Be(ProtoModels.PoliticalBusinessType.MajorityElection);
        primaryMappings.WriteInMappings.Should().HaveCount(4);

        secondaryMappings.Election.BusinessType.Should().Be(ProtoModels.PoliticalBusinessType.SecondaryMajorityElection);
        secondaryMappings.WriteInMappings.Should().HaveCount(1);

        return (writeIns.ImportId, primaryMappings, secondaryMappings);
    }

    private async Task<(MajorityElectionWriteInsReset PrimaryEvent, SecondaryMajorityElectionWriteInsReset SecondaryEvent)> ResetMappings(
        ProtoModels.MajorityElectionWriteInMappings primaryMappings,
        ProtoModels.MajorityElectionWriteInMappings secondaryMappings,
        ResultImportService.ResultImportServiceClient? service = null)
    {
        await ResetMappings(primaryMappings, service);
        await ResetMappings(secondaryMappings, service);
        return (
            EventPublisherMock.GetSinglePublishedEvent<MajorityElectionWriteInsReset>(),
            EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionWriteInsReset>());
    }

    private async Task ResetMappings(
        ProtoModels.MajorityElectionWriteInMappings mappings,
        ResultImportService.ResultImportServiceClient? service = null)
    {
        await (service ?? ErfassungElectionAdminClient).ResetMajorityElectionWriteInsAsync(new ResetMajorityElectionWriteInMappingsRequest
        {
            ElectionId = mappings.Election.Id,
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
            PoliticalBusinessType = mappings.Election.BusinessType,
        });
    }
}
