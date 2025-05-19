// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultResetTest : BaseIntegrationTest
{
    private static readonly Guid ResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(ProportionalElectionMockedData.IdBundProportionalElectionInContestBund),
        CountingCircleMockedData.GuidBund,
        false);

    public ProportionalElectionResultResetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);

        await ModifyDbEntities<ProportionalElectionResult>(
            r => r.Id == ResultId,
            r =>
            {
                r.State = CountingCircleResultState.SubmissionDone;
                r.CountOfVoters = new()
                {
                    ConventionalSubTotal = new PoliticalBusinessCountOfVotersNullableSubTotal
                    {
                        AccountedBallots = 200,
                        InvalidBallots = 30,
                        ReceivedBallots = 500,
                    },
                    EVotingSubTotal = new PoliticalBusinessCountOfVotersSubTotal
                    {
                        ReceivedBallots = 5,
                        AccountedBallots = 5,
                    },
                    ECountingSubTotal = new PoliticalBusinessCountOfVotersSubTotal
                    {
                        ReceivedBallots = 6,
                        AccountedBallots = 7,
                    },
                    VoterParticipation = 0.5M,
                };
                r.CountOfBundlesNotReviewedOrDeleted = 2;
                r.ConventionalSubTotal = new()
                {
                    TotalCountOfBlankRowsOnListsWithoutParty = 30,
                    TotalCountOfListsWithoutParty = 15,
                    TotalCountOfModifiedLists = 10,
                    TotalCountOfUnmodifiedLists = 5,
                };
                r.EVotingSubTotal = new()
                {
                    TotalCountOfBlankRowsOnListsWithoutParty = 5,
                    TotalCountOfListsWithoutParty = 4,
                    TotalCountOfModifiedLists = 1,
                    TotalCountOfUnmodifiedLists = 2,
                };
                r.ECountingSubTotal = new()
                {
                    TotalCountOfBlankRowsOnListsWithoutParty = 6,
                    TotalCountOfListsWithoutParty = 5,
                    TotalCountOfModifiedLists = 2,
                    TotalCountOfUnmodifiedLists = 3,
                };
                r.TotalCountOfVoters = 10000;
            });

        await ModifyDbEntities<ProportionalElectionUnmodifiedListResult>(
            r => r.ResultId == ResultId,
            r =>
            {
                r.ConventionalVoteCount = 10;
                r.EVotingVoteCount = 3;
                r.ECountingVoteCount = 4;
            });

        await ModifyDbEntities<ProportionalElectionListResult>(
            r => r.ResultId == ResultId,
            r =>
            {
                r.ConventionalSubTotal = new()
                {
                    ModifiedListBlankRowsCount = 50,
                    ModifiedListsCount = 40,
                    ModifiedListVotesCount = 150,
                    UnmodifiedListBlankRowsCount = 40,
                    UnmodifiedListsCount = 80,
                    UnmodifiedListVotesCount = 140,
                    ListVotesCountOnOtherLists = 10,
                };
                r.EVotingSubTotal = new()
                {
                    ModifiedListBlankRowsCount = 5,
                    ModifiedListsCount = 4,
                    ModifiedListVotesCount = 15,
                    UnmodifiedListBlankRowsCount = 4,
                    UnmodifiedListsCount = 8,
                    UnmodifiedListVotesCount = 14,
                    ListVotesCountOnOtherLists = 1,
                };
                r.ECountingSubTotal = new()
                {
                    ModifiedListBlankRowsCount = 6,
                    ModifiedListsCount = 5,
                    ModifiedListVotesCount = 16,
                    UnmodifiedListBlankRowsCount = 5,
                    UnmodifiedListsCount = 9,
                    UnmodifiedListVotesCount = 15,
                    ListVotesCountOnOtherLists = 2,
                };
            });

        await ModifyDbEntities<ProportionalElectionCandidateResult>(
            r => r.ListResult.ResultId == ResultId,
            r =>
            {
                r.ConventionalSubTotal = new()
                {
                    UnmodifiedListVotesCount = 5,
                    ModifiedListVotesCount = 4,
                    CountOfVotesFromAccumulations = 3,
                    CountOfVotesOnOtherLists = 2,
                };
                r.EVotingSubTotal = new()
                {
                    UnmodifiedListVotesCount = 1,
                    ModifiedListVotesCount = 1,
                    CountOfVotesFromAccumulations = 1,
                    CountOfVotesOnOtherLists = 1,
                };
                r.ECountingSubTotal = new()
                {
                    UnmodifiedListVotesCount = 2,
                    ModifiedListVotesCount = 3,
                    CountOfVotesFromAccumulations = 4,
                    CountOfVotesOnOtherLists = 5,
                };
            });

        await RunOnDb(async db =>
        {
            db.ProportionalElectionBundles.Add(new()
            {
                Id = Guid.Parse("1fe63a07-a6dd-4871-bc78-70e7829a07a3"),
                ElectionResultId = ResultId,
                CountOfBallots = 2,
                BallotNumbersToReview = [1],
            });
            await db.SaveChangesAsync();
        });

        await base.InitializeAsync();
    }

    [Fact]
    public async Task TestReset()
    {
        var result = await LoadResult();
        result.State.Should().Be(CountingCircleResultState.SubmissionDone);
        result.Bundles.Any().Should().BeTrue();
        result.Bundles = null!;
        result.MatchSnapshot("resultBefore");

        await TestEventPublisher.Publish(new ProportionalElectionResultResetted
        {
            EventInfo = GetMockedEventInfo(),
            ElectionResultId = ResultId.ToString(),
        });

        result = await LoadResult();
        result.State.Should().Be(CountingCircleResultState.SubmissionOngoing);
        result.Bundles.Any().Should().BeFalse();
        result.Bundles = null!;
        result.MatchSnapshot("resultAfter");
    }

    private async Task<ProportionalElectionResult> LoadResult()
    {
        var result = await RunOnDb(
            db => db
                .ProportionalElectionResults
                .AsSplitQuery()
                .Include(r => r.ListResults.OrderBy(x => x.List.Position))
                .Include(r => r.ListResults)
                    .ThenInclude(r => r.CandidateResults.OrderBy(x => x.Candidate.Number))
                .Include(r => r.Bundles)
                .Include(r => r.UnmodifiedListResults.OrderBy(x => x.List.Position))
                .SingleAsync(r => r.Id == ResultId));

        foreach (var listResult in result.ListResults)
        {
            listResult.Id = Guid.Empty;

            foreach (var candidateResult in listResult.CandidateResults)
            {
                candidateResult.Id = Guid.Empty;
                candidateResult.ListResultId = Guid.Empty;
            }
        }

        foreach (var unmodifiedListResult in result.UnmodifiedListResults)
        {
            unmodifiedListResult.Id = Guid.Empty;
        }

        return result;
    }
}
