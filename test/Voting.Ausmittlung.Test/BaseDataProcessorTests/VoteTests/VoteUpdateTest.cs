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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.VoteTests;

public class VoteUpdateTest : VoteProcessorBaseTest
{
    public VoteUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestUpdated()
    {
        await TestEventPublisher.Publish(
            new VoteUpdated
            {
                Vote = new VoteEventData
                {
                    Id = VoteMockedData.IdGossauVoteInContestStGallen,
                    PoliticalBusinessNumber = "2000",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 1") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 1") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdGossau,
                    ContestId = ContestMockedData.IdGossau,
                    ResultAlgorithm = SharedProto.VoteResultAlgorithm.CountingCircleUnanimity,
                },
            },
            new VoteUpdated
            {
                Vote = new VoteEventData
                {
                    Id = VoteMockedData.IdGossauVoteInContestGossau,
                    PoliticalBusinessNumber = "2001",
                    OfficialDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 2") },
                    ShortDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 2") },
                    DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                    ContestId = ContestMockedData.IdBundesurnengang,
                },
            });

        var data = await GetData(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau)
                                      || x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen));
        data.MatchSnapshot("full");

        var simpleVote = await RunOnDb(
            db => db.SimplePoliticalBusinesses
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau) || x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)),
            Languages.French);
        RemoveDynamicData(simpleVote);
        simpleVote.MatchSnapshot("simple");
    }

    [Fact]
    public async Task TestUpdatedAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(
            new VoteAfterTestingPhaseUpdated
            {
                EventInfo = GetMockedEventInfo(),
                Id = VoteMockedData.IdGossauVoteInContestStGallen,
                PoliticalBusinessNumber = "2000",
                OfficialDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 1") },
                ShortDescription = { LanguageUtil.MockAllLanguages("Updated Abstimmung 1") },
                InternalDescription = "Updated internal description",
                ReportDomainOfInfluenceLevel = 1,
            });

        var data = await GetData(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen));
        data.MatchSnapshot();
    }
}
