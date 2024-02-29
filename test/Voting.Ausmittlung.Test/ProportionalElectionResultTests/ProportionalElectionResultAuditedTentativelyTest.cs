// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultAuditedTentativelyTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultAuditedTentativelyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultAuditedTentatively>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminAfterCorrection()
    {
        await RunToState(CountingCircleResultState.CorrectionDone);
        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultAuditedTentatively>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionDone);
            await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultAuditedTentatively>();
        });
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(IdNotFound))),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(IdBadFormat))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowDuplicate()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen))),
            StatusCode.InvalidArgument,
            "duplicate");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);

        await RunOnDb(async db =>
        {
            var endResult = await db.ProportionalElectionEndResult
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.ListEndResults.OrderBy(y => y.List.OrderNumber))
                .ThenInclude(x => x.CandidateEndResults)
                .SingleAsync(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen));

            var listEndResults = endResult.ListEndResults.ToList();
            listEndResults[0].ConventionalSubTotal.UnmodifiedListVotesCount = 500;
            listEndResults[1].ConventionalSubTotal.UnmodifiedListVotesCount = 500;
            listEndResults[2].ConventionalSubTotal.UnmodifiedListVotesCount = 500;

            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultAuditedTentatively
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                EventInfo = GetMockedEventInfo(),
            });
        await AssertCurrentState(CountingCircleResultState.AuditedTentatively);

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });

        endResult.MatchSnapshot();
        endResult.ManualEndResultRequired.Should().BeFalse();
        endResult.ListEndResults.Any(l => l.NumberOfMandates != 0).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.CandidateEndResults.Any(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.Elected)).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.HasOpenRequiredLotDecisions).Should().BeTrue();

        var id = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen;
        await AssertHasPublishedMessage<ResultStateChanged>(x =>
            x.Id == id
            && x.NewState == CountingCircleResultState.AuditedTentatively);
    }

    [Fact]
    public async Task TestProcessorWithManualEndResult()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);

        await RunOnDb(async db =>
        {
            var endResult = await db.ProportionalElectionEndResult
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.ListEndResults.OrderBy(y => y.List.OrderNumber))
                .ThenInclude(x => x.CandidateEndResults)
                .SingleAsync(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen));

            var listEndResults = endResult.ListEndResults.ToList();
            listEndResults[0].ConventionalSubTotal.UnmodifiedListVotesCount = 1000;
            listEndResults[1].ConventionalSubTotal.UnmodifiedListVotesCount = 1000;
            listEndResults[2].ConventionalSubTotal.UnmodifiedListVotesCount = 0;

            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultAuditedTentatively
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                EventInfo = GetMockedEventInfo(),
            });
        await AssertCurrentState(CountingCircleResultState.AuditedTentatively);

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });

        endResult.MatchSnapshot();
        endResult.ManualEndResultRequired.Should().BeTrue();

        endResult.ListEndResults.Any().Should().BeTrue();
        endResult.ListEndResults.All(l => l.NumberOfMandates == 0).Should().BeTrue();
        endResult.ListEndResults.All(l => l.CandidateEndResults.All(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.NotElected)).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.HasOpenRequiredLotDecisions).Should().BeTrue();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .AuditedTentativelyAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private ProportionalElectionResultAuditedTentativelyRequest NewValidRequest(Action<ProportionalElectionResultAuditedTentativelyRequest>? customizer = null)
    {
        var r = new ProportionalElectionResultAuditedTentativelyRequest
        {
            ElectionResultIds =
            {
                ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            },
        };
        customizer?.Invoke(r);
        return r;
    }
}
