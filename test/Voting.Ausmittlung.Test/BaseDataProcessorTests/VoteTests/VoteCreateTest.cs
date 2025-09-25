// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.VoteTests;

public class VoteCreateTest : VoteProcessorBaseTest
{
    public VoteCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestCreated()
    {
        await TestEventPublisher.Publish(
            new VoteCreated
            {
                Vote = new VoteEventData
                {
                    Id = "5483076b-e596-44d3-b34e-6e9220eed84c",
                    PoliticalBusinessNumber = "2000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    ResultAlgorithm = SharedProto.VoteResultAlgorithm.CountingCircleUnanimity,
                    Type = SharedProto.VoteType.QuestionsOnSingleBallot,
                },
            },
            new VoteCreated
            {
                Vote = new VoteEventData
                {
                    Id = "051c2a1a-9df6-4c9c-98a2-d7f3d720c62e",
                    PoliticalBusinessNumber = "2001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundesurnengang,
                    Type = SharedProto.VoteType.VariantQuestionsOnMultipleBallots,
                },
            });

        var data = await GetData(x => x.PoliticalBusinessNumber == "2000"
                                      || x.PoliticalBusinessNumber == "2001");
        data.MatchSnapshot("full");

        var simpleVotes = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Where(x => x.PoliticalBusinessNumber == "2000"
                            || x.PoliticalBusinessNumber == "2001")
                .Include(x => x.Translations)
                .OrderBy(x => x.Id)
                .ToListAsync(),
            Languages.German);

        RemoveDynamicData(simpleVotes);
        simpleVotes.MatchSnapshot("simple");
    }

    [Fact]
    public async Task TestShouldUpdateTotalCountOfVoters()
    {
        await ModifyDbEntities<CountOfVotersInformationSubTotal>(
            st => st.ContestCountingCircleDetailsId == ContestCountingCircleDetailsMockData.GuidGossauUrnengangGossauContestCountingCircleDetails && st.DomainOfInfluenceType == DomainOfInfluenceType.Ch,
            st => st.DomainOfInfluenceType = DomainOfInfluenceType.Sk);

        // to test that ContestCountingCircleDetailsNotUpdatableException is not throwed.
        await ModifyDbEntities<VoteResult>(
            r => r.Id == VoteResultMockedData.GuidGossauVoteInContestGossauResult,
            r => r.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(
            new VoteCreated
            {
                Vote = new VoteEventData
                {
                    Id = "5483076b-e596-44d3-b34e-6e9220eed84c",
                    PoliticalBusinessNumber = "2000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    ResultAlgorithm = SharedProto.VoteResultAlgorithm.CountingCircleUnanimity,
                    Type = SharedProto.VoteType.QuestionsOnSingleBallot,
                },
            });

        var results = await RunOnDb(
            db => db.VoteResults
                .Where(x => x.Vote.PoliticalBusinessNumber == "2000")
                .Include(x => x.Vote)
                .ToListAsync());

        foreach (var result in results)
        {
            result.TotalCountOfVoters.Should().NotBe(0);
        }
    }

    [Fact]
    public async Task TestShouldCreateMissingVotingCardsAndSubTotals()
    {
        await RunOnDb(
            async db =>
            {
                var details = await db.ContestCountingCircleDetails
                    .AsSplitQuery()
                    .AsTracking()
                    .Include(x => x.VotingCards)
                    .Include(x => x.CountOfVotersInformationSubTotals)
                    .SingleAsync(x => x.Id == ContestCountingCircleDetailsMockData.GuidStGallenUrnengangBundContestCountingCircleDetails);
                details.VotingCards = details.VotingCards.Where(x => x.DomainOfInfluenceType != DomainOfInfluenceType.Ct).ToList();
                details.CountOfVotersInformationSubTotals = details.CountOfVotersInformationSubTotals.Where(x => x.DomainOfInfluenceType != DomainOfInfluenceType.Ct).ToList();
                await db.SaveChangesAsync();
            });

        var contestDetailsBefore = await RunOnDb(
            db => db.ContestDetails
                .AsSplitQuery()
                .Include(x => x.VotingCards)
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.ContestId == ContestMockedData.GuidBundesurnengang));

        var doiDetailsBefore = await RunOnDb(
            db => db.DomainOfInfluences
                .AsSplitQuery()
                .Include(x => x.Details)
                .ThenInclude(x => x!.VotingCards)
                .Include(x => x.Details)
                .ThenInclude(x => x!.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.SnapshotContestId == ContestMockedData.GuidBundesurnengang && x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallen.Id));

        await TestEventPublisher.Publish(
            new VoteCreated
            {
                Vote = new VoteEventData
                {
                    Id = "051c2a1a-9df6-4c9c-98a2-d7f3d720c62e",
                    PoliticalBusinessNumber = "2001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundesurnengang,
                },
            });

        var details = await RunOnDb(
            db => db.ContestCountingCircleDetails
                .AsSplitQuery()
                .Include(x => x.VotingCards)
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.Id == ContestCountingCircleDetailsMockData.GuidStGallenUrnengangBundContestCountingCircleDetails));

        var newCreatedVotingCards = details.VotingCards.Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct).ToList();
        newCreatedVotingCards.Single(x => x.Valid && x.Channel == VotingChannel.BallotBox).CountOfReceivedVotingCards.Should().Be(2000);
        newCreatedVotingCards.Single(x => x.Valid && x.Channel == VotingChannel.ByMail).CountOfReceivedVotingCards.Should().Be(1000);
        newCreatedVotingCards.Single(x => !x.Valid && x.Channel == VotingChannel.ByMail).CountOfReceivedVotingCards.Should().Be(3000);

        var newCreatedSubTotals = details.CountOfVotersInformationSubTotals.Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct).ToList();
        newCreatedSubTotals.Single(x => x.VoterType == VoterType.Swiss && x.Sex == SexType.Male).CountOfVoters.Should().Be(8000);
        newCreatedSubTotals.Single(x => x.VoterType == VoterType.Swiss && x.Sex == SexType.Female).CountOfVoters.Should().Be(7000);
        newCreatedSubTotals.Count.Should().Be(4);

        var contestDetailsAfter = await RunOnDb(
            db => db.ContestDetails
                .AsSplitQuery()
                .Include(x => x.VotingCards)
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.ContestId == ContestMockedData.GuidBundesurnengang));

        EnsureValidAggregatedVotingCards(
            contestDetailsBefore.VotingCards,
            contestDetailsAfter.VotingCards,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && x.Valid && x.Channel == VotingChannel.BallotBox,
            0);

        EnsureValidAggregatedVotingCards(
            contestDetailsBefore.VotingCards,
            contestDetailsAfter.VotingCards,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && x.Valid && x.Channel == VotingChannel.ByMail,
            0);

        EnsureValidAggregatedVotingCards(
            contestDetailsBefore.VotingCards,
            contestDetailsAfter.VotingCards,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && !x.Valid && x.Channel == VotingChannel.ByMail,
            0);

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && x.VoterType == VoterType.Swiss && x.Sex == SexType.Male,
            0);

        var doiDetailsAfter = await RunOnDb(
            db => db.DomainOfInfluences
                .AsSplitQuery()
                .Include(x => x.Details)
                .ThenInclude(x => x!.VotingCards)
                .Include(x => x.Details)
                .ThenInclude(x => x!.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.SnapshotContestId == ContestMockedData.GuidBundesurnengang && x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallen.Id));

        EnsureValidAggregatedVotingCards(
            doiDetailsBefore.Details!.VotingCards,
            doiDetailsAfter.Details!.VotingCards,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && x.Valid && x.Channel == VotingChannel.BallotBox,
            0);

        EnsureValidAggregatedVotingCards(
            doiDetailsBefore.Details!.VotingCards,
            doiDetailsAfter.Details!.VotingCards,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && x.Valid && x.Channel == VotingChannel.ByMail,
            0);

        EnsureValidAggregatedVotingCards(
            doiDetailsBefore.Details!.VotingCards,
            doiDetailsAfter.Details!.VotingCards,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && !x.Valid && x.Channel == VotingChannel.ByMail,
            0);

        EnsureValidAggregatedSubTotals(
            doiDetailsBefore.Details!.CountOfVotersInformationSubTotals,
            doiDetailsAfter.Details!.CountOfVotersInformationSubTotals,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && x.VoterType == VoterType.Swiss && x.Sex == SexType.Male,
            0);
    }
}
