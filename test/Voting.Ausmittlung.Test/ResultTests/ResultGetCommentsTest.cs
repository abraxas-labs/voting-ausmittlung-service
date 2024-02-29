// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Net.Client;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultTests;

public class ResultGetCommentsTest : BaseTest<ResultService.ResultServiceClient>
{
    public ResultGetCommentsTest(TestApplicationFactory factory)
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
        await SeedComments();
        await PermissionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldWorkAsErfassungCreator()
    {
        var response = await ErfassungCreatorClient.GetResultCommentsAsync(new GetResultCommentsRequest
        {
            ResultId = VoteResultMockedData.GuidUzwilVoteInContestUzwilResult.ToString(),
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldWorkAsErfassungElectionAdmin()
    {
        var response = await ErfassungElectionAdminClient.GetResultCommentsAsync(new GetResultCommentsRequest
        {
            ResultId = VoteResultMockedData.GuidUzwilVoteInContestUzwilResult.ToString(),
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldWorkAsMonitoringElectionAdmin()
    {
        var response = await MonitoringElectionAdminClient.GetResultCommentsAsync(new GetResultCommentsRequest
        {
            ResultId = VoteResultMockedData.GuidUzwilVoteInContestUzwilResult.ToString(),
        });
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .ListPoliticalBusinessUnionsAsync(new ListPoliticalBusinessUnionsRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
            });
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    private async Task SeedComments()
    {
        await RunOnDb(async db =>
        {
            db.CountingCircleResultComments.AddRange(
                new CountingCircleResultComment
                {
                    Content = "my-comment-2",
                    Id = Guid.Parse("460cfd97-c750-4b03-a346-e5dbf0e4ae01"),
                    CreatedAt = new DateTime(2011, 01, 03, 10, 15, 12, DateTimeKind.Utc),
                    CreatedBy = new User
                    {
                        FirstName = "hans",
                        LastName = "muster",
                        SecureConnectId = "123",
                    },
                    ResultId = VoteResultMockedData.GuidUzwilVoteInContestUzwilResult,
                    CreatedByMonitoringAuthority = true,
                },
                new CountingCircleResultComment
                {
                    Content = "my-comment-1",
                    Id = Guid.Parse("6c92d642-9ee0-4b69-8834-9811246500a4"),
                    CreatedAt = new DateTime(2010, 01, 03, 10, 15, 12, DateTimeKind.Utc),
                    CreatedBy = new User
                    {
                        FirstName = "hans",
                        LastName = "muster",
                        SecureConnectId = "123",
                    },
                    ResultId = VoteResultMockedData.GuidUzwilVoteInContestUzwilResult,
                    CreatedByMonitoringAuthority = true,
                },
                new CountingCircleResultComment
                {
                    Content = "my-comment-2",
                    Id = Guid.Parse("c6be1c66-9d24-46ca-b5ea-7e9c833148e2"),
                    CreatedAt = new DateTime(2011, 01, 03, 10, 15, 12, DateTimeKind.Utc),
                    CreatedBy = new User
                    {
                        FirstName = "hans",
                        LastName = "muster",
                        SecureConnectId = "123",
                    },
                    ResultId = ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
                    CreatedByMonitoringAuthority = true,
                },
                new CountingCircleResultComment
                {
                    Content = "my-comment-1",
                    Id = Guid.Parse("40bb4042-271e-4e47-88b2-92cc00a82e88"),
                    CreatedAt = new DateTime(2010, 01, 03, 10, 15, 12, DateTimeKind.Utc),
                    CreatedBy = new User
                    {
                        FirstName = "hans",
                        LastName = "muster",
                        SecureConnectId = "123",
                    },
                    ResultId = ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
                    CreatedByMonitoringAuthority = true,
                },
                new CountingCircleResultComment
                {
                    Content = "my-comment-2",
                    Id = Guid.Parse("5e3e7645-605e-46cb-b341-c33297a0f792"),
                    CreatedAt = new DateTime(2011, 01, 03, 10, 15, 12, DateTimeKind.Utc),
                    CreatedBy = new User
                    {
                        FirstName = "hans",
                        LastName = "muster",
                        SecureConnectId = "123",
                    },
                    ResultId = MajorityElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
                    CreatedByMonitoringAuthority = true,
                },
                new CountingCircleResultComment
                {
                    Content = "my-comment-1",
                    Id = Guid.Parse("f3493d56-7207-455f-a619-39ff041029e0"),
                    CreatedAt = new DateTime(2010, 01, 03, 10, 15, 12, DateTimeKind.Utc),
                    CreatedBy = new User
                    {
                        FirstName = "hans",
                        LastName = "muster",
                        SecureConnectId = "123",
                    },
                    ResultId = MajorityElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
                    CreatedByMonitoringAuthority = true,
                });
            await db.SaveChangesAsync();
        });
    }
}
