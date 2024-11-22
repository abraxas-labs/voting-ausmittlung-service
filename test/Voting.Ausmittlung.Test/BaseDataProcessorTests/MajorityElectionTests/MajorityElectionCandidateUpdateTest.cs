// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionTests;

public class MajorityElectionCandidateUpdateTest : BaseDataProcessorTest
{
    public MajorityElectionCandidateUpdateTest(TestApplicationFactory factory)
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
    public async Task TestUpdateCandidate()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionCandidateUpdated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    FirstName = "new first name",
                    LastName = "new last name",
                    PoliticalFirstName = "new pol first name",
                    PoliticalLastName = "new pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                    DateOfBirth = new DateTime(1961, 2, 26, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = false,
                    Position = 1,
                    Party = { LanguageUtil.MockAllLanguages("SVP") },
                    Locality = "locality",
                    Number = "numberNew",
                    Sex = SharedProto.SexType.Male,
                    Title = "new title",
                    ZipCode = "new zip code",
                    Origin = "origin",
                    CheckDigit = 0,
                },
            });

        var candidate = await RunOnDb(
            db => db.MajorityElectionCandidates
                .Include(c => c.Translations)
                .FirstAsync(c =>
                    c.Id == Guid.Parse(MajorityElectionMockedData
                        .CandidateIdStGallenMajorityElectionInContestStGallen)),
            Languages.German);

        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task TestUpdateCandidateAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionCandidateAfterTestingPhaseUpdated
            {
                EventInfo = GetMockedEventInfo(),
                Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
                MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                FirstName = "new first name",
                LastName = "new last name",
                PoliticalFirstName = "new pol first name",
                PoliticalLastName = "new pol last name",
                Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                DateOfBirth = new DateTime(1961, 2, 26, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Incumbent = false,
                Party = { LanguageUtil.MockAllLanguages("SVP") },
                Locality = "locality",
                Sex = SharedProto.SexType.Male,
                Title = "new title",
                ZipCode = "new zip code",
                Origin = "origin",
            });

        var candidate = await RunOnDb(
            db => db.MajorityElectionCandidates
                .Include(c => c.Translations)
                .FirstAsync(c =>
                    c.Id == Guid.Parse(MajorityElectionMockedData
                        .CandidateIdStGallenMajorityElectionInContestStGallen)),
            Languages.German);

        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task TestUpdateCandidateShouldTruncateCandidateNumber()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionCandidateUpdated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallen,
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    FirstName = "new first name",
                    LastName = "new last name",
                    PoliticalFirstName = "new pol first name",
                    PoliticalLastName = "new pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                    DateOfBirth = new DateTime(1961, 2, 26, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = false,
                    Position = 1,
                    Party = { LanguageUtil.MockAllLanguages("SVP") },
                    Locality = "locality",
                    Number = "numberNewtoolong",
                    Sex = SharedProto.SexType.Male,
                    Title = "new title",
                    ZipCode = "new zip code",
                    Origin = "origin",
                    CheckDigit = 0,
                },
            });

        var candidate = await RunOnDb(
            db => db.MajorityElectionCandidates
                .FirstAsync(c =>
                    c.Id == Guid.Parse(MajorityElectionMockedData
                        .CandidateIdStGallenMajorityElectionInContestStGallen)),
            Languages.German);

        candidate.Number.Should().Be("numberNewt");
    }

    [Fact]
    public async Task TestUpdateWithReferences()
    {
        var candidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund;

        await TestEventPublisher.Publish(
            new MajorityElectionCandidateUpdated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = candidateId,
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
                    FirstName = "new first name",
                    LastName = "new last name",
                    PoliticalFirstName = "new pol first name",
                    PoliticalLastName = "new pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                    DateOfBirth = new DateTime(1961, 2, 26, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = false,
                    Position = 1,
                    Party = { LanguageUtil.MockAllLanguages("SVP") },
                    Locality = "locality",
                    Number = "numberNew",
                    Sex = SharedProto.SexType.Male,
                    Title = "new title",
                    ZipCode = "new zip code",
                    Origin = "origin",
                    CheckDigit = 0,
                },
            });

        var secondaryCandidates = await RunOnDb(
            async db => await db.SecondaryMajorityElectionCandidates
                .Include(x => x.Translations.OrderBy(t => t.Language))
                .OrderBy(x => x.Id)
                .Where(x => x.CandidateReferenceId == Guid.Parse(candidateId))
                .ToListAsync(),
            Languages.German);

        foreach (var secondaryCandidate in secondaryCandidates)
        {
            SetDynamicIdToDefaultValue(secondaryCandidate.Translations);
        }

        secondaryCandidates.MatchSnapshot();
    }
}
