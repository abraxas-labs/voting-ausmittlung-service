// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultGetMajorityElectionWriteInMappingChangesTest : BaseTest<ResultImportService.ResultImportServiceClient>
{
    public ResultGetMajorityElectionWriteInMappingChangesTest(TestApplicationFactory factory)
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
    public async Task TestShouldNotifyCaller()
    {
        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        var responseStream = StGallenErfassungElectionAdminClient.GetMajorityElectionWriteInMappingChanges(
            new GetMajorityElectionWriteInMappingChangesRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            },
            new CallOptions(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(2, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(2), callCts.Token);

        var wrongContestElectionResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(
            MajorityElectionMockedData.UzwilMajorityElectionInContestUzwil.Id,
            CountingCircleMockedData.GuidUzwil,
            false);

        // this should be ignored (wrong contest)
        await PublishMessage(new WriteInMappingsChanged(wrongContestElectionResultId, true, 0, 0));

        var wrongCountingCircleElectionResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(
            MajorityElectionMockedData.UzwilMajorityElectionInContestStGallen.Id,
            CountingCircleMockedData.GuidStGallen,
            false);

        // this should be ignored (wrong counting circle)
        await PublishMessage(new WriteInMappingsChanged(wrongCountingCircleElectionResultId, true, 0, 0));

        var electionResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(
            MajorityElectionMockedData.UzwilMajorityElectionInContestStGallen.Id,
            CountingCircleMockedData.GuidUzwil,
            false);

        // this should be processed (reset event)
        await PublishMessage(new WriteInMappingsChanged(electionResultId, true, 0, 0));

        // this should be processed (mapped event)
        await PublishMessage(new WriteInMappingsChanged(electionResultId, false, 10, 20));

        var responses = await readResponsesTask;
        responses.Should().HaveCount(2);
        responses = responses.OrderBy(r => r.IsReset).ToList();

        var mappedResponse = responses[0];
        mappedResponse.IsReset.Should().BeFalse();
        mappedResponse.ResultId.Should().Be(electionResultId.ToString());
        mappedResponse.DuplicatedCandidates.Should().Be(10);
        mappedResponse.InvalidDueToEmptyBallot.Should().Be(20);

        var resetResponse = responses[1];
        resetResponse.IsReset.Should().BeTrue();
        resetResponse.ResultId.Should().Be(electionResultId.ToString());

        callCts.Cancel();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new ResultImportService.ResultImportServiceClient(channel).GetMajorityElectionWriteInMappingChanges(
            new GetMajorityElectionWriteInMappingChangesRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            },
            new CallOptions(cancellationToken: cts.Token));

        await responseStream.ResponseStream.ReadNIgnoreCancellation(1, cts.Token);
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.ErfassungElectionSupporter;
    }
}
