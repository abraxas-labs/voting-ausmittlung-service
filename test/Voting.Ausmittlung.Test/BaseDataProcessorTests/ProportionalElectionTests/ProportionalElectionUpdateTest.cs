// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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
}
