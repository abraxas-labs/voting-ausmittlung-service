// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
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
            InternalDescription = "Update internal description",
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
}
