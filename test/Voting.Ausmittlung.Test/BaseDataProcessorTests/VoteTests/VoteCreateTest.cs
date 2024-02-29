// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.VoteTests;

public class VoteCreateTest : VoteProcessorBaseTest
{
    public VoteCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestCreated()
    {
        await TestEventPublisher.Publish(
            new VoteCreated
            {
                Vote = new VoteEventData
                {
                    Id = "5483076b-e596-44d3-b34e-6e9220eed84c",
                    PoliticalBusinessNumber = "2000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    ResultAlgorithm = SharedProto.VoteResultAlgorithm.CountingCircleUnanimity,
                },
            },
            new VoteCreated
            {
                Vote = new VoteEventData
                {
                    Id = "051c2a1a-9df6-4c9c-98a2-d7f3d720c62e",
                    PoliticalBusinessNumber = "2001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Neue Abstimmung 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundesurnengang,
                },
            });

        var data = await GetData(x => x.PoliticalBusinessNumber == "2000"
                                      || x.PoliticalBusinessNumber == "2001");
        data.MatchSnapshot("full");

        var simpleVotes = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Where(x => x.PoliticalBusinessNumber == "2000"
                            || x.PoliticalBusinessNumber == "2001")
                .Include(x => x.Translations)
                .OrderBy(x => x.Id)
                .ToListAsync(),
            Languages.German);

        RemoveDynamicData(simpleVotes);
        simpleVotes.MatchSnapshot("simple");
    }
}
