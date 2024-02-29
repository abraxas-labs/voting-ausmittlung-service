// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultImportDeleteImportedDataTest : BaseTest<ResultImportService.ResultImportServiceClient>
{
    public ResultImportDeleteImportedDataTest(TestApplicationFactory factory)
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
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);
        await ProportionalElectionResultBallotMockedData.Seed(RunScoped);
        await ProportionalElectionUnmodifiedListResultMockedData.Seed(RunScoped);
        await ResultImportMockedData.Seed(RunScoped);
        await PermissionMockedData.Seed(RunScoped);

        // activate e voting for all for easier testing
        // we deactivate it in some tests again to test the flag
        await ModifyDbEntities((ContestCountingCircleDetails _) => true, details => details.EVoting = true);
        await ModifyDbEntities((Contest _) => true, contest => contest.EVoting = true);
    }

    [Fact]
    public async Task ShouldWorkAsMonitoringAdmin()
    {
        await SetProportionalElectionResultState(
            ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil,
            CountingCircleResultState.CorrectionDone);

        AggregateRepositoryMock.Clear();

        await MonitoringElectionAdminClient.DeleteImportDataAsync(NewValidRequest());

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
        await TestEventsWithSignature(ContestMockedData.IdUzwilEvoting, async () =>
        {
            await SetProportionalElectionResultState(
                ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil,
                CountingCircleResultState.CorrectionDone);

            AggregateRepositoryMock.Clear();

            await MonitoringElectionAdminClient.DeleteImportDataAsync(NewValidRequest());

            return new[]
            {
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<ResultImportDataDeleted>(),
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultFlaggedForCorrection>(),
            };
        });
    }

    [Fact]
    public Task ShouldThrowOtherContest()
    {
        return AssertStatus(
            async () => await MonitoringElectionAdminClient.DeleteImportDataAsync(new DeleteResultImportDataRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ContestEVotingDisabledShouldThrow()
    {
        await ModifyDbEntities<Contest>(
            x => x.Id == Guid.Parse(ContestMockedData.IdUzwilEvoting),
            x => x.EVoting = false);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.DeleteImportDataAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "eVoting is not active on the Contest with the id cc70fe43-8f4e-4bc6-a461-b808907bc996");
    }

    [Fact]
    public async Task ShouldThrowCountingCirclesAudited()
    {
        await SetProportionalElectionResultState(
            ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil,
            CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.DeleteImportDataAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            $"A result is in an invalid state for an eVoting import to be possible ({ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil})");
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        await EVotingMockedData.Seed(RunScoped, CreateHttpClient);

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
                    .Where(x => x.Channel == VotingChannel.ByMail && x.Valid)
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

        await AssertMajorityElectionEVotingZero();
        await AssertProportionalElectionEVotingZero();
        await AssertVoteEVotingZero();
        await AssertEVotingVotingCardsZero();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultImportService.ResultImportServiceClient(channel)
            .DeleteImportDataAsync(NewValidRequest());
    }

    protected override GrpcChannel CreateGrpcChannel(
        bool authorize = true,
        string? tenant = "000000000000000000",
        string? userId = "default-user-id",
        params string[] roles)
        => base.CreateGrpcChannel(authorize, SecureConnectTestDefaults.MockedTenantUzwil.Id, userId, roles);

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private DeleteResultImportDataRequest NewValidRequest()
    {
        return new DeleteResultImportDataRequest { ContestId = ContestMockedData.IdUzwilEvoting };
    }

    private async Task SetProportionalElectionResultState(string resultIdStr, CountingCircleResultState state)
    {
        TrySetFakeAuth(SecureConnectTestDefaults.MockedTenantStGallen.Id, RolesMockedData.MonitoringElectionAdmin);
        var id = Guid.Parse(resultIdStr);
        await ModifyDbEntities(
            (ProportionalElectionResult result) =>
                result.Id == id,
            result => result.State = state);

        var contestId = (await RunOnDb(db => db.ProportionalElectionResults
            .Include(r => r.ProportionalElection)
            .FirstAsync(r => r.Id == id))).ProportionalElection.ContestId;

        var resultAgg = await AggregateRepositoryMock.GetById<ProportionalElectionResultAggregate>(id);
        switch (state)
        {
            case CountingCircleResultState.SubmissionDone:
                resultAgg.SubmissionFinished(contestId);
                break;
            case CountingCircleResultState.CorrectionDone:
                resultAgg.SubmissionFinished(contestId);
                resultAgg.FlagForCorrection(contestId);
                resultAgg.CorrectionFinished(string.Empty, contestId);
                break;
            case CountingCircleResultState.AuditedTentatively:
                resultAgg.SubmissionFinished(contestId);
                resultAgg.AuditedTentatively(contestId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        await AggregateRepositoryMock.Save(resultAgg);
    }

    private async Task AssertProportionalElectionEVotingZero()
    {
        var proportionalElection = await RunOnDb(
            db => db.ProportionalElections
                .AsSplitQuery()
                .Include(x => x.EndResult!)
                .ThenInclude(x => x.ListEndResults)
                .ThenInclude(x => x.CandidateEndResults)
                .Include(x => x.Results)
                .ThenInclude(x => x.ListResults)
                .ThenInclude(x => x.CandidateResults)
                .FirstAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen)),
            Languages.German);
        proportionalElection.EndResult!.CountOfVoters.EVotingReceivedBallots.Should().Be(0);
        proportionalElection.EndResult!.EVotingSubTotal.TotalCountOfBlankRowsOnListsWithoutParty.Should().Be(0);
        proportionalElection.EndResult!.EVotingSubTotal.TotalCountOfLists.Should().Be(0);
        foreach (var listResult in proportionalElection.EndResult!.ListEndResults)
        {
            listResult.EVotingSubTotal.TotalVoteCount.Should().Be(0);

            foreach (var candidateEndResult in listResult.CandidateEndResults)
            {
                candidateEndResult.EVotingSubTotal.VoteCount.Should().Be(0);
            }
        }

        foreach (var result in proportionalElection.Results)
        {
            result.CountOfVoters.EVotingReceivedBallots.Should().Be(0);
            result.EVotingSubTotal.TotalCountOfBlankRowsOnListsWithoutParty.Should().Be(0);
            result.EVotingSubTotal.TotalCountOfLists.Should().Be(0);
            foreach (var listResult in result.ListResults)
            {
                listResult.EVotingSubTotal.TotalVoteCount.Should().Be(0);

                foreach (var candidateResult in listResult.CandidateResults)
                {
                    candidateResult.EVotingSubTotal.VoteCount.Should().Be(0);
                }
            }
        }
    }

    private async Task AssertMajorityElectionEVotingZero()
    {
        var majorityElection = await RunOnDb(
            db => db.MajorityElections
                .AsSplitQuery()
                .Include(x => x.EndResult!)
                .ThenInclude(x => x.CandidateEndResults)
                .Include(x => x.Results)
                .ThenInclude(x => x.CandidateResults)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen)),
            Languages.German);
        majorityElection.EndResult!.CountOfVoters.EVotingReceivedBallots.Should().Be(0);
        majorityElection.EndResult!.EVotingSubTotal.TotalCandidateVoteCountInclIndividual.Should().Be(0);
        majorityElection.EndResult!.EVotingSubTotal.InvalidVoteCount.Should().Be(0);
        majorityElection.EndResult!.EVotingSubTotal.EmptyVoteCountInclWriteIns.Should().Be(0);
        foreach (var candidateResult in majorityElection.EndResult!.CandidateEndResults)
        {
            candidateResult.EVotingVoteCount.Should().Be(0);
        }

        foreach (var result in majorityElection.Results)
        {
            result.CountOfVoters.EVotingReceivedBallots.Should().Be(0);
            result.EVotingSubTotal.TotalCandidateVoteCountInclIndividual.Should().Be(0);
            result.EVotingSubTotal.InvalidVoteCount.Should().Be(0);
            result.EVotingSubTotal.EmptyVoteCountInclWriteIns.Should().Be(0);
            foreach (var candidateResult in result.CandidateResults)
            {
                candidateResult.EVotingInclWriteInsVoteCount.Should().Be(0);
            }
        }
    }

    private async Task AssertVoteEVotingZero()
    {
        var vote = await RunOnDb(
            db => db.Votes
                .AsSplitQuery()
                .Include(x => x.EndResult!)
                .ThenInclude(x => x.BallotEndResults)
                .ThenInclude(x => x.QuestionEndResults)
                .Include(x => x.EndResult!)
                .ThenInclude(x => x.BallotEndResults)
                .ThenInclude(x => x.TieBreakQuestionEndResults)
                .Include(x => x.Results)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.QuestionResults)
                .Include(x => x.Results)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.TieBreakQuestionResults)
                .FirstAsync(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)),
            Languages.German);
        foreach (var ballotEndResult in vote.EndResult!.BallotEndResults)
        {
            ballotEndResult.CountOfVoters.EVotingReceivedBallots.Should().Be(0);
            foreach (var questionEndResult in ballotEndResult.QuestionEndResults)
            {
                questionEndResult.EVotingSubTotal.TotalCountOfAnswerYes.Should().Be(0);
                questionEndResult.EVotingSubTotal.TotalCountOfAnswerNo.Should().Be(0);
                questionEndResult.EVotingSubTotal.TotalCountOfAnswerUnspecified.Should().Be(0);
            }

            foreach (var questionEndResult in ballotEndResult.TieBreakQuestionEndResults)
            {
                questionEndResult.EVotingSubTotal.TotalCountOfAnswerQ1.Should().Be(0);
                questionEndResult.EVotingSubTotal.TotalCountOfAnswerQ2.Should().Be(0);
                questionEndResult.EVotingSubTotal.TotalCountOfAnswerUnspecified.Should().Be(0);
            }
        }

        foreach (var result in vote.Results)
        {
            foreach (var ballotResult in result.Results)
            {
                ballotResult.CountOfVoters.EVotingReceivedBallots.Should().Be(0);
                foreach (var questionResult in ballotResult.QuestionResults)
                {
                    questionResult.EVotingSubTotal.TotalCountOfAnswerYes.Should().Be(0);
                    questionResult.EVotingSubTotal.TotalCountOfAnswerNo.Should().Be(0);
                    questionResult.EVotingSubTotal.TotalCountOfAnswerUnspecified.Should().Be(0);
                }

                foreach (var questionResult in ballotResult.TieBreakQuestionResults)
                {
                    questionResult.EVotingSubTotal.TotalCountOfAnswerQ1.Should().Be(0);
                    questionResult.EVotingSubTotal.TotalCountOfAnswerQ2.Should().Be(0);
                    questionResult.EVotingSubTotal.TotalCountOfAnswerUnspecified.Should().Be(0);
                }
            }
        }
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
