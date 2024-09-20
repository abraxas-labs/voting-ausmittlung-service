// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultImportGetMajorityElectionWriteInMappingsTest : BaseTest<ResultImportService.ResultImportServiceClient>
{
    public ResultImportGetMajorityElectionWriteInMappingsTest(TestApplicationFactory factory)
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
    public async Task ShouldWorkAsElectionAdmin()
    {
        var resp = await ErfassungElectionAdminClient.GetMajorityElectionWriteInMappingsAsync(new GetMajorityElectionWriteInMappingsRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
        });
        resp.ElectionWriteInMappings.Single(x => x.Election.BusinessType == ProtoModels.PoliticalBusinessType.MajorityElection).InvalidVotes.Should().BeFalse();
        CleanIds(resp);
        resp.ShouldMatchSnapshot();
    }

    [Fact]
    public async Task ShouldWorkAsElectionAdminWithMappingsAndInvalidVotes()
    {
        var id = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(Guid.Parse(ContestMockedData.IdStGallenEvoting), Guid.Parse(DomainOfInfluenceMockedData.IdUzwil));
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidStGallenEvoting,
            x => x.MajorityElectionInvalidVotes = true,
            true);

        var candidateResultId = await RunOnDb(db => db.MajorityElectionCandidateResults
            .Where(x => x.CandidateId == Guid.Parse(MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen))
            .Select(x => x.Id)
            .FirstAsync());
        await ModifyDbEntities(
            (MajorityElectionWriteInMapping _) => true,
            m =>
            {
                switch (m.WriteInCandidateName)
                {
                    case "vereinzelt":
                        m.Target = MajorityElectionWriteInMappingTarget.Individual;
                        break;
                    case "Hans Muster":
                        m.Target = MajorityElectionWriteInMappingTarget.Invalid;
                        break;
                    case "Hans Mueller":
                        m.Target = MajorityElectionWriteInMappingTarget.Candidate;
                        m.CandidateResultId = candidateResultId;
                        break;
                }
            });

        var resp = await ErfassungElectionAdminClient.GetMajorityElectionWriteInMappingsAsync(new GetMajorityElectionWriteInMappingsRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
        });
        resp.ElectionWriteInMappings
            .Single(x => x.Election.BusinessType == ProtoModels.PoliticalBusinessType.MajorityElection)
            .InvalidVotes
            .Should()
            .BeTrue();
        CleanIds(resp);
        resp.ShouldMatchSnapshot();
    }

    [Fact]
    public async Task ShouldWorkAsElectionAdminWithMappingsAndDisabledIndividualVotes()
    {
        var id = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(Guid.Parse(ContestMockedData.IdStGallenEvoting), Guid.Parse(DomainOfInfluenceMockedData.IdUzwil));
        await ModifyDbEntities<MajorityElection>(
            x => x.Id == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen),
            x => x.IndividualCandidatesDisabled = true,
            true);

        var resp = await ErfassungElectionAdminClient.GetMajorityElectionWriteInMappingsAsync(new GetMajorityElectionWriteInMappingsRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
        });
        resp.ElectionWriteInMappings
            .Single(x => x.Election.BusinessType == ProtoModels.PoliticalBusinessType.MajorityElection)
            .IndividualVotes
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task ShouldWorkAsContestManagerDuringTestingPhase()
    {
        var resp = await StGallenErfassungElectionAdminClient.GetMajorityElectionWriteInMappingsAsync(new GetMajorityElectionWriteInMappingsRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
        });
        resp.ElectionWriteInMappings.Single(x => x.Election.BusinessType == ProtoModels.PoliticalBusinessType.MajorityElection).InvalidVotes.Should().BeFalse();
        CleanIds(resp);
        resp.ShouldMatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.GetMajorityElectionWriteInMappingsAsync(
                new GetMajorityElectionWriteInMappingsRequest
                {
                    ContestId = ContestMockedData.IdStGallenEvoting,
                    CountingCircleId = CountingCircleMockedData.IdUzwil,
                }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        return AssertStatus(
            async () => await ErfassungElectionAdminClient.GetMajorityElectionWriteInMappingsAsync(
                new GetMajorityElectionWriteInMappingsRequest
                {
                    ContestId = ContestMockedData.IdStGallenEvoting,
                    CountingCircleId = CountingCircleMockedData.IdGossau,
                }),
            StatusCode.PermissionDenied,
            "does not belong to this tenant");
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultImportService.ResultImportServiceClient(channel)
            .GetMajorityElectionWriteInMappingsAsync(new GetMajorityElectionWriteInMappingsRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            });
    }

    private void CleanIds(ProtoModels.MajorityElectionContestWriteInMappings resp)
    {
        resp.ImportId.Should().NotBeEmpty();
        resp.ImportId = string.Empty;
        foreach (var mappingGroup in resp.ElectionWriteInMappings)
        {
            foreach (var mapping in mappingGroup.WriteInMappings)
            {
                mapping.Id.Should().NotBeEmpty();
                mapping.Id = string.Empty;
            }
        }
    }
}
