// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionCandidateUpdateTest : BaseDataProcessorTest
{
    public ProportionalElectionCandidateUpdateTest(TestApplicationFactory factory)
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
    public async Task TestCandidateUpdate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionCandidateUpdated
            {
                ProportionalElectionCandidate = new ProportionalElectionCandidateEventData
                {
                    Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
                    ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
                    FirstName = "new first name",
                    LastName = "new last name",
                    PoliticalFirstName = "new pol first name",
                    PoliticalLastName = "new pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                    DateOfBirth = new DateTime(1961, 2, 26, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = false,
                    Position = 1,
                    Accumulated = false,
                    Locality = "locality",
                    Number = "numberNew",
                    Sex = SharedProto.SexType.Male,
                    Title = "new title",
                    ZipCode = "new zip code",
                    PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
                    Origin = "origin",
                },
            });

        var idGuid = Guid.Parse(ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen);
        var candidate = await RunOnDb(
            db => db.ProportionalElectionCandidates
                .AsSplitQuery()
                .Include(c => c.Translations)
                .Include(x => x.ProportionalElectionList)
                .ThenInclude(l => l.Translations)
                .Include(c => c.Party)
                .ThenInclude(p => p!.Translations)
                .FirstAsync(x => x.Id == idGuid),
            Languages.German);

        SetDynamicIdToDefaultValue(candidate.Translations);
        SetDynamicIdToDefaultValue(candidate.ProportionalElectionList.Translations);
        SetDynamicIdToDefaultValue(candidate.Party!.Translations);
        candidate.Party.DomainOfInfluenceId = Guid.Empty;
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task TestCandidateUpdateAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionCandidateAfterTestingPhaseUpdated
            {
                EventInfo = GetMockedEventInfo(),
                Id = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
                ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
                ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
                FirstName = "new first name",
                LastName = "new last name",
                PoliticalFirstName = "new pol first name",
                PoliticalLastName = "new pol last name",
                Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                DateOfBirth = new DateTime(1961, 2, 26, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Incumbent = false,
                Locality = "locality",
                Sex = SharedProto.SexType.Male,
                Title = "new title",
                ZipCode = "new zip code",
                Origin = "origin",
            });

        var idGuid = Guid.Parse(ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen);
        var candidate = await RunOnDb(
            db => db.ProportionalElectionCandidates
                .AsSplitQuery()
                .Include(c => c.Translations)
                .Include(x => x.ProportionalElectionList)
                .ThenInclude(l => l.Translations)
                .FirstAsync(x => x.Id == idGuid),
            Languages.Italian);

        SetDynamicIdToDefaultValue(candidate.Translations);
        SetDynamicIdToDefaultValue(candidate.ProportionalElectionList.Translations);
        candidate.MatchSnapshot();
    }
}
