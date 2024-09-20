// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public abstract class ProportionalElectionDoubleProportionalResultBaseTest
    : BaseTest<ProportionalElectionResultService.ProportionalElectionResultServiceClient>
{
    protected ProportionalElectionDoubleProportionalResultBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ZhMockedData.Seed(RunScoped);
    }

    protected Task CreateDpResult(Guid electionId)
        => RunScoped<DoubleProportionalResultBuilder>(builder => builder.BuildForElection(electionId));

    protected Task DeleteDpResult(Guid electionId)
        => RunScoped<DoubleProportionalResultBuilder>(builder => builder.ResetForElection(electionId));

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantBund.Id, TestDefaults.UserId, roles);
}
