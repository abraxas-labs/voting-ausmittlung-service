// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;
using PoliticalBusinessType = Abraxas.Voting.Ausmittlung.Services.V1.Models.PoliticalBusinessType;

namespace Voting.Ausmittlung.Test.ExportTests;

public class GetBundleReviewExportStateChangesTest : BaseTest<ExportService.ExportServiceClient>
{
    public GetBundleReviewExportStateChangesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);

        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());

        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
    }

    [Fact]
    public async Task TestShouldNotifyCaller()
    {
        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var responseStream = ErfassungElectionAdminClient.GetBundleReviewExportStateChanges(
            new GetBundleReviewExportStateChangesRequest
            {
                PoliticalBusinessResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                PoliticalBusinessType = PoliticalBusinessType.ProportionalElection,
            },
            new CallOptions(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(2, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(2), callCts.Token);

        var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungPdfProportionalElectionTemplates.ResultBundleReview.Key,
            SecureConnectTestDefaults.MockedTenantStGallen.Id,
            CountingCircleMockedData.GuidGossau,
            ProportionalElectionMockedData.GossauProportionalElectionInContestStGallen.Id,
            politicalBusinessResultBundleId: ProportionalElectionResultBundleMockedData.GossauBundle1List1.Id);

        var protocolExportId = AusmittlungUuidV5.BuildProtocolExport(
            Guid.Parse(ContestMockedData.IdStGallenEvoting),
            true,
            exportTemplateId);

        // this should be processed
        await PublishMessage(
            new ProtocolExportStateChanged(
                protocolExportId,
                exportTemplateId,
                ProtocolExportState.Completed,
                "test-file.pdf",
                MockedClock.GetDate()));

        var protocolExportIdDifferentContest = AusmittlungUuidV5.BuildProtocolExport(
            Guid.Parse(ContestMockedData.IdKirche),
            false,
            exportTemplateId);

        // this should be ignored (different contest)
        await PublishMessage(
            new ProtocolExportStateChanged(
                protocolExportIdDifferentContest,
                exportTemplateId,
                ProtocolExportState.Completed,
                "test-file.pdf",
                MockedClock.GetDate()));

        // this should be processed
        await PublishMessage(
            new ProtocolExportStateChanged(
                protocolExportId,
                exportTemplateId,
                ProtocolExportState.Failed,
                "test-file.pdf",
                MockedClock.GetDate()));

        var responses = await readResponsesTask;
        responses.Should().HaveCount(2);
        responses.All(x => x.ProtocolExportId == protocolExportId.ToString()).Should().BeTrue();

        callCts.Cancel();
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var responseStream = ErfassungElectionAdminClient.GetBundleReviewExportStateChanges(
                    new GetBundleReviewExportStateChangesRequest
                    {
                        PoliticalBusinessResultId = MajorityElectionResultMockedData.IdKircheElectionResultInContestKirche,
                        PoliticalBusinessType = PoliticalBusinessType.MajorityElection,
                    },
                    new CallOptions(cancellationToken: cts.Token));

                await responseStream.ResponseStream.MoveNext();
            },
            StatusCode.PermissionDenied);
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new ExportService.ExportServiceClient(channel).GetBundleReviewExportStateChanges(
            new GetBundleReviewExportStateChangesRequest
            {
                PoliticalBusinessResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                PoliticalBusinessType = PoliticalBusinessType.ProportionalElection,
            },
            new CallOptions(cancellationToken: cts.Token));

        await responseStream.ResponseStream.ReadNIgnoreCancellation(1, cts.Token);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }
}