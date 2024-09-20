// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionUnionResultTests;

public class ProportionalElectionUnionPartialEndResultGetTest : ProportionalElectionUnionResultBaseTest
{
    public ProportionalElectionUnionPartialEndResultGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunOnDb(db => db.DomainOfInfluences
            .Where(doi => doi.BasisDomainOfInfluenceId == ZhMockedData.DomainOfInfluenceGuidMeilen)
            .ExecuteUpdateAsync(setters => setters.SetProperty(doi => doi.ViewCountingCirclePartialResults, true)));
    }

    [Fact]
    public async Task TestShouldWork()
    {
        var endResult = await MonitoringElectionAdminClient.GetPartialEndResultAsync(NewValidRequest());

        foreach (var candidate in endResult.ProportionalElectionEndResults
            .SelectMany(x => x.ListEndResults)
            .SelectMany(x => x.CandidateEndResults))
        {
            candidate.Candidate.Id = "removed"; // not consistent in each test run
        }

        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowWithoutFlag()
    {
        await RunOnDb(db => db.DomainOfInfluences
            .Where(doi => doi.BasisDomainOfInfluenceId == ZhMockedData.DomainOfInfluenceGuidMeilen)
            .ExecuteUpdateAsync(setters => setters.SetProperty(doi => doi.ViewCountingCirclePartialResults, false)));

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetPartialEndResultAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionUnionResultService.ProportionalElectionUnionResultServiceClient(channel)
            .GetPartialEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    private GetProportionalElectionUnionPartialEndResultRequest NewValidRequest()
    {
        return new() { ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat };
    }
}
