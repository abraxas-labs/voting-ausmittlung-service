// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Voting.Ausmittlung.Core.Auth;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Ausmittlung.Test.ContestCountingCircleContactPersonTests;

public abstract class ContestCountingCircleContactPersonBaseTest : BaseTest<ContestCountingCircleContactPersonService.ContestCountingCircleContactPersonServiceClient>
{
    protected ContestCountingCircleContactPersonBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected ContestCountingCircleContactPersonService.ContestCountingCircleContactPersonServiceClient BundErfassungElectionAdminClient { get; private set; } = null!; // initialized during InitializeAsync

    public override async Task InitializeAsync()
    {
        BundErfassungElectionAdminClient = new ContestCountingCircleContactPersonService.ContestCountingCircleContactPersonServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantBund.Id, TestDefaults.UserId, RolesMockedData.ErfassungElectionAdmin));
        await base.InitializeAsync();
    }
}
