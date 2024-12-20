﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionEndResultGetAvailableLotDecisionsTest : ProportionalElectionEndResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    private bool _initializedAuthorizationTest;

    public ProportionalElectionEndResultGetAvailableLotDecisionsTest(TestApplicationFactory factory)
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
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await TriggerMandateDistribution();
        var endResult = await MonitoringElectionAdminClient.GetListEndResultAvailableLotDecisionsAsync(NewValidRequest());
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowIfElectionMandateDistributionNotStarted()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetListEndResultAvailableLotDecisionsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "lot decisions are not allowed on this end result");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetListEndResultAvailableLotDecisionsAsync(
                new GetProportionalElectionListEndResultAvailableLotDecisionsRequest
                {
                    ProportionalElectionListId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetListEndResultAvailableLotDecisionsAsync(
                new GetProportionalElectionListEndResultAvailableLotDecisionsRequest
                {
                    ProportionalElectionListId = IdBadFormat,
                }),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (!_initializedAuthorizationTest)
        {
            _initializedAuthorizationTest = true;
            await TriggerMandateDistribution();
        }

        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .GetListEndResultAvailableLotDecisionsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    private GetProportionalElectionListEndResultAvailableLotDecisionsRequest NewValidRequest()
    {
        return new GetProportionalElectionListEndResultAvailableLotDecisionsRequest
        {
            ProportionalElectionListId = ProportionalElectionEndResultMockedData.ListId1,
        };
    }
}
