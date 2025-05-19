// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.ContestTests;

public class ContestListSummariesTest : BaseTest<ContestService.ContestServiceClient>
{
    public ContestListSummariesTest(TestApplicationFactory factory)
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
        await PermissionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldFilterByState()
    {
        var response = await ErfassungElectionAdminClient.ListSummariesAsync(new ListContestSummariesRequest
        {
            States =
                {
                    ProtoModels.ContestState.Archived,
                    ProtoModels.ContestState.PastLocked,
                },
        });

        response.ContestSummaries_.Count(x => x.State == ProtoModels.ContestState.Archived).Should().Be(1);
        response.ContestSummaries_.Count(x => x.State == ProtoModels.ContestState.PastLocked).Should().Be(1);
        response.ContestSummaries_.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnParentAndOwnContests()
    {
        var response = await ErfassungElectionAdminClient.ListSummariesAsync(new ListContestSummariesRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsMonitoringAdminShouldReturnParentAndOwnContestsWithoutForeignPoliticalBusinesses()
    {
        var response = await MonitoringElectionAdminClient.ListSummariesAsync(new ListContestSummariesRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsErfassungAdminShouldReturnNoContestsWithoutAnyAccessiblePoliticalBusiness()
    {
        var contestId = ContestMockedData.GuidBundesurnengang;

        await ModifyDbEntities<Data.Models.SimpleCountingCircleResult>(
            x => x.CountingCircle!.SnapshotContestId == contestId,
            x => x.CountingCircleId = CountingCircleMockedData.GuidUzwilKirche);

        var response = await ErfassungElectionAdminClient.ListSummariesAsync(new ListContestSummariesRequest());
        var contest = response.ContestSummaries_.FirstOrDefault(s => s.Id == contestId.ToString());
        contest.Should().BeNull();
    }

    [Fact]
    public async Task TestAsMonitoringAdminShouldReturnContestsWithoutAnyOwnedPoliticalBusiness()
    {
        var contestId = ContestMockedData.GuidStGallenEvoting;

        await ModifyDbEntities<Data.Models.SimplePoliticalBusiness>(
            x => x.ContestId == contestId,
            x => x.DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGenf));

        // update the current user to the contest owner
        await ModifyDbEntities<Data.Models.DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            x => x.SecureConnectId = SecureConnectTestDefaults.MockedTenantGossau.Id);

        var response = await MonitoringElectionAdminClient.ListSummariesAsync(new ListContestSummariesRequest());
        var contest = response.ContestSummaries_.FirstOrDefault(s => s.Id == contestId.ToString());
        contest.Should().NotBeNull();
        contest!.ContestEntriesDetails.Should().BeEmpty();
    }

    [Fact]
    public async Task TestAsMonitoringAdminFilterByStateShouldNotReturnOwnedContestsWithInvalidState()
    {
        var contestId = ContestMockedData.GuidStGallenEvoting;

        await ModifyDbEntities<Data.Models.SimplePoliticalBusiness>(
            x => x.ContestId == contestId,
            x => x.DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGenf));

        // update the current user to the contest owner
        await ModifyDbEntities<Data.Models.DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            x => x.SecureConnectId = SecureConnectTestDefaults.MockedTenantGossau.Id);

        var response = await MonitoringElectionAdminClient.ListSummariesAsync(new ListContestSummariesRequest
        {
            States = { ProtoModels.ContestState.Active },
        });

        var contest = response.ContestSummaries_.FirstOrDefault(s => s.Id == contestId.ToString());
        contest.Should().BeNull();
    }

    [Fact]
    public async Task TestAsCreatorShouldReturnParentAndOwnContests()
    {
        var response = await ErfassungCreatorClient.ListSummariesAsync(new ListContestSummariesRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldOrderAscendingWithUpcomingContests()
    {
        var response = await ErfassungElectionAdminClient.ListSummariesAsync(new ListContestSummariesRequest
        {
            States =
            {
                ProtoModels.ContestState.TestingPhase,
                ProtoModels.ContestState.Active,
            },
        });

        var ascendingOrderedContests = response.ContestSummaries_.OrderBy(c => c.Date).ToList();
        ascendingOrderedContests.Any().Should().BeTrue();
        ascendingOrderedContests.SequenceEqual(response.ContestSummaries_).Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldOrderDescendingWithoutUpcomingContests()
    {
        var response = await ErfassungElectionAdminClient.ListSummariesAsync(new ListContestSummariesRequest
        {
            States =
            {
                ProtoModels.ContestState.PastUnlocked,
                ProtoModels.ContestState.PastLocked,
                ProtoModels.ContestState.Archived,
            },
        });
        var descendingOrderedContests = response.ContestSummaries_.OrderByDescending(c => c.Date).ToList();
        descendingOrderedContests.Any().Should().BeTrue();
        descendingOrderedContests.SequenceEqual(response.ContestSummaries_).Should().BeTrue();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .ListSummariesAsync(new ListContestSummariesRequest());
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }
}
