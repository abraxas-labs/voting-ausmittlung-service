// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Ausmittlung.Test.ProportionalElectionUnionResultTests;

public abstract class ProportionalElectionUnionResultBaseTest : BaseTest<ProportionalElectionUnionResultService.ProportionalElectionUnionResultServiceClient>
{
    protected ProportionalElectionUnionResultBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ZhMockedData.Seed(RunScoped);
    }

    protected async Task FinalizeUnion(Guid unionId)
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionUnionEndResultFinalized
            {
                ProportionalElectionUnionId = unionId.ToString(),
                ProportionalElectionUnionEndResultId = AusmittlungUuidV5.BuildPoliticalBusinessUnionEndResult(unionId, false).ToString(),
                EventInfo = new EventInfo
                {
                    Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                    User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                    Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                },
            });
    }

    protected async Task RevertFinalizeUnion(Guid unionId)
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionUnionEndResultFinalizationReverted
            {
                ProportionalElectionUnionId = unionId.ToString(),
                ProportionalElectionUnionEndResultId = AusmittlungUuidV5.BuildPoliticalBusinessUnionEndResult(unionId, false).ToString(),
                EventInfo = new EventInfo
                {
                    Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                    User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                    Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                },
            });
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantBund.Id, TestDefaults.UserId, roles);
}
