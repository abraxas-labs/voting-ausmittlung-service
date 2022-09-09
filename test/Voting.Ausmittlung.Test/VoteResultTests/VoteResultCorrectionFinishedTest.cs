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
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultCorrectionFinishedTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public VoteResultCorrectionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.ReadyForCorrection);
            await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultCorrectionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithoutComment()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest(x => x.Comment = string.Empty));
        EventPublisherMock.GetSinglePublishedEvent<VoteResultCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAsync(
                new VoteResultCorrectionFinishedRequest
                {
                    VoteResultId = IdNotFound,
                    SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAsync(
                new VoteResultCorrectionFinishedRequest
                {
                    VoteResultId = IdBadFormat,
                    SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
                }),
            StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultCorrectionFinished
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                Comment = "my-comment",
                EventInfo = new EventInfo
                {
                    Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                    User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                    Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                },
            });
        await AssertCurrentState(CountingCircleResultState.CorrectionDone);
        var comments = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult))
            .ToListAsync());
        comments.MatchSnapshot(x => x.Id);

        var id = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult);
        await AssertHasPublishedMessage<ResultStateChanged>(x =>
            x.Id == id
            && x.NewState == CountingCircleResultState.CorrectionDone);
    }

    [Fact]
    public async Task TestProcessorNoComment()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultCorrectionFinished
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                EventInfo = GetMockedEventInfo(),
            });
        await AssertCurrentState(CountingCircleResultState.CorrectionDone);
        var comment = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult))
            .FirstOrDefaultAsync());
        comment.Should().BeNull();
    }

    [Fact]
    public async Task TestShouldThrowDataChanged()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);

        await RunScoped<TemporaryDataContext>(async db =>
        {
            var item = await db.SecondFactorTransactions
                .AsTracking()
                .FirstAsync(x => x.ExternalIdentifier == SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction);

            item.ActionId = "updated-action-id";
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Data changed during the second factor transaction");
    }

    [Fact]
    public async Task TestShouldThrowNotVerified()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);

        const string invalidExternalId = "a11c61aa-af52-431b-9c0e-f86d24d8a72b";
        await RunScoped<TemporaryDataContext>(async db =>
        {
            var item = await db.SecondFactorTransactions
                .AsTracking()
                .FirstAsync(x => x.ExternalIdentifier == SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction);

            item.ExternalIdentifier = invalidExternalId;
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAsync(new VoteResultCorrectionFinishedRequest
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                SecondFactorTransactionId = invalidExternalId,
            }),
            StatusCode.FailedPrecondition,
            "Second factor transaction is not verified");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await new VoteResultService.VoteResultServiceClient(channel)
            .CorrectionFinishedAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private VoteResultCorrectionFinishedRequest NewValidRequest(
        Action<VoteResultCorrectionFinishedRequest>? customizer = null)
    {
        var req = new VoteResultCorrectionFinishedRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            Comment = "my-comment",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };
        customizer?.Invoke(req);
        return req;
    }
}
