// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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

public class ProportionalElectionCreateTest : BaseDataProcessorTest
{
    public ProportionalElectionCreateTest(TestApplicationFactory factory)
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
    public async Task TestCreate()
    {
        var id1 = Guid.Parse("91d9234e-cd14-4e18-95ce-5c06a9cec6f0");
        var id2 = Guid.Parse("350fb432-9083-44f0-ade9-f9f7f99dabe8");
        await TestEventPublisher.Publish(
            new ProportionalElectionCreated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = id1.ToString(),
                    PoliticalBusinessNumber = "6000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    NumberOfMandates = 6,
                    MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.HagenbachBischoff,
                    FederalIdentification = 111,
                },
            },
            new ProportionalElectionCreated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = id2.ToString(),
                    PoliticalBusinessNumber = "6001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundesurnengang,
                    NumberOfMandates = 3,
                    MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum,
                },
            });

        var elections = await RunOnDb(
            db => db.ProportionalElections
                .Include(e => e.Translations)
                .Where(x => x.Id == id1 || x.Id == id2)
                .OrderBy(x => x.PoliticalBusinessNumber)
                .ToListAsync(),
            Languages.German);

        SetDynamicIdToDefaultValue(elections.SelectMany(e => e.Translations));
        foreach (var election in elections)
        {
            election.DomainOfInfluenceId = Guid.Empty;
        }

        elections.MatchSnapshot("full");

        var simpleElections = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(e => e.Translations)
                .Where(x => x.Id == id1 || x.Id == id2)
                .OrderBy(x => x.Id)
                .ToListAsync(),
            Languages.German);

        SetDynamicIdToDefaultValue(simpleElections.SelectMany(e => e.Translations));
        foreach (var election in simpleElections)
        {
            election.DomainOfInfluenceId = Guid.Empty;
        }

        simpleElections.MatchSnapshot("simple");
    }

    [Theory]
    [InlineData(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim0Quorum, ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum)]
    [InlineData(SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum, ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum)]
    public async Task TestCreateWithDeprecatedMandateAlgorithms(SharedProto.ProportionalElectionMandateAlgorithm deprecatedMandateAlgorithm, ProportionalElectionMandateAlgorithm expectedMandateAlgorithm)
    {
        var id = Guid.Parse("f6ebc06e-a252-4cf4-9aa7-9ad46dd517f3");
        await TestEventPublisher.Publish(
            new ProportionalElectionCreated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = id.ToString(),
                    PoliticalBusinessNumber = "6000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Proporzwahl 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Proporzwahl 1") },
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
            new ProportionalElectionCreated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = "8393c549-04b9-4c6c-9cd7-7ccb57f89e5d",
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
                details.CountOfVotersInformationSubTotals.Add(new CountOfVotersInformationSubTotal
                {
                    VoterType = VoterType.Foreigner,
                });

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
            new ProportionalElectionCreated
            {
                ProportionalElection = new ProportionalElectionEventData
                {
                    Id = "809049e5-3245-4fdf-9708-28ec4b8f8722",
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

        details.CountOfVotersInformationSubTotals.Any(st => st.VoterType == VoterType.Foreigner).Should().BeFalse();

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
