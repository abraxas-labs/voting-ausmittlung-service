// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ResultImportType = Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultImportDeleteImportedECountingPoliticalBusinessDataTest : ResultImportDeleteImportedDataBaseTest
{
    public ResultImportDeleteImportedECountingPoliticalBusinessDataTest(TestApplicationFactory factory)
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
        await ErfassungElectionAdminClient.DeleteECountingPoliticalBusinessImportDataAsync(NewValidRequest());

        var ev = EventPublisherMock.GetSinglePublishedEvent<ResultImportPoliticalBusinessDataDeleted>();
        ev.ImportId = string.Empty;
        ev.MatchSnapshot();
    }

    [Fact]
    public async Task InCorrectionShouldWork()
    {
        await SetVoteResultState(
            ContestMockedData.GuidStGallenEvoting,
            VoteResultMockedData.GuidUzwilVoteInContestStGallenResult,
            CountingCircleResultState.ReadyForCorrection);

        await ErfassungElectionAdminClient.DeleteECountingPoliticalBusinessImportDataAsync(NewValidRequest());

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
            async () => await ErfassungElectionAdminClient.DeleteECountingPoliticalBusinessImportDataAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            $"A result is in an invalid state for an import to be possible ({VoteResultMockedData.GuidUzwilVoteInContestStGallenResult})");
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        var eventCounter = await ResultImportMockedData.SeedECounting(RunScoped, CreateHttpClient);

        var pbId = Guid.Parse(VoteMockedData.IdUzwilVoteInContestStGallen);
        var id = "759b344f-511a-41f6-8836-43870949e52c";
        await TestEventPublisher.Publish(
            eventCounter,
            new ResultImportPoliticalBusinessDataDeleted
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
                ImportType = ResultImportType.Ecounting,
                ImportId = id,
                EventInfo = GetMockedEventInfo(),
                PoliticalBusinessId = pbId.ToString(),
            });

        var import = await RunOnDb(db => db.ResultImports.Include(i => i.ImportedPoliticalBusinesses).FirstAsync(x => x.Id == Guid.Parse(id)));
        import.Deleted.Should().BeTrue();
        import.ImportedPoliticalBusinesses.Any(ipb => ipb.PoliticalBusinessId == pbId).Should().BeTrue();
        import.ImportedPoliticalBusinesses.Count.Should().Be(1);
        import.ImportedPoliticalBusinesses = null!;

        import.MatchSnapshot();

        // TODO: Test ECountingIMportedReset
        await AssertVoteResultZero(Guid.Parse(VoteMockedData.IdUzwilVoteInContestStGallen));
    }

    [Fact]
    public async Task ProcessorWithUnimportedSecondaryElectionShouldWork()
    {
        var eventCounter = await ResultImportMockedData.SeedECounting(RunScoped, CreateHttpClient);

        var id = "759b344f-511a-41f6-8836-43870949e52c";
        var electionId = Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen);
        var ccId = Guid.Parse(CountingCircleMockedData.IdUzwil);
        var resultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(electionId, ccId, false);

        await ModifyDbEntities<SimpleCountingCircleResult>(
            r => r.Id == resultId,
            r => r.ECountingImported = true);

        await ModifyDbEntities<MajorityElectionResult>(
            r => r.Id == resultId,
            r => r.ECountingSubTotal.IndividualVoteCount = 1);

        await TestEventPublisher.Publish(
            eventCounter,
            new ResultImportPoliticalBusinessDataDeleted
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = ccId.ToString(),
                ImportType = ResultImportType.Ecounting,
                ImportId = id,
                EventInfo = GetMockedEventInfo(),
                PoliticalBusinessId = electionId.ToString(),
            });

        var import = await RunOnDb(db => db.ResultImports.Include(i => i.ImportedPoliticalBusinesses).FirstAsync(x => x.Id == Guid.Parse(id)));
        import.Deleted.Should().BeTrue();
        import.ImportedPoliticalBusinesses.Any(ipb => ipb.PoliticalBusinessId == electionId).Should().BeTrue();
        import.ImportedPoliticalBusinesses.Count.Should().Be(1);

        await AssertMajorityElectionResultZero(electionId);
    }

    [Fact]
    public async Task ProcessorWithImportedSecondaryElectionShouldWork()
    {
        var eventCounter = await ResultImportMockedData.SeedECounting(RunScoped, CreateHttpClient);

        var id = "759b344f-511a-41f6-8836-43870949e52c";
        var electionId = Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen);
        var ccId = Guid.Parse(CountingCircleMockedData.IdUzwil);
        var secondaryElectionId = Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdUzwilMajorityElectionInContestStGallen);

        var resultIds = new[]
        {
            AusmittlungUuidV5.BuildPoliticalBusinessResult(electionId, ccId, false),
            AusmittlungUuidV5.BuildPoliticalBusinessResult(secondaryElectionId, ccId, false),
        };

        await ModifyDbEntities<SimpleCountingCircleResult>(
            r => resultIds.Contains(r.Id),
            r => r.ECountingImported = true);

        await ModifyDbEntities<MajorityElectionResult>(
            r => r.Id == resultIds[0],
            r => r.ECountingSubTotal.IndividualVoteCount = 1);

        await ModifyDbEntities<SecondaryMajorityElectionResult>(
            r => r.Id == resultIds[1],
            r => r.ECountingSubTotal.IndividualVoteCount = 1);

        await TestEventPublisher.Publish(
            eventCounter,
            new ResultImportPoliticalBusinessDataDeleted
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = ccId.ToString(),
                ImportType = ResultImportType.Ecounting,
                ImportId = id,
                EventInfo = GetMockedEventInfo(),
                PoliticalBusinessId = electionId.ToString(),
            });

        var import = await RunOnDb(db => db.ResultImports.Include(i => i.ImportedPoliticalBusinesses).FirstAsync(x => x.Id == Guid.Parse(id)));
        import.Deleted.Should().BeTrue();
        import.ImportedPoliticalBusinesses.Any(ipb => ipb.PoliticalBusinessId == electionId).Should().BeTrue();

        import.ImportedPoliticalBusinesses.Any(ipb => ipb.PoliticalBusinessId == secondaryElectionId).Should().BeTrue();
        import.ImportedPoliticalBusinesses.Count.Should().Be(2);
        import.ImportedPoliticalBusinesses = null!;

        await AssertMajorityElectionResultZero(electionId);
        await AssertSecondaryMajorityElectionResultZero(secondaryElectionId);

        var simpleResultsWithoutECountingImportedCount = await RunOnDb(db => db.SimpleCountingCircleResults
            .Where(r => resultIds.Contains(r.Id) && !r.ECountingImported)
            .CountAsync());

        simpleResultsWithoutECountingImportedCount.Should().Be(resultIds.Length);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultImportService.ResultImportServiceClient(channel)
            .DeleteECountingPoliticalBusinessImportDataAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private DeleteECountingResultPoliticalBusinessImportDataRequest NewValidRequest()
    {
        return new DeleteECountingResultPoliticalBusinessImportDataRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
            PoliticalBusinessId = VoteMockedData.IdUzwilVoteInContestStGallen,
        };
    }
}
