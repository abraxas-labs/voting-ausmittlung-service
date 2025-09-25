// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
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

public class VoteUpdateTest : VoteProcessorBaseTest
{
    public VoteUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestUpdated()
    {
        await TestEventPublisher.Publish(
            new VoteUpdated
            {
                Vote = new VoteEventData
                {
                    Id = VoteMockedData.IdGossauVoteInContestStGallen,
                    PoliticalBusinessNumber = "2000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    ResultAlgorithm = SharedProto.VoteResultAlgorithm.CountingCircleUnanimity,
                    Type = SharedProto.VoteType.QuestionsOnSingleBallot,
                },
            },
            new VoteUpdated
            {
                Vote = new VoteEventData
                {
                    Id = VoteMockedData.IdGossauVoteInContestGossau,
                    PoliticalBusinessNumber = "2001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundesurnengang,
                },
            });

        var data = await GetData(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau)
                                      || x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen));
        data.MatchSnapshot("full");

        var simpleVote = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau) || x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)),
            Languages.French);
        RemoveDynamicData(simpleVote);
        simpleVote.MatchSnapshot("simple");
    }

    [Fact]
    public async Task TestUpdatedAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(
            new VoteAfterTestingPhaseUpdated
            {
                EventInfo = GetMockedEventInfo(),
                Id = VoteMockedData.IdGossauVoteInContestStGallen,
                PoliticalBusinessNumber = "2000",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 1") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 1") },
                InternalDescription = "Updated internal description",
                ReportDomainOfInfluenceLevel = 1,
            });

        var data = await GetData(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen));
        data.MatchSnapshot();
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
            new VoteUpdated
            {
                Vote = new VoteEventData
                {
                    Id = VoteMockedData.IdStGallenVoteInContestBund,
                    PoliticalBusinessNumber = "2000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 1") },
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

        await ModifyDbEntities<SimpleCountingCircleResult>(
            x => x.PoliticalBusinessId == VoteMockedData.BundVoteInContestBund.Id,
            x => x.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(
            new VoteUpdated
            {
                Vote = new VoteEventData
                {
                    Id = VoteMockedData.IdBundVoteInContestBund,
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
            2000);

        EnsureValidAggregatedVotingCards(
            contestDetailsBefore.VotingCards,
            contestDetailsAfter.VotingCards,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && x.Valid && x.Channel == VotingChannel.ByMail,
            1000);

        EnsureValidAggregatedVotingCards(
            contestDetailsBefore.VotingCards,
            contestDetailsAfter.VotingCards,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && !x.Valid && x.Channel == VotingChannel.ByMail,
            3000);

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && x.VoterType == VoterType.Swiss && x.Sex == SexType.Male,
            8000);

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
            2000);

        EnsureValidAggregatedVotingCards(
            doiDetailsBefore.Details!.VotingCards,
            doiDetailsAfter.Details!.VotingCards,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && x.Valid && x.Channel == VotingChannel.ByMail,
            1000);

        EnsureValidAggregatedVotingCards(
            doiDetailsBefore.Details!.VotingCards,
            doiDetailsAfter.Details!.VotingCards,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && !x.Valid && x.Channel == VotingChannel.ByMail,
            3000);

        EnsureValidAggregatedSubTotals(
            doiDetailsBefore.Details!.CountOfVotersInformationSubTotals,
            doiDetailsAfter.Details!.CountOfVotersInformationSubTotals,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && x.VoterType == VoterType.Swiss && x.Sex == SexType.Male,
            8000);
    }

    [Fact]
    public async Task TestShouldRemoveNotNeededSubTotals()
    {
        var subTotals = new List<CountOfVotersInformationSubTotal>
        {
            new()
            {
                VoterType = VoterType.Swiss,
                Sex = SexType.Male,
                CountOfVoters = 1050,
                DomainOfInfluenceType = DomainOfInfluenceType.Ch,
            },
            new()
            {
                VoterType = VoterType.Swiss,
                Sex = SexType.Female,
                CountOfVoters = 950,
                DomainOfInfluenceType = DomainOfInfluenceType.Ch,
            },
            new()
            {
                VoterType = VoterType.Foreigner,
                Sex = SexType.Male,
                CountOfVoters = 90,
                DomainOfInfluenceType = DomainOfInfluenceType.Ch,
            },
            new()
            {
                VoterType = VoterType.Foreigner,
                Sex = SexType.Female,
                CountOfVoters = 110,
                DomainOfInfluenceType = DomainOfInfluenceType.Ch,
            },
            new()
            {
                VoterType = VoterType.Minor,
                Sex = SexType.Male,
                CountOfVoters = 11,
                DomainOfInfluenceType = DomainOfInfluenceType.Ch,
            },
            new()
            {
                VoterType = VoterType.Minor,
                Sex = SexType.Female,
                CountOfVoters = 9,
                DomainOfInfluenceType = DomainOfInfluenceType.Ch,
            },
        };

        await RunOnDb(
            async db =>
            {
                var details = await db.ContestCountingCircleDetails
                    .AsTracking()
                    .Include(x => x.CountOfVotersInformationSubTotals)
                    .SingleAsync(x => x.Id == ContestCountingCircleDetailsMockData.GuidStGallenUrnengangBundContestCountingCircleDetails);
                details.CountOfVotersInformationSubTotals.Clear();

                foreach (var subTotal in subTotals)
                {
                    details.CountOfVotersInformationSubTotals.Add(subTotal);
                }

                var contestDetails = await db.ContestDetails
                    .AsTracking()
                    .Include(x => x.CountOfVotersInformationSubTotals)
                    .SingleAsync(x => x.ContestId == ContestMockedData.GuidBundesurnengang);
                contestDetails.CountOfVotersInformationSubTotals.Clear();

                foreach (var subTotal in subTotals)
                {
                    contestDetails.CountOfVotersInformationSubTotals.Add(new ContestCountOfVotersInformationSubTotal
                    {
                        Sex = subTotal.Sex,
                        CountOfVoters = subTotal.CountOfVoters.GetValueOrDefault(),
                        VoterType = subTotal.VoterType,
                        DomainOfInfluenceType = subTotal.DomainOfInfluenceType,
                    });
                }

                await db.SaveChangesAsync();
            });

        var contestDetailsBefore = await RunOnDb(
            db => db.ContestDetails
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.ContestId == ContestMockedData.GuidBundesurnengang));

        await ModifyDbEntities<SimpleCountingCircleResult>(
            x => x.PoliticalBusinessId == VoteMockedData.BundVoteInContestBund.Id,
            x => x.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(
            new VoteUpdated
            {
                Vote = new VoteEventData
                {
                    Id = VoteMockedData.IdBundVoteInContestBund,
                    PoliticalBusinessNumber = "2001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundesurnengang,
                },
            });

        var contestDetailsAfter = await RunOnDb(
            db => db.ContestDetails
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.ContestId == ContestMockedData.GuidBundesurnengang));

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Male && x.VoterType == VoterType.Swiss && x.DomainOfInfluenceType == DomainOfInfluenceType.Ch,
            0);

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Female && x.VoterType == VoterType.Foreigner && x.DomainOfInfluenceType == DomainOfInfluenceType.Ch,
            -110);

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Male && x.VoterType == VoterType.Minor && x.DomainOfInfluenceType == DomainOfInfluenceType.Ch,
            -11);
    }
}
