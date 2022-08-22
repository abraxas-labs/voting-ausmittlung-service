// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
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
                    MandateAlgorithm = SharedProto.ProportionalElectionMandateAlgorithm.DoppelterPukelsheim5Quorum,
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
}
