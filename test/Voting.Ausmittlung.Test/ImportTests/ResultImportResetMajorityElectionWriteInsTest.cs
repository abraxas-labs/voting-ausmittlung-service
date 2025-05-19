// (c) Copyright by Abraxas Informatik AG
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
using ResultImportType = Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType;

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
        await ResultImportEVotingMockedData.Seed(RunScoped);
        await PermissionMockedData.Seed(RunScoped);

        // activate e voting for all for easier testing
        await ModifyDbEntities((ContestCountingCircleDetails _) => true, details => details.EVoting = true);
        await ModifyDbEntities((CountingCircle _) => true, cc => cc.EVoting = true);
        await ModifyDbEntities((Contest _) => true, contest => contest.EVoting = true);

        await ResultImportMockedData.SeedEVoting(RunScoped, CreateHttpClient);

        await ResultImportECountingMockedData.Seed(RunScoped);
        await ResultImportECountingMockedData.SeedUzwilAggregates(RunScoped);

        // start submission and set result states
        await new ResultService.ResultServiceClient(CreateGrpcChannel(RolesMockedData.ErfassungElectionAdmin))
            .GetListAsync(new GetResultListRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            });

        EventPublisherMock.Clear();
        await ResultImportMockedData.SeedECounting(RunScoped, CreateHttpClient);
    }

    [Fact]
    public async Task ShouldWorkAsElectionAdmin()
    {
        var groups = await FetchMappings();
        var (primaryEvents, secondaryEvents) = await ResetMappings(groups);

        await TestEventPublisher.Publish(primaryEvents.ToArray());
        await TestEventPublisher.Publish(primaryEvents.Count, secondaryEvents.ToArray());

        ResetIds(primaryEvents, secondaryEvents);

        primaryEvents.ShouldMatchChildSnapshot("primary");
        secondaryEvents.ShouldMatchChildSnapshot("secondary");

        var primaryEVotingEvent = primaryEvents.Single(x => x.ImportType == ResultImportType.Evoting);
        var primaryResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryEVotingEvent.MajorityElectionId)));
        primaryResult.CountOfElectionsWithUnmappedEVotingWriteIns.Should().Be(2);
        primaryResult.HasUnmappedWriteIns.Should().BeTrue();
        primaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        primaryResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified)
            .Should()
            .BeTrue();

        var primaryECountingEvent = primaryEvents.Single(x => x.ImportType == ResultImportType.Ecounting);
        var primaryECountingResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryECountingEvent.MajorityElectionId)));
        primaryECountingResult.CountOfElectionsWithUnmappedECountingWriteIns.Should().Be(1);
        primaryECountingResult.HasUnmappedWriteIns.Should().BeTrue();
        primaryECountingResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified)
            .Should()
            .BeTrue();

        var secondaryEVotingEvent = secondaryEvents.Single(x => x.ImportType == ResultImportType.Evoting);
        var secondaryResult = await RunOnDb(db => db.SecondaryMajorityElectionResults
            .Include(x => x.WriteInMappings)
            .SingleAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.SecondaryMajorityElectionId == Guid.Parse(secondaryEVotingEvent.SecondaryMajorityElectionId)));
        secondaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        secondaryResult.WriteInMappings.All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldWorkMultipleTimes()
    {
        var groups = await FetchMappings();
        var (primaryEvents, secondaryEvents) = await ResetMappings(groups);

        var eventCounter = 0;
        await TestEventPublisher.Publish(eventCounter, primaryEvents.ToArray());
        eventCounter += primaryEvents.Count;
        await TestEventPublisher.Publish(eventCounter, secondaryEvents.ToArray());
        eventCounter += secondaryEvents.Count;

        EventPublisherMock.Clear();
        var (primaryEvents2, secondaryEvents2) = await ResetMappings(groups);
        await TestEventPublisher.Publish(eventCounter, primaryEvents2.ToArray());
        eventCounter += primaryEvents2.Count;

        await TestEventPublisher.Publish(eventCounter, secondaryEvents2.ToArray());
        eventCounter += secondaryEvents2.Count;

        var primaryEVotingEvent = primaryEvents.Single(x => x.ImportType == ResultImportType.Evoting);
        var primaryResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryEVotingEvent.MajorityElectionId)));
        primaryResult.CountOfElectionsWithUnmappedEVotingWriteIns.Should().Be(2);
        primaryResult.HasUnmappedWriteIns.Should().BeTrue();
        primaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        primaryResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified)
            .Should()
            .BeTrue();

        var primaryECountingEvent = primaryEvents.Single(x => x.ImportType == ResultImportType.Ecounting);
        var primaryECountingResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryECountingEvent.MajorityElectionId)));
        primaryECountingResult.CountOfElectionsWithUnmappedECountingWriteIns.Should().Be(1);
        primaryECountingResult.HasUnmappedWriteIns.Should().BeTrue();
        primaryECountingResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified)
            .Should()
            .BeTrue();

        var secondaryEVotingEvent = secondaryEvents.Single(x => x.ImportType == ResultImportType.Evoting);
        var secondaryResult = await RunOnDb(db => db.SecondaryMajorityElectionResults
            .Include(x => x.WriteInMappings)
            .SingleAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.SecondaryMajorityElectionId == Guid.Parse(secondaryEVotingEvent.SecondaryMajorityElectionId)));
        secondaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        secondaryResult.WriteInMappings.All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified).Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            var groups = await FetchMappings();

            await ResetMappings(groups);

            return
            [
                .. EventPublisherMock.GetPublishedEventsWithMetadata<MajorityElectionWriteInsReset>(),
                .. EventPublisherMock.GetPublishedEventsWithMetadata<SecondaryMajorityElectionWriteInsReset>()
            ];
        });
    }

    [Fact]
    public async Task ShouldWorkAsContestManagerDuringTestingPhase()
    {
        var groups = await FetchMappings();
        var (primaryEvents, secondaryEvents) = await ResetMappings(groups, StGallenErfassungElectionAdminClient);

        await TestEventPublisher.Publish(primaryEvents.ToArray());
        await TestEventPublisher.Publish(primaryEvents.Count, secondaryEvents.ToArray());

        ResetIds(primaryEvents, secondaryEvents);

        primaryEvents.ShouldMatchChildSnapshot("primary");
        secondaryEvents.ShouldMatchChildSnapshot("secondary");

        var primaryEVotingEvent = primaryEvents.Single(x => x.ImportType == ResultImportType.Evoting);
        var primaryResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryEVotingEvent.MajorityElectionId)));
        primaryResult.CountOfElectionsWithUnmappedEVotingWriteIns.Should().Be(2);
        primaryResult.HasUnmappedWriteIns.Should().BeTrue();
        primaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        primaryResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified)
            .Should()
            .BeTrue();

        var primaryECountingEvent = primaryEvents.Single(x => x.ImportType == ResultImportType.Ecounting);
        var primaryECountingResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryECountingEvent.MajorityElectionId)));
        primaryECountingResult.CountOfElectionsWithUnmappedECountingWriteIns.Should().Be(1);
        primaryECountingResult.HasUnmappedWriteIns.Should().BeTrue();
        primaryECountingResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified)
            .Should()
            .BeTrue();

        var secondaryEVotingEvent = secondaryEvents.Single(x => x.ImportType == ResultImportType.Evoting);
        var secondaryResult = await RunOnDb(db => db.SecondaryMajorityElectionResults
            .Include(x => x.WriteInMappings)
            .SingleAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.SecondaryMajorityElectionId == Guid.Parse(secondaryEVotingEvent.SecondaryMajorityElectionId)));
        secondaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(0);
        secondaryResult.WriteInMappings.All(x => x.Target == MajorityElectionWriteInMappingTarget.Unspecified).Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        var groups = await FetchMappings();
        await AssertStatus(
            async () => await ResetMappings(groups.PrimaryEVotingMappings, StGallenErfassungElectionAdminClient),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldThrowWithNonMajorityPoliticalBusinessType()
    {
        var groups = await FetchMappings();
        groups.PrimaryEVotingMappings.Election.BusinessType = ProtoModels.PoliticalBusinessType.Vote;

        await AssertStatus(
            async () => await ResetMappings(groups.PrimaryEVotingMappings),
            StatusCode.InvalidArgument,
            "Write-Ins are only available for majority elections!");
    }

    [Fact]
    public async Task ShouldThrowWithResultInImmutableState()
    {
        var groups = await FetchMappings();
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
        authStore.SetValues("mock-token", SecureConnectTestDefaults.MockedUserDefault.Loginid, "test", []);

        var aggFactory = GetService<IAggregateFactory>();
        var aggRepo = GetService<IAggregateRepository>();
        var resultAggregate = aggFactory.New<MajorityElectionResultAggregate>();
        resultAggregate.StartSubmission(result.CountingCircle.BasisCountingCircleId, result.MajorityElectionId, contestId, false);
        resultAggregate.SubmissionFinished(contestId);
        await aggRepo.Save(resultAggregate);

        await AssertStatus(
            async () => await ResetMappings(groups.PrimaryEVotingMappings),
            StatusCode.InvalidArgument,
            "WriteIns are only possible if the result is in a mutable state");
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        var groups = await FetchMappings();
        await new ResultImportService.ResultImportServiceClient(channel)
            .ResetMajorityElectionWriteInsAsync(new ResetMajorityElectionWriteInMappingsRequest
            {
                ImportId = groups.PrimaryEVotingMappings.ImportId,
                ElectionId = groups.PrimaryEVotingMappings.Election.Id,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
                PoliticalBusinessType = groups.PrimaryEVotingMappings.Election.BusinessType,
            });
    }

    private async Task<WriteInGroups> FetchMappings()
    {
        var writeIns = await ErfassungElectionAdminClient.GetMajorityElectionWriteInMappingsAsync(
            new GetMajorityElectionWriteInMappingsRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            });

        writeIns.WriteInMappings.Should().HaveCount(4);
        var primaryEVoting = writeIns.WriteInMappings.Single(x =>
            x.ImportType == ResultImportType.Evoting
            && x.Election.BusinessType == ProtoModels.PoliticalBusinessType.MajorityElection);
        var secondaryEVoting = writeIns.WriteInMappings.Single(x =>
            x.ImportType == ResultImportType.Evoting
            && x.Election.BusinessType == ProtoModels.PoliticalBusinessType.SecondaryMajorityElection);
        var primaryECounting = writeIns.WriteInMappings.Single(x =>
            x.ImportType == ResultImportType.Ecounting
            && x.Election.BusinessType == ProtoModels.PoliticalBusinessType.MajorityElection
            && x.Election.Id == MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen);

        primaryEVoting.WriteInMappings.Should().HaveCount(4);
        secondaryEVoting.WriteInMappings.Should().HaveCount(1);
        primaryECounting.WriteInMappings.Should().HaveCount(1);
        return new WriteInGroups(primaryEVoting, primaryECounting, secondaryEVoting);
    }

    private async Task<(List<MajorityElectionWriteInsReset> PrimaryEvent, List<SecondaryMajorityElectionWriteInsReset> SecondaryEvent)> ResetMappings(
        WriteInGroups groups,
        ResultImportService.ResultImportServiceClient? service = null)
    {
        await ResetMappings(groups.PrimaryECountingMappings, service);
        await ResetMappings(groups.PrimaryEVotingMappings, service);
        await ResetMappings(groups.SecondaryEVotingMappings, service);
        return (
            EventPublisherMock.GetPublishedEvents<MajorityElectionWriteInsReset>().ToList(),
            EventPublisherMock.GetPublishedEvents<SecondaryMajorityElectionWriteInsReset>().ToList());
    }

    private async Task ResetMappings(
        ProtoModels.MajorityElectionWriteInMappings mappings,
        ResultImportService.ResultImportServiceClient? service = null)
    {
        await (service ?? ErfassungElectionAdminClient).ResetMajorityElectionWriteInsAsync(new ResetMajorityElectionWriteInMappingsRequest
        {
            ImportId = mappings.ImportId,
            ElectionId = mappings.Election.Id,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
            PoliticalBusinessType = mappings.Election.BusinessType,
        });
    }

    private void ResetIds(
        List<MajorityElectionWriteInsReset> primaryEvents,
        List<SecondaryMajorityElectionWriteInsReset> secondaryEvents)
    {
        foreach (var evnt in primaryEvents)
        {
            evnt.ImportId = string.Empty;
        }

        foreach (var evnt in secondaryEvents)
        {
            evnt.ImportId = string.Empty;
        }
    }

    private record WriteInGroups(
        ProtoModels.MajorityElectionWriteInMappings PrimaryEVotingMappings,
        ProtoModels.MajorityElectionWriteInMappings PrimaryECountingMappings,
        ProtoModels.MajorityElectionWriteInMappings SecondaryEVotingMappings);
}
