// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultResetTest : BaseIntegrationTest
{
    private static readonly Guid ResultId = MajorityElectionResultMockedData.GuidUzwilElectionResultInContestStGallen;

    public MajorityElectionResultResetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);

        await ModifyDbEntities<MajorityElectionResult>(
            r => r.Id == ResultId,
            r =>
            {
                r.State = CountingCircleResultState.ReadyForCorrection;
                r.CountOfVoters.EVotingSubTotal.ReceivedBallots = 1;
                r.CountOfVoters.EVotingSubTotal.AccountedBallots = 1;
                r.ConventionalCountOfBallotGroupVotes = 2;
                r.ConventionalCountOfDetailedEnteredBallots = 2;
                r.CountOfBundlesNotReviewedOrDeleted = 1;
                r.CountOfElectionsWithUnmappedEVotingWriteIns = 1;
                r.ConventionalSubTotal = new()
                {
                    EmptyVoteCountExclWriteIns = null,
                    IndividualVoteCount = 4,
                    InvalidVoteCount = 3,
                    TotalCandidateVoteCountExclIndividual = 15,
                };
                r.EVotingSubTotal = new()
                {
                    EmptyVoteCountExclWriteIns = 1,
                    InvalidVoteCount = 2,
                    IndividualVoteCount = 3,
                    TotalCandidateVoteCountExclIndividual = 9,
                };
            });

        await ModifyDbEntities<SecondaryMajorityElectionResult>(
            r => r.PrimaryResultId == ResultId,
            r =>
            {
                r.ConventionalSubTotal = new()
                {
                    IndividualVoteCount = 2,
                    InvalidVoteCount = 1,
                    TotalCandidateVoteCountExclIndividual = 4,
                };
                r.EVotingSubTotal = new()
                {
                    IndividualVoteCount = 1,
                    TotalCandidateVoteCountExclIndividual = 9,
                };
            });

        await ModifyDbEntities<MajorityElectionBallotGroupResult>(
            r => r.ElectionResultId == ResultId,
            r => r.VoteCount = 5);

        await ModifyDbEntities<MajorityElectionCandidateResult>(
            r => r.ElectionResultId == ResultId,
            r =>
            {
                r.ConventionalVoteCount = 2;
                r.EVotingExclWriteInsVoteCount = 1;
            });

        await ModifyDbEntities<SecondaryMajorityElectionCandidateResult>(
            r => r.ElectionResult.PrimaryResultId == ResultId,
            r =>
            {
                r.ConventionalVoteCount = 2;
                r.EVotingExclWriteInsVoteCount = 1;
            });

        await RunOnDb(async db =>
        {
            db.MajorityElectionResultBundles.Add(new()
            {
                Id = Guid.Parse("1fe63a07-a6dd-4871-bc78-70e7829a07a3"),
                ElectionResultId = ResultId,
                CountOfBallots = 2,
                BallotNumbersToReview = new() { 1 },
            });
            await db.SaveChangesAsync();
        });

        await base.InitializeAsync();
    }

    [Fact]
    public async Task TestReset()
    {
        var result = await LoadResult();
        result.State.Should().Be(CountingCircleResultState.ReadyForCorrection);
        result.CandidateResults.Any().Should().BeTrue();
        result.CandidateResults.All(r => r.VoteCount == 3).Should().BeTrue();
        result.Bundles.Any().Should().BeTrue();
        var smeResult = result.SecondaryMajorityElectionResults.First();
        smeResult.CandidateResults.Any().Should().BeTrue();
        smeResult.CandidateResults.All(r => r.VoteCount == 3).Should().BeTrue();

        result.CandidateResults = null!;
        result.Bundles = null!;
        smeResult.CandidateResults = null!;

        result.MatchSnapshot("resultBefore");

        await TestEventPublisher.Publish(new MajorityElectionResultResetted
        {
            EventInfo = GetMockedEventInfo(),
            ElectionResultId = ResultId.ToString(),
        });

        result = await LoadResult();
        result.State.Should().Be(CountingCircleResultState.SubmissionOngoing);
        result.CandidateResults.Any().Should().BeTrue();
        result.CandidateResults.All(r => r.VoteCount == 1).Should().BeTrue();
        result.Bundles.Any().Should().BeFalse();
        smeResult = result.SecondaryMajorityElectionResults.First();
        smeResult.CandidateResults.Any().Should().BeTrue();
        smeResult.CandidateResults.All(r => r.VoteCount == 1).Should().BeTrue();

        result.CandidateResults = null!;
        result.Bundles = null!;
        smeResult.CandidateResults = null!;

        result.MatchSnapshot("resultAfter");
    }

    private async Task<MajorityElectionResult> LoadResult()
    {
        var result = await RunOnDb(db => db
            .MajorityElectionResults
            .AsSplitQuery()
            .Include(r => r.CandidateResults.OrderBy(x => x.Candidate.Position))
            .ThenInclude(r => r.Candidate)
            .Include(r => r.SecondaryMajorityElectionResults)
            .ThenInclude(r => r.CandidateResults.OrderBy(x => x.Candidate.Position))
            .ThenInclude(r => r.Candidate)
            .Include(r => r.BallotGroupResults)
            .Include(r => r.Bundles)
            .SingleAsync(r => r.Id == ResultId));

        SetDynamicIdToDefaultValue(result.Bundles);
        SetDynamicIdToDefaultValue(result.SecondaryMajorityElectionResults);
        SetDynamicIdToDefaultValue(result.BallotGroupResults);

        return result;
    }
}
