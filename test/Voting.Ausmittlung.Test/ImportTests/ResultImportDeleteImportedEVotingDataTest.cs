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
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultImportDeleteImportedEVotingDataTest : ResultImportDeleteImportedDataBaseTest
{
    public ResultImportDeleteImportedEVotingDataTest(TestApplicationFactory factory)
        : base(VotingDataSource.EVoting, factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ResultImportEVotingMockedData.Seed(RunScoped);

        // activate e voting for all for easier testing
        // we deactivate it in some tests again to test the flag
        await ModifyDbEntities((ContestCountingCircleDetails _) => true, details => details.EVoting = true);
        await ModifyDbEntities((CountingCircle _) => true, details => details.EVoting = true);
        await ModifyDbEntities((Contest _) => true, contest => contest.EVoting = true);

        await ResultImportEVotingMockedData.SeedUzwilAggregates(RunScoped);

        EventPublisherMock.Clear();
    }

    [Fact]
    public async Task ShouldWorkAsMonitoringAdmin()
    {
        await SetProportionalElectionResultState(
            ContestMockedData.GuidUzwilEvoting,
            Guid.Parse(ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil),
            CountingCircleResultState.CorrectionDone);
        EventPublisherMock.Clear();

        await MonitoringElectionAdminClient.DeleteEVotingImportDataAsync(NewValidRequest());

        var ev = EventPublisherMock.GetSinglePublishedEvent<ResultImportDataDeleted>();
        ev.ImportId = string.Empty;
        ev.MatchSnapshot();

        EventPublisherMock.GetPublishedEvents<MajorityElectionResultFlaggedForCorrection>()
            .Should()
            .BeEmpty();
        EventPublisherMock.GetPublishedEvents<VoteResultFlaggedForCorrection>()
            .Should()
            .BeEmpty();
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultFlaggedForCorrection>()
            .ElectionResultId
            .Should()
            .Be(ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil);
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdUzwilEVoting, async () =>
        {
            await SetProportionalElectionResultState(
                ContestMockedData.GuidUzwilEvoting,
                Guid.Parse(ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil),
                CountingCircleResultState.CorrectionDone);
            EventPublisherMock.Clear();

            await MonitoringElectionAdminClient.DeleteEVotingImportDataAsync(NewValidRequest());

            return
            [
                EventPublisherMock.GetSinglePublishedEventWithMetadata<ResultImportDataDeleted>(),
                EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultFlaggedForCorrection>()
            ];
        });
    }

    [Fact]
    public Task ShouldThrowOtherContest()
    {
        return AssertStatus(
            async () => await MonitoringElectionAdminClient.DeleteEVotingImportDataAsync(new DeleteEVotingResultImportDataRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldThrowDeleteTwice()
    {
        var req = NewValidRequest();
        await MonitoringElectionAdminClient.DeleteEVotingImportDataAsync(req);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.DeleteEVotingImportDataAsync(req),
            StatusCode.InvalidArgument,
            "Cannot delete since no results are currently imported");
    }

    [Fact]
    public async Task ContestEVotingDisabledShouldThrow()
    {
        await ModifyDbEntities<Contest>(
            x => x.Id == Guid.Parse(ContestMockedData.IdUzwilEVoting),
            x => x.EVoting = false);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.DeleteEVotingImportDataAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "eVoting is not active on the Contest with the id cc70fe43-8f4e-4bc6-a461-b808907bc996");
    }

    [Fact]
    public async Task CountingCirclesAuditedAfterTestingPhaseEndedShouldThrow()
    {
        await SetContestState(ContestMockedData.IdUzwilEVoting, ContestState.Active);
        await SetProportionalElectionResultState(
            ContestMockedData.GuidUzwilEvoting,
            Guid.Parse(ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil),
            CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.DeleteEVotingImportDataAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            $"A result is in an invalid state for an import to be possible ({ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil})");
    }

    [Fact]
    public async Task CountingCircleResultAuditedTentativelyInTestingPhaseShouldWork()
    {
        await SetProportionalElectionResultState(
            ContestMockedData.GuidStGallenEvoting,
            Guid.Parse(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen),
            CountingCircleResultState.AuditedTentatively);

        await MonitoringElectionAdminClient.DeleteEVotingImportDataAsync(NewValidRequest());
        EventPublisherMock.GetPublishedEvents<ResultImportDataDeleted>().Should().HaveCount(1);
    }

    [Fact]
    public async Task CountingCircleResultPlausibilisedInTestingPhaseShouldWork()
    {
        await SetProportionalElectionResultState(
            ContestMockedData.GuidStGallenEvoting,
            Guid.Parse(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen),
            CountingCircleResultState.Plausibilised);

        await MonitoringElectionAdminClient.DeleteEVotingImportDataAsync(NewValidRequest());
        EventPublisherMock.GetPublishedEvents<ResultImportDataDeleted>().Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        await ResultImportMockedData.SeedEVoting(RunScoped, CreateHttpClient);

        // Add some voting cards, so we can verify that only the e-voting voting cards will be deleted
        await RunOnDb(async db =>
        {
            var ccDetails = await db.ContestCountingCircleDetails
                .AsSplitQuery()
                .AsTracking()
                .Include(x => x.VotingCards)
                .Where(x => x.ContestId == Guid.Parse(ContestMockedData.IdStGallenEvoting))
                .ToListAsync();

            foreach (var ccDetail in ccDetails)
            {
                var byMailVotingCards = ccDetail.VotingCards
                    .Where(x => x is { Channel: VotingChannel.ByMail, Valid: true })
                    .ToList();

                // Add an e-voting voting card for each DOI type
                foreach (var byMailVotingCard in byMailVotingCards)
                {
                    ccDetail.VotingCards.Add(new VotingCardResultDetail
                    {
                        Channel = VotingChannel.EVoting,
                        Valid = true,
                        CountOfReceivedVotingCards = 234,
                        DomainOfInfluenceType = byMailVotingCard.DomainOfInfluenceType,
                    });
                }
            }

            await db.SaveChangesAsync();
        });

        var id = "3b29fd77-3cb2-4b34-b490-442d248ddd13";
        await TestEventPublisher.Publish(
            0,
            new ResultImportDataDeleted
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                ImportId = id,
                EventInfo = GetMockedEventInfo(),
            });

        var import = await RunOnDb(db => db.ResultImports.FirstAsync(x => x.Id == Guid.Parse(id)));
        import.Deleted.Should().BeTrue();
        import.MatchSnapshot("import");

        await AssertProportionalElectionResultZero(Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen));
        await AssertMajorityElectionResultZero(Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen));
        await AssertVoteResultZero(Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen));
        await AssertEVotingVotingCardsZero();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await SetProportionalElectionResultState(
            ContestMockedData.GuidUzwilEvoting,
            Guid.Parse(ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil),
            CountingCircleResultState.CorrectionDone);

        await new ResultImportService.ResultImportServiceClient(channel)
            .DeleteEVotingImportDataAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private DeleteEVotingResultImportDataRequest NewValidRequest()
    {
        return new DeleteEVotingResultImportDataRequest { ContestId = ContestMockedData.IdUzwilEVoting };
    }

    private async Task AssertEVotingVotingCardsZero()
    {
        var ccDetails = await RunOnDb(
            db => db.ContestCountingCircleDetails
                .AsSplitQuery()
                .Include(x => x.VotingCards)
                .Where(x => x.ContestId == Guid.Parse(ContestMockedData.IdStGallenEvoting))
                .ToListAsync());

        var votingCards = ccDetails.SelectMany(x => x.VotingCards).ToList();
        votingCards.Count(vc => vc.Channel != VotingChannel.EVoting && vc.CountOfReceivedVotingCards > 0).Should().BeGreaterThan(0);
        votingCards
            .Where(vc => vc.Channel == VotingChannel.EVoting)
            .All(vc => vc.CountOfReceivedVotingCards == 0)
            .Should()
            .BeTrue();
    }
}
