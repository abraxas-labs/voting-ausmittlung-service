// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ResultImportType = Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultImportDeleteImportedECountingDataTest : ResultImportDeleteImportedDataBaseTest
{
    public ResultImportDeleteImportedECountingDataTest(TestApplicationFactory factory)
        : base(VotingDataSource.ECounting, factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ResultImportECountingMockedData.Seed(RunScoped);
        await ResultImportECountingMockedData.SeedUzwilAggregates(RunScoped);

        // start submission and set result states
        await new ResultService.ResultServiceClient(CreateGrpcChannel(RolesMockedData.ErfassungElectionAdmin))
            .GetListAsync(new GetResultListRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            });

        EventPublisherMock.Clear();
    }

    [Fact]
    public async Task ShouldWorkAsElectionAdmin()
    {
        await ErfassungElectionAdminClient.DeleteECountingImportDataAsync(NewValidRequest());

        var ev = EventPublisherMock.GetSinglePublishedEvent<ResultImportDataDeleted>();
        ev.ImportId = string.Empty;
        ev.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldThrowDeleteTwice()
    {
        var req = NewValidRequest();
        await ErfassungElectionAdminClient.DeleteECountingImportDataAsync(req);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DeleteECountingImportDataAsync(req),
            StatusCode.InvalidArgument,
            "Cannot delete since no results are currently imported");
    }

    [Fact]
    public async Task InCorrectionShouldWork()
    {
        await SetVoteResultState(
            ContestMockedData.GuidStGallenEvoting,
            VoteResultMockedData.GuidUzwilVoteInContestStGallenResult,
            CountingCircleResultState.ReadyForCorrection);

        await ErfassungElectionAdminClient.DeleteECountingImportDataAsync(NewValidRequest());

        await AssertVoteResultZero(Guid.Parse(VoteMockedData.IdUzwilVoteInContestStGallen));
    }

    [Fact]
    public async Task SubmissionDoneShouldThrow()
    {
        await SetVoteResultState(
            ContestMockedData.GuidStGallenEvoting,
            VoteResultMockedData.GuidUzwilVoteInContestStGallenResult,
            CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DeleteECountingImportDataAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            $"A result is in an invalid state for an import to be possible ({VoteResultMockedData.GuidUzwilVoteInContestStGallenResult})");
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        await ResultImportMockedData.SeedECounting(RunScoped, CreateHttpClient);

        var id = "759b344f-511a-41f6-8836-43870949e52c";
        await TestEventPublisher.Publish(
            0,
            new ResultImportDataDeleted
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
                ImportType = ResultImportType.Ecounting,
                ImportId = id,
                EventInfo = GetMockedEventInfo(),
            });

        var import = await RunOnDb(db => db.ResultImports.FirstAsync(x => x.Id == Guid.Parse(id)));
        import.Deleted.Should().BeTrue();
        import.MatchSnapshot();

        await AssertProportionalElectionResultZero(Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestStGallen));
        await AssertMajorityElectionResultZero(Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen));
        await AssertVoteResultZero(Guid.Parse(VoteMockedData.IdUzwilVoteInContestStGallen));
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultImportService.ResultImportServiceClient(channel)
            .DeleteECountingImportDataAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private DeleteECountingResultImportDataRequest NewValidRequest()
    {
        return new DeleteECountingResultImportDataRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
        };
    }
}
