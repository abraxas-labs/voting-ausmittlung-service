﻿// (c) Copyright 2022 by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionUpdateTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionUpdateTest(TestApplicationFactory factory)
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
    public async Task TestUpdateElection()
    {
        await TestEventPublisher.Publish(new SecondaryMajorityElectionUpdated
        {
            SecondaryMajorityElection = new SecondaryMajorityElectionEventData
            {
                Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                PoliticalBusinessNumber = "10546",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Update Nebenwahl") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Update Nebenwahl") },
                NumberOfMandates = 2,
                AllowedCandidates = SharedProto.SecondaryMajorityElectionAllowedCandidates.MayExistInPrimaryElection,
                PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
                Active = true,
            },
        });

        var election = await RunOnDb(
            db => db.SecondaryMajorityElections
                .AsSplitQuery()
                .Include(x => x.Translations)
                .Include(e => e.PrimaryMajorityElection.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund)),
            Languages.German);

        RemoveDynamicData(election.PrimaryMajorityElection);
        SetDynamicIdToDefaultValue(election.Translations);
        election.MatchSnapshot("full");

        var simpleElection = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund)),
            Languages.German);
        RemoveDynamicData(simpleElection);
        simpleElection.MatchSnapshot("simple");
    }

    [Fact]
    public async Task TestUpdateElectionAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(new SecondaryMajorityElectionAfterTestingPhaseUpdated
        {
            EventInfo = GetMockedEventInfo(),
            Id = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            PrimaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            OfficialDescription = { LanguageUtil.MockAllLanguages("Update Nebenwahl") },
            ShortDescription = { LanguageUtil.MockAllLanguages("Update Nebenwahl") },
            PoliticalBusinessNumber = "n1 UPDATED",
        });

        var election = await RunOnDb(
            db => db.SecondaryMajorityElections
                .AsSplitQuery()
                .Include(x => x.Translations)
                .Include(e => e.PrimaryMajorityElection.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund)),
            Languages.French);

        RemoveDynamicData(election.PrimaryMajorityElection);
        SetDynamicIdToDefaultValue(election.Translations);
        election.MatchSnapshot();
    }
}
