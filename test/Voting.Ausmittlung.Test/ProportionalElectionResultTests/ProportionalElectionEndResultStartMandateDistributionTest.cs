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
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionEndResultStartMandateDistributionTest : ProportionalElectionEndResultBaseTest
{
    public ProportionalElectionEndResultStartMandateDistributionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedElectionAndFinishSubmissions();
        await SetAllAuditedTentatively();
    }

    [Fact]
    public async Task ShouldWork()
    {
        await MonitoringElectionAdminClient.StartEndResultMandateDistributionAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultMandateDistributionStarted>();
        ev.ProportionalElectionId.Should().Be(ProportionalElectionEndResultMockedData.ElectionId);
        ev.ProportionalElectionEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await MonitoringElectionAdminClient.StartEndResultMandateDistributionAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionEndResultMandateDistributionStarted>();
        });
    }

    [Fact]
    public async Task TestShouldWorkAfterTestingPhaseEnded()
    {
        var request = NewValidRequest();
        var electionId = Guid.Parse(request.ProportionalElectionId);
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        await MonitoringElectionAdminClient.StartEndResultMandateDistributionAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultMandateDistributionStarted>();
        await RunEvents<ProportionalElectionEndResultMandateDistributionStarted>();

        var endResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, false);
        evInTestingPhase.ProportionalElectionEndResultId.Should().Be(endResultInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(GetNextEventNumber(), new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        await ModifyDbEntities<ProportionalElectionEndResult>(
            e => e.ProportionalElectionId == electionId,
            e => e.CountOfDoneCountingCircles = e.TotalCountOfCountingCircles);

        await MonitoringElectionAdminClient.StartEndResultMandateDistributionAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultMandateDistributionStarted>();
        await RunEvents<ProportionalElectionEndResultMandateDistributionStarted>();

        var endResultTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, true);
        evTestingPhaseEnded.ProportionalElectionEndResultId.Should().Be(endResultTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task ShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.StartEndResultMandateDistributionAsync(NewValidRequest()),
            StatusCode.FailedPrecondition);
    }

    [Fact]
    public async Task ShouldThrowCountingCirclesNotDone()
    {
        await ModifyDbEntities<ProportionalElectionEndResult>(
            x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x => x.CountOfDoneCountingCircles--);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.StartEndResultMandateDistributionAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Not all counting circles are done");
    }

    [Fact]
    public async Task ShouldThrowIfAlreadyCalculated()
    {
        await TriggerMandateDistribution();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.StartEndResultMandateDistributionAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Cannot start mandate distribution, if it is already triggered");
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        return AssertStatus(
            async () => await CreateService("unknown-tenant", roles: RolesMockedData.MonitoringElectionAdmin).StartEndResultMandateDistributionAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestProcessor()
    {
        var electionGuid = Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId);
        var result = await RunOnDb(x => x.ProportionalElectionEndResult.FirstAsync(r => r.ProportionalElectionId == electionGuid));
        result.Finalized.Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionEndResultMandateDistributionStarted
            {
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                ProportionalElectionEndResultId = result.Id.ToString(),
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = electionGuid.ToString(),
        });

        endResult.MatchSnapshot();
        endResult.Finalized.Should().BeFalse();
        endResult.ManualEndResultRequired.Should().BeFalse();
        endResult.MandateDistributionTriggered.Should().BeTrue();
        endResult.ListEndResults.Any(l => l.NumberOfMandates != 0).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.CandidateEndResults.Any(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.Elected)).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.HasOpenRequiredLotDecisions).Should().BeTrue();

        await AssertHasPublishedEventProcessedMessage(
            ProportionalElectionEndResultMandateDistributionStarted.Descriptor,
            result.Id);
    }

    [Fact]
    public async Task TestProcessorWithDisabledCantonSettingsEndResultFinalize()
    {
        var electionGuid = Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId);

        await ModifyDbEntities<ContestCantonDefaults>(
            _ => true,
            x => x.EndResultFinalizeDisabled = true,
            splitQuery: true);

        var result = await RunOnDb(x => x.ProportionalElectionEndResult.FirstAsync(r => r.ProportionalElectionId == electionGuid));
        result.Finalized.Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionEndResultMandateDistributionStarted
            {
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                ProportionalElectionEndResultId = result.Id.ToString(),
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = electionGuid.ToString(),
        });

        endResult.Finalized.Should().BeTrue();
    }

    [Fact]
    public async Task TestProcessorWithNonUnionDpAlgorithm()
    {
        var electionGuid = Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId);

        await ModifyDbEntities<ProportionalElection>(
            x => x.Id == electionGuid,
            x => x.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum);

        var result = await RunOnDb(x => x.ProportionalElectionEndResult.FirstAsync(r => r.ProportionalElectionId == electionGuid));
        result.Finalized.Should().BeFalse();
        result.MandateDistributionTriggered.Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionEndResultMandateDistributionStarted
            {
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                ProportionalElectionEndResultId = result.Id.ToString(),
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = electionGuid.ToString(),
        });

        endResult.MatchSnapshot();
        endResult.Finalized.Should().BeFalse();
        endResult.ManualEndResultRequired.Should().BeFalse();
        endResult.MandateDistributionTriggered.Should().BeTrue();
        endResult.ListEndResults.Any(l => l.NumberOfMandates != 0).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.CandidateEndResults.Any(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.Elected)).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.HasOpenRequiredLotDecisions).Should().BeTrue();
    }

    [Fact]
    public async Task TestProcessorWithNonUnionDpAlgorithmAndSuperApportionmentLotDecisions()
    {
        await ZhMockedData.Seed(RunScoped);

        var electionGuid = ZhMockedData.ProportionalElectionGuidSingleDoiSuperLot;

        var result = await RunOnDb(x => x.ProportionalElectionEndResult.FirstAsync(r => r.ProportionalElectionId == electionGuid));
        result.Finalized.Should().BeFalse();
        result.MandateDistributionTriggered.Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionEndResultMandateDistributionStarted
            {
                ProportionalElectionId = electionGuid.ToString(),
                ProportionalElectionEndResultId = result.Id.ToString(),
                EventInfo = GetMockedEventInfo(),
            });

        var client = CreateService(SecureConnectTestDefaults.MockedTenantBund.Id, roles: new[] { RolesMockedData.MonitoringElectionAdmin });

        var endResult = await client.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = electionGuid.ToString(),
        });

        endResult.Finalized.Should().BeFalse();
        endResult.ManualEndResultRequired.Should().BeFalse();
        endResult.MandateDistributionTriggered.Should().BeTrue();
        endResult.ListEndResults.Any(l => l.NumberOfMandates != 0).Should().BeFalse();
        endResult.ListEndResults.Any(l => l.CandidateEndResults.All(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.Pending)).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.HasOpenRequiredLotDecisions).Should().BeFalse();
    }

    [Fact]
    public async Task TestProcessorWithManualEndResult()
    {
        var electionGuid = Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId);

        await RunOnDb(async db =>
        {
            var endResult = await db.ProportionalElectionEndResult
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.ProportionalElection)
                .Include(x => x.ListEndResults.OrderBy(y => y.List.OrderNumber))
                .ThenInclude(x => x.CandidateEndResults)
                .SingleAsync(x => x.ProportionalElectionId == electionGuid);

            endResult.ProportionalElection.NumberOfMandates = 3;

            var listEndResults = endResult.ListEndResults.ToList();
            listEndResults[0].ConventionalSubTotal.UnmodifiedListVotesCount = 1000;
            listEndResults[1].ConventionalSubTotal.UnmodifiedListVotesCount = 1000;
            listEndResults[2].ConventionalSubTotal.UnmodifiedListVotesCount = 0;
            listEndResults[3].ConventionalSubTotal.UnmodifiedListVotesCount = 0;

            foreach (var listEndResult in listEndResults)
            {
                listEndResult.ConventionalSubTotal.ModifiedListVotesCount = 0;
                listEndResult.ConventionalSubTotal.UnmodifiedListBlankRowsCount = 0;
                listEndResult.ConventionalSubTotal.ModifiedListBlankRowsCount = 0;
            }

            await db.SaveChangesAsync();
        });

        var result = await RunOnDb(x => x.ProportionalElectionEndResult.FirstAsync(r => r.ProportionalElectionId == electionGuid));
        result.Finalized.Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionEndResultMandateDistributionStarted
            {
                ProportionalElectionId = electionGuid.ToString(),
                ProportionalElectionEndResultId = result.Id.ToString(),
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = electionGuid.ToString(),
        });

        endResult.MatchSnapshot();
        endResult.ManualEndResultRequired.Should().BeTrue();

        endResult.ListEndResults.Any().Should().BeTrue();
        endResult.ListEndResults.All(l => l.NumberOfMandates == 0).Should().BeTrue();
        endResult.ListEndResults.All(l => l.CandidateEndResults.All(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.NotElected)).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.HasOpenRequiredLotDecisions).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldThrowWithUnionDpAlgorithm()
    {
        await ModifyDbEntities<ProportionalElection>(
            x => x.Id == ProportionalElectionEndResultMockedData.ElectionGuid,
            x => x.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum);

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.StartEndResultMandateDistributionAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Cannot start mandate distribution with a union mandate algorithm");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .StartEndResultMandateDistributionAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private StartProportionalElectionEndResultMandateDistributionRequest NewValidRequest()
    {
        return new()
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
        };
    }
}
