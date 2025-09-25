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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionTests;

public class MajorityElectionUpdateTest : BaseDataProcessorTest
{
    public MajorityElectionUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestUpdate()
    {
        await TestEventPublisher.Publish(new MajorityElectionUpdated
        {
            MajorityElection = new MajorityElectionEventData
            {
                Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                PoliticalBusinessNumber = "8164",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Update Majorzwahl") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Update Majorzwahl") },
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                ContestId = ContestMockedData.IdStGallenEvoting,
                AutomaticBallotBundleNumberGeneration = true,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSize = 25,
                BallotBundleSampleSize = 8,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                CandidateCheckDigit = false,
                EnforceEmptyVoteCountingForCountingCircles = true,
                MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.RelativeMajority,
                ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults,
                EnforceResultEntryForCountingCircles = false,
                NumberOfMandates = 2,
                EnforceReviewProcedureForCountingCircles = false,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Physically,
                EnforceCandidateCheckDigitForCountingCircles = false,
                FederalIdentification = 111,
            },
        });

        var election = await RunOnDb(
            db => db.MajorityElections
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen)),
            Languages.German);
        RemoveDynamicData(election);
        election.MatchSnapshot("full");

        var simpleElection = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen)),
            Languages.German);
        RemoveDynamicData(simpleElection);
        simpleElection.MatchSnapshot("simple");
    }

    [Fact]
    public async Task TestUpdateAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(new MajorityElectionAfterTestingPhaseUpdated
        {
            EventInfo = GetMockedEventInfo(),
            Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
            PoliticalBusinessNumber = "8164",
            OfficialDescription = { LanguageUtil.MockAllLanguages("Update Majorzwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Update Majorzwahl") },
            EnforceEmptyVoteCountingForCountingCircles = true,
            EnforceResultEntryForCountingCircles = false,
            ReportDomainOfInfluenceLevel = 1,
        });

        var election = await RunOnDb(
            db => db.MajorityElections
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen)),
            Languages.German);
        RemoveDynamicData(election);
        election.MatchSnapshot("full");

        var simpleElection = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen)),
            Languages.German);
        RemoveDynamicData(simpleElection);
        simpleElection.MatchSnapshot("simple");
    }

    [Fact]
    public async Task TestShouldUpdateTotalCountOfVoters()
    {
        await ModifyDbEntities<CountOfVotersInformationSubTotal>(
            st => st.ContestCountingCircleDetailsId == ContestCountingCircleDetailsMockData.GuidGossauUrnengangGossauContestCountingCircleDetails && st.DomainOfInfluenceType == DomainOfInfluenceType.Ch,
            st => st.DomainOfInfluenceType = DomainOfInfluenceType.Sk);

        // to test that ContestCountingCircleDetailsNotUpdatableException is not throwed.
        await ModifyDbEntities<MajorityElectionResult>(
            r => r.Id == MajorityElectionResultMockedData.GuidGossauElectionResultInContestGossau,
            r => r.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(
            new MajorityElectionUpdated
            {
                MajorityElection = new MajorityElectionEventData
                {
                    Id = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
                    PoliticalBusinessNumber = "8000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Update Majorzwahl") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Update Majorzwahl") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    NumberOfMandates = 6,
                    MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.AbsoluteMajority,
                    ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults,
                },
            });

        var results = await RunOnDb(
            db => db.MajorityElectionResults
                .Where(x => x.MajorityElection.PoliticalBusinessNumber == "8000")
                .Include(x => x.MajorityElection)
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
            x => x.PoliticalBusinessId == MajorityElectionMockedData.UzwilMajorityElectionInContestBundWithoutChilds.Id,
            x => x.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(
            new MajorityElectionUpdated
            {
                MajorityElection = new MajorityElectionEventData
                {
                    Id = MajorityElectionMockedData.IdUzwilMajorityElectionInContestBundWithoutChilds,
                    PoliticalBusinessNumber = "8001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundesurnengang,
                    NumberOfMandates = 3,
                    MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.RelativeMajority,
                    ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
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
            x => x.PoliticalBusinessId == MajorityElectionMockedData.BundMajorityElectionInContestBund.Id,
            x => x.State = CountingCircleResultState.SubmissionDone);

        await TestEventPublisher.Publish(
            new MajorityElectionUpdated
            {
                MajorityElection = new MajorityElectionEventData
                {
                    Id = MajorityElectionMockedData.IdBundMajorityElectionInContestBund,
                    PoliticalBusinessNumber = "8001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundesurnengang,
                    NumberOfMandates = 3,
                    MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.RelativeMajority,
                    ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
                },
            });

        var contestDetailsAfter = await RunOnDb(
            db => db.ContestDetails
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.ContestId == ContestMockedData.GuidBundesurnengang));

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Female && x.VoterType == VoterType.Swiss && x.DomainOfInfluenceType == DomainOfInfluenceType.Ch,
            0);

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Male && x.VoterType == VoterType.Foreigner && x.DomainOfInfluenceType == DomainOfInfluenceType.Ch,
            -90);

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Female && x.VoterType == VoterType.Minor && x.DomainOfInfluenceType == DomainOfInfluenceType.Ch,
            -9);
    }
}
