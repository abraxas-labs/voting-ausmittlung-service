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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionTests;

public class MajorityElectionCreateTest : BaseDataProcessorTest
{
    public MajorityElectionCreateTest(TestApplicationFactory factory)
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
    public async Task TestCreate()
    {
        var id1 = Guid.Parse("4a258e1d-ba12-4564-a8eb-6bf89262dece");
        var id2 = Guid.Parse("764f30b9-3510-41a5-b7be-64173551d292");
        await TestEventPublisher.Publish(
            new MajorityElectionCreated
            {
                MajorityElection = new MajorityElectionEventData
                {
                    Id = id1.ToString(),
                    PoliticalBusinessNumber = "8000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    NumberOfMandates = 6,
                    MandateAlgorithm = SharedProto.MajorityElectionMandateAlgorithm.AbsoluteMajority,
                    ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults,
                    FederalIdentification = 111,
                },
            },
            new MajorityElectionCreated
            {
                MajorityElection = new MajorityElectionEventData
                {
                    Id = id2.ToString(),
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

        var elections = await RunOnDb(
            db => db.MajorityElections
                .Where(x => x.Id == id1 || x.Id == id2)
                .Include(x => x.Translations)
                .OrderBy(x => x.Id)
                .ToListAsync(),
            Languages.German);

        RemoveDynamicData(elections);
        elections.MatchSnapshot("full");

        var simpleElections = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Where(x => x.Id == id1 || x.Id == id2)
                .Include(x => x.Translations)
                .OrderBy(x => x.Id)
                .ToListAsync(),
            Languages.French);

        RemoveDynamicData(simpleElections);
        simpleElections.MatchSnapshot("simple");
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
            new MajorityElectionCreated
            {
                MajorityElection = new MajorityElectionEventData
                {
                    Id = "7e96190c-f1db-4231-b609-700040d54554",
                    PoliticalBusinessNumber = "8000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Majorzwahl 1") },
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

        await TestEventPublisher.Publish(
            new MajorityElectionCreated
            {
                MajorityElection = new MajorityElectionEventData
                {
                    Id = "c3dc377b-ee9d-4265-b281-c29e2ca7dc7b",
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
}
