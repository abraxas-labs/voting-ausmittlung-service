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
using Google.Protobuf.Collections;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
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
        var (importId, primaryMappings, secondaryMappings) = await FetchMappings();
        var (primaryEvent, secondaryEvent) = await MapMappings(importId, primaryMappings, secondaryMappings, (mapping, writeIn) =>
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

        await TestEventPublisher.Publish(primaryEvent);
        await TestEventPublisher.Publish(1, secondaryEvent);

        ResetIds(primaryEvent.WriteInMappings);
        ResetIds(secondaryEvent.WriteInMappings);

        primaryEvent.ShouldMatchChildSnapshot("primary");
        secondaryEvent.ShouldMatchChildSnapshot("secondary");

        var candidateId = Guid.Parse(MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen);
        var primaryResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryEvent.MajorityElectionId)));
        primaryResult.CountOfElectionsWithUnmappedWriteIns.Should().Be(0);
        primaryResult.HasUnmappedWriteIns.Should().BeFalse();
        primaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(3);
        primaryResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Candidate && x.CandidateId == candidateId)
            .Should()
            .BeTrue();

        var secondaryResult = await RunOnDb(db => db.SecondaryMajorityElectionResults
            .Include(x => x.WriteInMappings)
            .SingleAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.SecondaryMajorityElectionId == Guid.Parse(secondaryEvent.SecondaryMajorityElectionId)));
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

        resultBefore.CountOfVoters.EVotingInvalidBallots.Should().Be(0);
        resultBefore.CountOfVoters.EVotingAccountedBallots.Should().Be(8);
        resultBefore.EVotingSubTotal.EmptyVoteCountInclWriteIns.Should().Be(31);
        resultBefore.EmptyVoteCount.Should().Be(31);
        resultBefore.CandidateResults.First().EVotingInclWriteInsVoteCount.Should().Be(3);
        resultBefore.TotalCandidateVoteCountExclIndividual.Should().Be(3);

        var (importId, primaryMappings, secondaryMappings) = await FetchMappings();
        var (primaryEvent, secondaryEvent) = await MapMappings(importId, primaryMappings, secondaryMappings, (mapping, writeIn) =>
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

        await TestEventPublisher.Publish(primaryEvent);
        await TestEventPublisher.Publish(1, secondaryEvent);

        var resultAfter = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.CountOfVoters)
            .Include(x => x.CandidateResults)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen)));

        resultAfter.CountOfVoters.EVotingInvalidBallots.Should().Be(5);
        resultAfter.CountOfVoters.EVotingAccountedBallots.Should().Be(3);
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
            var (importId, primaryMappings, secondaryMappings) = await FetchMappings();

            await MapMappings(importId, primaryMappings, secondaryMappings, (mapping, writeIn) =>
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

            return new[]
            {
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionWriteInsMapped>(),
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<SecondaryMajorityElectionWriteInsMapped>(),
            };
        });
    }

    [Fact]
    public async Task ShouldWorkAsElectionAdminWithInvalidVotes()
    {
        var id = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(Guid.Parse(ContestMockedData.IdStGallenEvoting), Guid.Parse(DomainOfInfluenceMockedData.IdUzwil));
        await ModifyDbEntities(
            (DomainOfInfluence doi) => doi.Id == id,
            doi => doi.CantonDefaults.MajorityElectionInvalidVotes = true);
        var (importId, primaryMappings, secondaryMappings) = await FetchMappings();
        var (primaryEvent, secondaryEvent) = await MapMappings(
            importId,
            primaryMappings,
            secondaryMappings,
            (_, writeIn) => writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Invalid);

        ResetIds(primaryEvent.WriteInMappings);
        ResetIds(secondaryEvent.WriteInMappings);

        primaryEvent.ShouldMatchChildSnapshot("primary");
        secondaryEvent.ShouldMatchChildSnapshot("secondary");
    }

    [Fact]
    public async Task ShouldWorkAsContestManagerDuringTestingPhase()
    {
        var (importId, primaryMappings, secondaryMappings) = await FetchMappings();
        var (primaryEvent, secondaryEvent) = await MapMappings(
            importId,
            primaryMappings,
            secondaryMappings,
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

        await TestEventPublisher.Publish(primaryEvent);
        await TestEventPublisher.Publish(1, secondaryEvent);

        ResetIds(primaryEvent.WriteInMappings);
        ResetIds(secondaryEvent.WriteInMappings);

        primaryEvent.ShouldMatchChildSnapshot("primary");
        secondaryEvent.ShouldMatchChildSnapshot("secondary");

        var candidateId = Guid.Parse(MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen);
        var primaryResult = await RunOnDb(db => db.MajorityElectionResults
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .SingleAsync(x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.MajorityElectionId == Guid.Parse(primaryEvent.MajorityElectionId)));
        primaryResult.CountOfElectionsWithUnmappedWriteIns.Should().Be(0);
        primaryResult.HasUnmappedWriteIns.Should().BeFalse();
        primaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(3);
        primaryResult.WriteInMappings
            .Where(x => x.WriteInCandidateName == "Hans Mueller")
            .All(x => x.Target == MajorityElectionWriteInMappingTarget.Candidate && x.CandidateId == candidateId)
            .Should()
            .BeTrue();

        var secondaryResult = await RunOnDb(db => db.SecondaryMajorityElectionResults
            .Include(x => x.WriteInMappings)
            .SingleAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.SecondaryMajorityElectionId == Guid.Parse(secondaryEvent.SecondaryMajorityElectionId)));
        secondaryResult.EVotingSubTotal.IndividualVoteCount.Should().Be(3);
        secondaryResult.WriteInMappings.All(x => x.Target == MajorityElectionWriteInMappingTarget.Individual).Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        var (importId, primaryMappings, _) = await FetchMappings();
        await AssertStatus(
            async () => await MapMappings(importId, primaryMappings, null, StGallenErfassungElectionAdminClient),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldThrowWithInvalidVoteMappingsButNoInvalidVotes()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => doi.Id == Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            doi => doi.CantonDefaults.MajorityElectionInvalidVotes = true);

        var (importId, primaryMappings, secondaryMappings) = await FetchMappings();

        await AssertStatus(
            async () => await MapMappings(importId, primaryMappings, (_, m) => m.Target = SharedProto.MajorityElectionWriteInMappingTarget.Invalid),
            StatusCode.InvalidArgument,
            "Invalid votes are not enabled on this election");

        await AssertStatus(
            async () => await MapMappings(importId, secondaryMappings, (_, m) => m.Target = SharedProto.MajorityElectionWriteInMappingTarget.Invalid),
            StatusCode.InvalidArgument,
            "Invalid votes are not enabled on this election");
    }

    [Fact]
    public async Task ShouldThrowWithUnknownCandidateIds()
    {
        var (importId, primaryMappings, secondaryMappings) = await FetchMappings();

        void Mapper(ProtoModels.MajorityElectionWriteInMapping mapping, MapMajorityElectionWriteInRequest writeIn)
        {
            writeIn.Target = SharedProto.MajorityElectionWriteInMappingTarget.Candidate;
            writeIn.CandidateId = "8536002a-b052-42c6-ae7d-6ed6be8da69a";
        }

        await AssertStatus(
            async () => await MapMappings(importId, primaryMappings, Mapper),
            StatusCode.InvalidArgument,
            "Invalid candidates provided");

        await AssertStatus(
            async () => await MapMappings(importId, secondaryMappings, Mapper),
            StatusCode.InvalidArgument,
            "Invalid candidates provided");
    }

    [Fact]
    public async Task ShouldThrowWithNonMajorityPoliticalBusinessType()
    {
        var (importId, primaryMappings, _) = await FetchMappings();
        primaryMappings.Election.BusinessType = ProtoModels.PoliticalBusinessType.Vote;

        await AssertStatus(
            async () => await MapMappings(importId, primaryMappings),
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
            async () => await MapMappings(importId, primaryMappings),
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
            .MapMajorityElectionWriteInsAsync(new MapMajorityElectionWriteInsRequest
            {
                ElectionId = "eebc9095-8ba3-4dbb-b2ae-99e0a5e1b965",
                ImportId = "5649dc51-9558-4aef-9c1b-41f37868809e",
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

    private async Task<(MajorityElectionWriteInsMapped PrimaryEvent, SecondaryMajorityElectionWriteInsMapped SecondaryEvent)> MapMappings(
        string importId,
        ProtoModels.MajorityElectionWriteInMappings primaryMappings,
        ProtoModels.MajorityElectionWriteInMappings secondaryMappings,
        Action<ProtoModels.MajorityElectionWriteInMapping, MapMajorityElectionWriteInRequest>? customizer = null,
        ResultImportService.ResultImportServiceClient? service = null)
    {
        await MapMappings(importId, primaryMappings, customizer, service);
        await MapMappings(importId, secondaryMappings, customizer, service);
        return (
            EventPublisherMock.GetSinglePublishedEvent<MajorityElectionWriteInsMapped>(),
            EventPublisherMock.GetSinglePublishedEvent<SecondaryMajorityElectionWriteInsMapped>());
    }

    private async Task MapMappings(
        string importId,
        ProtoModels.MajorityElectionWriteInMappings mappings,
        Action<ProtoModels.MajorityElectionWriteInMapping, MapMajorityElectionWriteInRequest>? customizer = null,
        ResultImportService.ResultImportServiceClient? service = null)
    {
        await (service ?? ErfassungElectionAdminClient).MapMajorityElectionWriteInsAsync(new MapMajorityElectionWriteInsRequest
        {
            ImportId = importId,
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

    private void ResetIds(RepeatedField<MajorityElectionWriteInMappedEventData> writeInMappings)
    {
        foreach (var mapping in writeInMappings)
        {
            mapping.WriteInMappingId = string.Empty;
        }
    }
}
