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
    public async Task TestShouldCreateMissingVotingCards()
    {
        await RunOnDb(
            async db =>
            {
                var details = await db.ContestCountingCircleDetails
                    .AsTracking()
                    .Include(x => x.VotingCards)
                    .SingleAsync(x => x.Id == ContestCountingCircleDetailsMockData.GuidStGallenUrnengangBundContestCountingCircleDetails);
                details.VotingCards = details.VotingCards.Where(x => x.DomainOfInfluenceType != DomainOfInfluenceType.Ct).ToList();
                await db.SaveChangesAsync();
            });

        var contestDetailsBefore = await RunOnDb(
            db => db.ContestDetails
                .Include(x => x.VotingCards)
                .SingleAsync(x => x.ContestId == ContestMockedData.GuidBundesurnengang));

        var doiDetailsBefore = await RunOnDb(
            db => db.DomainOfInfluences
                .Include(x => x.Details)
                .ThenInclude(x => x!.VotingCards)
                .SingleAsync(x => x.SnapshotContestId == ContestMockedData.GuidBundesurnengang && x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallen.Id));

        await TestEventPublisher.Publish(
            new MajorityElectionUpdated
            {
                MajorityElection = new MajorityElectionEventData
                {
                    Id = MajorityElectionMockedData.IdBundMajorityElectionInContestStGallen,
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
                .Include(x => x.VotingCards)
                .SingleAsync(x => x.Id == ContestCountingCircleDetailsMockData.GuidStGallenUrnengangBundContestCountingCircleDetails));

        var newCreatedVotingCards = details.VotingCards.Where(x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct).ToList();
        newCreatedVotingCards.Single(x => x.Valid && x.Channel == VotingChannel.BallotBox).CountOfReceivedVotingCards.Should().Be(2000);
        newCreatedVotingCards.Single(x => x.Valid && x.Channel == VotingChannel.ByMail).CountOfReceivedVotingCards.Should().Be(1000);
        newCreatedVotingCards.Single(x => !x.Valid && x.Channel == VotingChannel.ByMail).CountOfReceivedVotingCards.Should().Be(3000);

        var contestDetailsAfter = await RunOnDb(
            db => db.ContestDetails
                .Include(x => x.VotingCards)
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

        var doiDetailsAfter = await RunOnDb(
            db => db.DomainOfInfluences
                .Include(x => x.Details)
                .ThenInclude(x => x!.VotingCards)
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
            },
            new()
            {
                VoterType = VoterType.Swiss,
                Sex = SexType.Female,
                CountOfVoters = 950,
            },
            new()
            {
                VoterType = VoterType.Foreigner,
                Sex = SexType.Male,
                CountOfVoters = 90,
            },
            new()
            {
                VoterType = VoterType.Foreigner,
                Sex = SexType.Female,
                CountOfVoters = 110,
            },
            new()
            {
                VoterType = VoterType.Minor,
                Sex = SexType.Male,
                CountOfVoters = 11,
            },
            new()
            {
                VoterType = VoterType.Minor,
                Sex = SexType.Female,
                CountOfVoters = 9,
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

                details.TotalCountOfVoters = subTotals.Sum(s => s.CountOfVoters.GetValueOrDefault());

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
                    });
                }

                await db.SaveChangesAsync();
            });

        var ccDetailsBefore = await RunOnDb(db => db.ContestCountingCircleDetails.Include(x => x.CountOfVotersInformationSubTotals)
            .SingleAsync(x => x.Id == ContestCountingCircleDetailsMockData.GuidStGallenUrnengangBundContestCountingCircleDetails));

        var contestDetailsBefore = await RunOnDb(
            db => db.ContestDetails
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.ContestId == ContestMockedData.GuidBundesurnengang));

        await TestEventPublisher.Publish(
            new MajorityElectionUpdated
            {
                MajorityElection = new MajorityElectionEventData
                {
                    Id = MajorityElectionMockedData.IdBundMajorityElectionInContestStGallen,
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

        var detailsAfter = await RunOnDb(
            db => db.ContestCountingCircleDetails
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.Id == ContestCountingCircleDetailsMockData.GuidStGallenUrnengangBundContestCountingCircleDetails));

        var contestDetailsAfter = await RunOnDb(
            db => db.ContestDetails
                .Include(x => x.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.ContestId == ContestMockedData.GuidBundesurnengang));

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Female && x.VoterType == VoterType.Swiss,
            0);

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Male && x.VoterType == VoterType.Foreigner,
            -90);

        EnsureValidAggregatedSubTotals(
            contestDetailsBefore.CountOfVotersInformationSubTotals,
            contestDetailsAfter.CountOfVotersInformationSubTotals,
            x => x.Sex == SexType.Female && x.VoterType == VoterType.Minor,
            -9);
    }
}
