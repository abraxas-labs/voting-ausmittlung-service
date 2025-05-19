// (c) Copyright by Abraxas Informatik AG
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
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultImportMapMajorityElectionWriteInsTest : BaseTest<ResultImportService.ResultImportServiceClient>
{
    public ResultImportMapMajorityElectionWriteInsTest(TestApplicationFactory factory)
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
        var (primaryEvents, secondaryEvents) = await MapMappings(groups, (mapping, writeIn) =>
        {
            switch (mapping.WriteInCandidateName)
            {
                case "Hans Muster":
                    writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Empty;
                    break;
                case "Hans Mueller":
                    writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Candidate;
                    writeIn.CandidateId = MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen;
                    break;
            }
        });

        await TestEventPublisher.Publish(primaryEvents.ToArray());
        await TestEventPublisher.Publish(primaryEvents.Count, secondaryEvents.ToArray());

        ResetIds(primaryEvents, secondaryEvents);

        primaryEvents.ShouldMatchChildSnapshot("primary");
        secondaryEvents.ShouldMatchChildSnapshot("secondary");

        var candidateId = Guid.Parse(MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen);
        var primaryEVotingEvent = primaryEvents.Single(x => x.ImportType == SharedProto.ResultImportType.Evoting);
        var primaryResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryEVotingEvent.MajorityElectionId)));
        primaryResult.CountOfElectionsWithUnmappedEVotingWriteIns.Should().Be(0);
        primaryResult.HasUnmappedWriteIns.Should().BeFalse();
        primaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(3);
        primaryResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Candidate && x.CandidateId == candidateId)
            .Should()
            .BeTrue();

        var secondaryEVotingEvent = secondaryEvents.Single(x => x.ImportType == SharedProto.ResultImportType.Evoting);
        var secondaryResult = await RunOnDb(db => db.SecondaryMajorityElectionResults
            .Include(x => x.WriteInMappings)
            .SingleAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.SecondaryMajorityElectionId == Guid.Parse(secondaryEVotingEvent.SecondaryMajorityElectionId)));
        secondaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(3);
        secondaryResult.WriteInMappings.All(x => x.Target == MajorityElectionWriteInMappingTarget.Individual).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldWorkWithInvalidBallotTarget()
    {
        var resultBefore = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.CountOfVoters)
            .Include(x => x.CandidateResults)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen)));

        resultBefore.CountOfVoters.EVotingSubTotal.InvalidBallots.Should().Be(0);
        resultBefore.CountOfVoters.EVotingSubTotal.AccountedBallots.Should().Be(8);
        resultBefore.EVotingSubTotal.EmptyVoteCountInclWriteIns.Should().Be(31);
        resultBefore.EmptyVoteCount.Should().Be(31);
        resultBefore.CandidateResults.First().EVotingInclWriteInsVoteCount.Should().Be(3);
        resultBefore.TotalCandidateVoteCountExclIndividual.Should().Be(3);

        var groups = await FetchMappings();
        var (primaryEvents, secondaryEvents) = await MapMappings(groups, (mapping, writeIn) =>
        {
            switch (mapping.WriteInCandidateName)
            {
                case "Hans Muster":
                    writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Empty;
                    break;
                case "Hans Mueller":
                    // Should result in some invalid ballots, as this is sometimes the only entry on a ballot
                    writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Empty;
                    break;
                case "vereinzelt":
                    writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.InvalidBallot;
                    break;
            }
        });

        await TestEventPublisher.Publish(primaryEvents.ToArray());
        await TestEventPublisher.Publish(primaryEvents.Count, secondaryEvents.ToArray());

        var resultAfter = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.CountOfVoters)
            .Include(x => x.CandidateResults)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen)));

        resultAfter.CountOfVoters.EVotingSubTotal.InvalidBallots.Should().Be(5);
        resultAfter.CountOfVoters.EVotingSubTotal.AccountedBallots.Should().Be(3);
        resultAfter.EVotingSubTotal.EmptyVoteCountInclWriteIns.Should().Be(12);
        resultAfter.EmptyVoteCount.Should().Be(12);
        resultAfter.CandidateResults.First().EVotingInclWriteInsVoteCount.Should().Be(2);
        resultAfter.TotalCandidateVoteCountExclIndividual.Should().Be(2);
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            var groups = await FetchMappings();
            await MapMappings(groups, (mapping, writeIn) =>
            {
                switch (mapping.WriteInCandidateName)
                {
                    case "Hans Muster":
                        writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Empty;
                        break;
                    case "Hans Mueller":
                        writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Candidate;
                        writeIn.CandidateId = MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen;
                        break;
                }
            });

            return
            [
                .. EventPublisherMock.GetPublishedEventsWithMetadata<MajorityElectionWriteInsMapped>(),
                .. EventPublisherMock.GetPublishedEventsWithMetadata<SecondaryMajorityElectionWriteInsMapped>(),
            ];
        });
    }

    [Fact]
    public async Task ShouldWorkAsElectionAdminWithInvalidVotes()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidStGallenEvoting,
            x => x.MajorityElectionInvalidVotes = true,
            true);
        var groups = await FetchMappings();
        var (primaryEvents, secondaryEvents) = await MapMappings(
            groups,
            (_, writeIn) => writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Invalid);

        ResetIds(primaryEvents, secondaryEvents);
        primaryEvents.ShouldMatchChildSnapshot("primary");
        secondaryEvents.ShouldMatchChildSnapshot("secondary");
    }

    [Fact]
    public async Task ShouldWorkAsContestManagerDuringTestingPhase()
    {
        await ModifyDbEntities<SecondaryMajorityElection>(
            x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen),
            x => x.IndividualCandidatesDisabled = false);

        var groups = await FetchMappings();
        var (primaryEvents, secondaryEvents) = await MapMappings(
            groups,
            (mapping, writeIn) =>
            {
                switch (mapping.WriteInCandidateName)
                {
                    case "Hans Muster":
                        writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Empty;
                        break;
                    case "Hans Mueller":
                        writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Candidate;
                        writeIn.CandidateId = MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen;
                        break;
                }
            },
            StGallenErfassungElectionAdminClient);

        await TestEventPublisher.Publish(primaryEvents.ToArray());
        await TestEventPublisher.Publish(primaryEvents.Count, secondaryEvents.ToArray());

        ResetIds(primaryEvents, secondaryEvents);

        primaryEvents.ShouldMatchChildSnapshot("primary");
        secondaryEvents.ShouldMatchChildSnapshot("secondary");

        var candidateId = Guid.Parse(MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen);
        var primaryEVotingEvent = primaryEvents.Single(e => e.ImportType == SharedProto.ResultImportType.Evoting);
        var primaryResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryEVotingEvent.MajorityElectionId)));
        primaryResult.CountOfElectionsWithUnmappedEVotingWriteIns.Should().Be(0);
        primaryResult.HasUnmappedEVotingWriteIns.Should().BeFalse();
        primaryResult.HasUnmappedWriteIns.Should().BeFalse();
        primaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(3);
        primaryResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Candidate && x.CandidateId == candidateId)
            .Should()
            .BeTrue();

        var primaryECountingEvent = primaryEvents.Single(e => e.ImportType == SharedProto.ResultImportType.Ecounting);
        var primaryECountingResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryECountingEvent.MajorityElectionId)));
        primaryECountingResult.CountOfElectionsWithUnmappedECountingWriteIns.Should().Be(0);
        primaryECountingResult.HasUnmappedECountingWriteIns.Should().BeFalse();
        primaryECountingResult.HasUnmappedWriteIns.Should().BeFalse();
        primaryECountingResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Candidate && x.CandidateId == candidateId)
            .Should()
            .BeTrue();

        var secondaryEVotingEvent = secondaryEvents.Single(e => e.ImportType == SharedProto.ResultImportType.Evoting);
        var secondaryResult = await RunOnDb(db => db.SecondaryMajorityElectionResults
            .Include(x => x.WriteInMappings)
            .SingleAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.SecondaryMajorityElectionId == Guid.Parse(secondaryEVotingEvent.SecondaryMajorityElectionId)));
        secondaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(3);
        secondaryResult.WriteInMappings.All(x => x.Target == MajorityElectionWriteInMappingTarget.Individual).Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        var groups = await FetchMappings();
        await AssertStatus(
            async () => await MapMappings(groups.PrimaryEVotingMappings, null, StGallenErfassungElectionAdminClient),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldThrowWithInvalidVoteMappingsButNoInvalidVotes()
    {
        var groups = await FetchMappings();

        await AssertStatus(
            async () => await MapMappings(groups.PrimaryEVotingMappings, (_, m) => m.Target = SharedProto.MajorityElectionWriteInMappingTarget.Invalid),
            StatusCode.InvalidArgument,
            "Invalid votes are not enabled on this election");

        await AssertStatus(
            async () => await MapMappings(groups.PrimaryECountingMappings, (_, m) => m.Target = SharedProto.MajorityElectionWriteInMappingTarget.Invalid),
            StatusCode.InvalidArgument,
            "Invalid votes are not enabled on this election");

        await AssertStatus(
            async () => await MapMappings(groups.SecondaryEVotingMappings, (_, m) => m.Target = SharedProto.MajorityElectionWriteInMappingTarget.Invalid),
            StatusCode.InvalidArgument,
            "Invalid votes are not enabled on this election");
    }

    [Fact]
    public async Task ShouldThrowWithIndividualVoteMappingsButNoIndividualVotes()
    {
        await ModifyDbEntities<MajorityElection>(
            x => x.Id == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen),
            x => x.IndividualCandidatesDisabled = true);

        await ModifyDbEntities<SecondaryMajorityElection>(
            x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdUzwilMajorityElectionInContestStGallen),
            x => x.IndividualCandidatesDisabled = true);

        await ModifyDbEntities<MajorityElection>(
            x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen),
            x => x.IndividualCandidatesDisabled = true);

        var groups = await FetchMappings();

        await AssertStatus(
            async () => await MapMappings(groups.PrimaryEVotingMappings, (_, m) => m.Target = SharedProto.MajorityElectionWriteInMappingTarget.Individual),
            StatusCode.InvalidArgument,
            "Individual votes are not enabled on this election");

        await AssertStatus(
            async () => await MapMappings(groups.SecondaryEVotingMappings, (_, m) => m.Target = SharedProto.MajorityElectionWriteInMappingTarget.Individual),
            StatusCode.InvalidArgument,
            "Individual votes are not enabled on this election");

        await AssertStatus(
            async () => await MapMappings(groups.PrimaryECountingMappings, (_, m) => m.Target = SharedProto.MajorityElectionWriteInMappingTarget.Individual),
            StatusCode.InvalidArgument,
            "Individual votes are not enabled on this election");
    }

    [Fact]
    public async Task ShouldThrowWithUnknownCandidateIds()
    {
        var groups = await FetchMappings();

        void Mapper(ProtoModels.MajorityElectionWriteInMapping mapping, MapMajorityElectionWriteInRequest writeIn)
        {
            writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Candidate;
            writeIn.CandidateId = "8536002a-b052-42c6-ae7d-6ed6be8da69a";
        }

        await AssertStatus(
            async () => await MapMappings(groups.PrimaryEVotingMappings, Mapper),
            StatusCode.InvalidArgument,
            "Invalid candidates provided");

        await AssertStatus(
            async () => await MapMappings(groups.PrimaryECountingMappings, Mapper),
            StatusCode.InvalidArgument,
            "Invalid candidates provided");

        await AssertStatus(
            async () => await MapMappings(groups.SecondaryEVotingMappings, Mapper),
            StatusCode.InvalidArgument,
            "Invalid candidates provided");
    }

    [Fact]
    public async Task ShouldThrowWithNonMajorityPoliticalBusinessType()
    {
        var groups = await FetchMappings();
        groups.PrimaryEVotingMappings.Election.BusinessType = ProtoModels.PoliticalBusinessType.Vote;

        await AssertStatus(
            async () => await MapMappings(groups.PrimaryEVotingMappings),
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
            async () => await MapMappings(groups.PrimaryEVotingMappings),
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
            .MapMajorityElectionWriteInsAsync(new MapMajorityElectionWriteInsRequest
            {
                ImportId = groups.PrimaryEVotingMappings.ImportId,
                ElectionId = groups.PrimaryEVotingMappings.Election.Id,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
                PoliticalBusinessType = groups.PrimaryEVotingMappings.Election.BusinessType,
                Mappings =
                {
                    groups.PrimaryEVotingMappings.WriteInMappings.Select(m =>
                    {
                        var writeIn = new MapMajorityElectionWriteInRequest
                        {
                            WriteInId = m.Id,
                            Target = SharedProto.MajorityElectionWriteInMappingTarget.Individual,
                        };
                        return writeIn;
                    }),
                },
            });
    }

    private async Task<WriteInGroups>
        FetchMappings()
    {
        var writeIns = await ErfassungElectionAdminClient.GetMajorityElectionWriteInMappingsAsync(
            new GetMajorityElectionWriteInMappingsRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            });

        writeIns.WriteInMappings.Should().HaveCount(4);
        var primaryEVoting = writeIns.WriteInMappings.Single(x =>
            x.ImportType == SharedProto.ResultImportType.Evoting
            && x.Election.BusinessType == ProtoModels.PoliticalBusinessType.MajorityElection);
        var secondaryEVoting = writeIns.WriteInMappings.Single(x =>
            x.ImportType == SharedProto.ResultImportType.Evoting
            && x.Election.BusinessType == ProtoModels.PoliticalBusinessType.SecondaryMajorityElection);
        var primaryECounting = writeIns.WriteInMappings.Single(x =>
            x.ImportType == SharedProto.ResultImportType.Ecounting
            && x.Election.BusinessType == ProtoModels.PoliticalBusinessType.MajorityElection
            && x.Election.Id == MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen);

        primaryEVoting.WriteInMappings.Should().HaveCount(4);
        secondaryEVoting.WriteInMappings.Should().HaveCount(1);
        primaryECounting.WriteInMappings.Should().HaveCount(1);
        return new WriteInGroups(primaryEVoting, primaryECounting, secondaryEVoting);
    }

    private async Task<(IReadOnlyCollection<MajorityElectionWriteInsMapped> PrimaryEvents, IReadOnlyCollection<SecondaryMajorityElectionWriteInsMapped> SecondaryEvents)> MapMappings(
        WriteInGroups groups,
        Action<ProtoModels.MajorityElectionWriteInMapping, MapMajorityElectionWriteInRequest>? customizer = null,
        ResultImportService.ResultImportServiceClient? service = null)
    {
        await MapMappings(groups.PrimaryECountingMappings, customizer, service);
        await MapMappings(groups.PrimaryEVotingMappings, customizer, service);
        await MapMappings(groups.SecondaryEVotingMappings, customizer, service);
        return (
            EventPublisherMock.GetPublishedEvents<MajorityElectionWriteInsMapped>().ToList(),
            EventPublisherMock.GetPublishedEvents<SecondaryMajorityElectionWriteInsMapped>().ToList());
    }

    private async Task MapMappings(
        ProtoModels.MajorityElectionWriteInMappings mappings,
        Action<ProtoModels.MajorityElectionWriteInMapping, MapMajorityElectionWriteInRequest>? customizer = null,
        ResultImportService.ResultImportServiceClient? service = null)
    {
        await (service ?? ErfassungElectionAdminClient).MapMajorityElectionWriteInsAsync(new MapMajorityElectionWriteInsRequest
        {
            ImportId = mappings.ImportId,
            ElectionId = mappings.Election.Id,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
            PoliticalBusinessType = mappings.Election.BusinessType,
            Mappings =
                {
                    mappings.WriteInMappings.Select(m =>
                    {
                        var writeIn = new MapMajorityElectionWriteInRequest
                        {
                            WriteInId = m.Id,
                            Target = SharedProto.MajorityElectionWriteInMappingTarget.Individual,
                        };
                        customizer?.Invoke(m, writeIn);
                        return writeIn;
                    }),
                },
        });
    }

    private void ResetIds(
        IReadOnlyCollection<MajorityElectionWriteInsMapped> primaryEvents,
        IReadOnlyCollection<SecondaryMajorityElectionWriteInsMapped> secondaryEvents)
    {
        foreach (var evnt in primaryEvents)
        {
            evnt.ImportId.Should().NotBeEmpty();
            evnt.ImportId = string.Empty;
        }

        foreach (var evnt in secondaryEvents)
        {
            evnt.ImportId.Should().NotBeEmpty();
            evnt.ImportId = string.Empty;
        }

        ResetIds(primaryEvents.SelectMany(x => x.WriteInMappings));
        ResetIds(secondaryEvents.SelectMany(x => x.WriteInMappings));
    }

    private void ResetIds(IEnumerable<MajorityElectionWriteInMappedEventData> writeInMappings)
    {
        foreach (var mapping in writeInMappings)
        {
            mapping.WriteInMappingId = string.Empty;
        }
    }

    private record WriteInGroups(
        ProtoModels.MajorityElectionWriteInMappings PrimaryEVotingMappings,
        ProtoModels.MajorityElectionWriteInMappings PrimaryECountingMappings,
        ProtoModels.MajorityElectionWriteInMappings SecondaryEVotingMappings);
}
