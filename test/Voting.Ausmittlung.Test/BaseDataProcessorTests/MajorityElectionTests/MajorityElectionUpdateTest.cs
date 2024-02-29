// (c) Copyright 2024 by Abraxas Informatik AG
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
}
