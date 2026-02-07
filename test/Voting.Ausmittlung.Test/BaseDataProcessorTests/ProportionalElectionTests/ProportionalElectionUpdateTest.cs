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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionUpdateTest : BaseDataProcessorTest
{
    public ProportionalElectionUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestUpdate()
    {
        await TestEventPublisher.Publish(new ProportionalElectionUpdated
        {
            ProportionalElection = new ProportionalElectionEventData
            {
                Id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
                PoliticalBusinessNumber = "6000",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Update Proporzwahl") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Update Proporzwahl") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                ContestId = ContestMockedData.IdStGallenEvoting,
                AutomaticBallotBundleNumberGeneration = true,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSize = 25,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                CandidateCheckDigit = false,
                EnforceEmptyVoteCountingForCountingCircles = true,
                MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                NumberOfMandates = 2,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Physically,
                EnforceReviewProcedureForCountingCircles = false,
                EnforceCandidateCheckDigitForCountingCircles = false,
                FederalIdentification = 111,
            },
        });

        var result = await RunOnDb(
            db => db.ProportionalElections
                .Include(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen)),
            Languages.German);
        RemoveDynamicData(result!);
        result.MatchSnapshot("full");

        var simpleElection = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen)),
            Languages.German);
        RemoveDynamicData(simpleElection);
        simpleElection.MatchSnapshot("simple");
    }

    [Fact]
    public async Task TestUpdatedAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(new ProportionalElectionAfterTestingPhaseUpdated
        {
            EventInfo = GetMockedEventInfo(),
            Id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
            PoliticalBusinessNumber = "6000",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Update Proporzwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Update Proporzwahl") },
            EnforceEmptyVoteCountingForCountingCircles = true,
        });

        var result = await RunOnDb(
            db => db.ProportionalElections
                .Include(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Id == Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen)),
            Languages.German);

        RemoveDynamicData(result!);
        result.MatchSnapshot();
    }

    [Theory]
    [InlineData(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum, ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum)]
    [InlineData(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum, ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum)]
    public async Task TestUpdateWithDeprecatedMandateAlgorithms(SharedProto.ProportionalElectionMandateAlgorithm deprecatedMandateAlgorithm, ProportionalElectionMandateAlgorithm expectedMandateAlgorithm)
    {
        var id = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen);

        await TestEventPublisher.Publish(
            new ProportionalElectionUpdated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = id.ToString(),
                    PoliticalBusinessNumber = "6000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Updated Official Description") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Updated Short Description") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    NumberOfMandates = 6,
                    MandateAlgorithm = deprecatedMandateAlgorithm,
                    BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                    ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
                    EnforceReviewProcedureForCountingCircles = true,
                    CandidateCheckDigit = false,
                    EnforceCandidateCheckDigitForCountingCircles = true,
                },
            });

        var proportionalElection = await RunOnDb(db => db.ProportionalElections.SingleAsync(pe => pe.Id == id));
        proportionalElection.MandateAlgorithm.Should().Be(expectedMandateAlgorithm);
    }

    [Fact]
    public async Task TestShouldUpdateTotalCountOfVoters()
    {
        await ModifyDbEntities<CountOfVotersInformationSubTotal>(
            st => st.ContestCountingCircleDetailsId == ContestCountingCircleDetailsMockData.GuidGossauUrnengangGossauContestCountingCircleDetails && st.DomainOfInfluenceType == DomainOfInfluenceType.Ch,
            st => st.DomainOfInfluenceType = DomainOfInfluenceType.Sk);

        // to test that ContestCountingCircleDetailsNotUpdatableException is not throwed.
        await ModifyDbEntities<ProportionalElectionResult>(
            r => r.Id == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestGossau,
            r => r.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(
            new ProportionalElectionUpdated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
                    PoliticalBusinessNumber = "6000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    NumberOfMandates = 6,
                    MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                },
            });

        var results = await RunOnDb(
            db => db.ProportionalElectionResults
                .Where(x => x.ProportionalElection.PoliticalBusinessNumber == "6000")
                .Include(x => x.ProportionalElection)
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
            x => x.PoliticalBusinessId == ProportionalElectionMockedData.UzwilProportionalElectionInContestBundWithoutChilds.Id,
            x => x.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(
            new ProportionalElectionUpdated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = ProportionalElectionMockedData.IdUzwilProportionalElectionInContestBundWithoutChilds,
                    PoliticalBusinessNumber = "6001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundesurnengang,
                    NumberOfMandates = 3,
                    MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
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
                DomainOfInfluenceType = DomainOfInfluenceType.Ct,
            },
            new()
            {
                VoterType = VoterType.Swiss,
                Sex = SexType.Female,
                CountOfVoters = 950,
                DomainOfInfluenceType = DomainOfInfluenceType.Ct,
            },
            new()
            {
                VoterType = VoterType.Foreigner,
                Sex = SexType.Male,
                CountOfVoters = 90,
                DomainOfInfluenceType = DomainOfInfluenceType.Ct,
            },
            new()
            {
                VoterType = VoterType.Foreigner,
                Sex = SexType.Female,
                CountOfVoters = 110,
                DomainOfInfluenceType = DomainOfInfluenceType.Ct,
            },
            new()
            {
                VoterType = VoterType.Minor,
                Sex = SexType.Male,
                CountOfVoters = 11,
                DomainOfInfluenceType = DomainOfInfluenceType.Ct,
            },
            new()
            {
                VoterType = VoterType.Minor,
                Sex = SexType.Female,
                CountOfVoters = 9,
                DomainOfInfluenceType = DomainOfInfluenceType.Ct,
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
            x => x.PoliticalBusinessId == ProportionalElectionMockedData.BundProportionalElectionInContestBund.Id,
            x => x.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(
            new ProportionalElectionUpdated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = ProportionalElectionMockedData.IdBundProportionalElectionInContestBund,
                    PoliticalBusinessNumber = "6001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundesurnengang,
                    NumberOfMandates = 3,
                    MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
                },
            });

        var contestDetailsAfter = await RunOnDb(
            db => db.ContestDetails
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.ContestId == ContestMockedData.GuidBundesurnengang));

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Female && x.VoterType == VoterType.Swiss && x.DomainOfInfluenceType == DomainOfInfluenceType.Ct,
            0);

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Male && x.VoterType == VoterType.Foreigner && x.DomainOfInfluenceType == DomainOfInfluenceType.Ct,
            -90);

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Female && x.VoterType == VoterType.Minor && x.DomainOfInfluenceType == DomainOfInfluenceType.Ct,
            -9);
    }

    [Fact]
    public async Task TestAdjustElectionsCountOnUnionEndResults()
    {
        await ZhMockedData.Seed(RunScoped);

        var unionId = ZhMockedData.ProportionalElectionUnionGuidKtrat;
        var unionEndResult = await RunOnDb(db => db.ProportionalElectionUnionEndResults
            .SingleAsync(c => c.ProportionalElectionUnionId == unionId));

        unionEndResult.TotalCountOfElections.Should().Be(3);
        unionEndResult.CountOfDoneElections.Should().Be(3);

        var ev = new ProportionalElectionUpdated
        {
            ProportionalElection = new ProportionalElectionEventData
            {
                Id = ZhMockedData.ProportionalElectionGuidKtratWinterthur.ToString(),
                PoliticalBusinessNumber = "6000",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Updated Official Description") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Updated Short Description") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                ContestId = ContestMockedData.IdGossau,
                NumberOfMandates = 6,
                MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
                EnforceReviewProcedureForCountingCircles = true,
                CandidateCheckDigit = false,
                EnforceCandidateCheckDigitForCountingCircles = true,
            },
        };

        await TestEventPublisher.Publish(ev, ev);

        unionEndResult = await RunOnDb(db => db.ProportionalElectionUnionEndResults
            .SingleAsync(c => c.ProportionalElectionUnionId == unionId));

        unionEndResult.TotalCountOfElections.Should().Be(2);
        unionEndResult.CountOfDoneElections.Should().Be(2);
    }
}
