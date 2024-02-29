// (c) Copyright 2024 by Abraxas Informatik AG
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
}
