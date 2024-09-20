// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Net.Client;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionEndResultBuildTest : ProportionalElectionEndResultBaseTest
{
    public ProportionalElectionEndResultBuildTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestBuildHagenbach()
    {
        await SeedElectionAndFinishSubmissions();

        var initEndResult = await GetEndResult();
        initEndResult.MatchSnapshot("init");

        await SetOneAuditedTentatively();

        var afterOneAuditedEndResult = await GetEndResult();
        afterOneAuditedEndResult.MatchSnapshot("afterOneAuditedEndResult");

        await SetOtherAuditedTentatively();
        await TriggerMandateDistribution();

        var afterAllAuditedEndResults = await GetEndResult();
        afterAllAuditedEndResults.MatchSnapshot("afterAllAuditedEndResultsAndMandateDistributionTriggered");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .GetEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        // Skip this test, tested in another class
        yield break;
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        // Skip this test, tested in another class
        yield break;
    }

    private GetProportionalElectionEndResultRequest NewValidRequest()
    {
        return new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
        };
    }

    private async Task<ProtoModels.ProportionalElectionEndResult> GetEndResult()
    {
        return await MonitoringElectionAdminClient.GetEndResultAsync(NewValidRequest());
    }
}
