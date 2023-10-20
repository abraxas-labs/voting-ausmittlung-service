// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class GetProtocolExportStateChangesTest : BaseTest<ExportService.ExportServiceClient>
{
    public GetProtocolExportStateChangesTest(TestApplicationFactory factory)
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
        await MajorityElectionUnionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);

        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());

        // Cannot export until the end result has been finalized
        await ModifyDbEntities<SimplePoliticalBusiness>(
            _ => true,
            x => x.EndResultFinalized = true);

        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
    }

    [Fact]
    public async Task TestShouldNotifyCaller()
    {
        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var responseStream = StGallenMonitoringElectionAdminClient.GetProtocolExportStateChanges(
            new GetProtocolExportStateChangesRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
            },
            new CallOptions(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(2, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(2), callCts.Token);

        // The generic export template ID of the activity protocol, which is the same for all contests
        var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungPdfContestTemplates.ActivityProtocol.Key,
            SecureConnectTestDefaults.MockedTenantStGallen.Id);

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
                var responseStream = StGallenMonitoringElectionAdminClient.GetProtocolExportStateChanges(
                    new GetProtocolExportStateChangesRequest
                    {
                        ContestId = ContestMockedData.IdGossau,
                    },
                    new CallOptions(cancellationToken: cts.Token));

                await responseStream.ResponseStream.MoveNext();
            },
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new ExportService.ExportServiceClient(channel).GetProtocolExportStateChanges(
            new GetProtocolExportStateChangesRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
            },
            new CallOptions(cancellationToken: cts.Token));

        await responseStream.ResponseStream.MoveNext();
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
