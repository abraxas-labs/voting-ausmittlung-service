// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultExportConfigurationTests;

public class ResultExportConfigurationsListTest : BaseTest<ExportService.ExportServiceClient>
{
    public ResultExportConfigurationsListTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ExportConfigurationMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldWork()
    {
        var configs = await StGallenMonitoringElectionAdminClient.ListResultExportConfigurationsAsync(NewValidRequest());

        foreach (var config in configs.Configurations)
        {
            var pbs = config.PoliticalBusinessIds.OrderBy(x => x).ToList();
            config.PoliticalBusinessIds.Clear();
            config.PoliticalBusinessIds.AddRange(pbs);
        }

        configs.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel).ListResultExportConfigurationsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private ListResultExportConfigurationsRequest NewValidRequest()
    {
        return new ListResultExportConfigurationsRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
        };
    }
}
