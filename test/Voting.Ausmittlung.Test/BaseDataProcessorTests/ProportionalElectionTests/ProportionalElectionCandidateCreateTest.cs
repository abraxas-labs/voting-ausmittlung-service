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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionCandidateCreateTest : BaseDataProcessorTest
{
    public ProportionalElectionCandidateCreateTest(TestApplicationFactory factory)
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
    public async Task TestCandidateCreate()
    {
        var id1 = Guid.Parse("413525c6-8da4-4c49-bbdc-d6bc43ceacff");
        var id2 = Guid.Parse("498b4e2b-f1b4-4110-89b3-d9f5689eb1ec");
        await TestEventPublisher.Publish(
            new ProportionalElectionCandidateCreated
            {
                ProportionalElectionCandidate = new ProportionalElectionCandidateEventData
                {
                    Id = id1.ToString(),
                    ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
                    Position = 2,
                    FirstName = "firstName",
                    LastName = "lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Accumulated = false,
                    Locality = "locality",
                    Number = "number1",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
                    Origin = "origin",
                    CheckDigit = 6,
                },
            },
            new ProportionalElectionCandidateCreated
            {
                ProportionalElectionCandidate = new ProportionalElectionCandidateEventData
                {
                    Id = id2.ToString(),
                    ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
                    Position = 2,
                    FirstName = "firstName",
                    LastName = "lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Accumulated = false,
                    Locality = "locality",
                    Number = "number1",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Origin = "origin",
                    CheckDigit = 6,
                },
            });

        var candidates = await RunOnDb(
            db =>
                db.ProportionalElectionCandidates
                    .AsSplitQuery()
                    .Include(c => c.Translations)
                    .Include(c => c.ProportionalElectionList)
                    .ThenInclude(l => l.Translations)
                    .Include(c => c.Party)
                    .ThenInclude(p => p!.Translations)
                    .Where(c => c.Id == id1 || c.Id == id2)
                    .OrderBy(x => x.Position)
                    .ThenBy(x => x.Id)
                    .ToListAsync(),
            Languages.German);

        SetDynamicIdToDefaultValue(candidates.SelectMany(c => c.Translations));
        SetDynamicIdToDefaultValue(candidates.SelectMany(c => c.ProportionalElectionList.Translations));

        foreach (var candidate in candidates)
        {
            if (candidate.Party == null)
            {
                continue;
            }

            SetDynamicIdToDefaultValue(candidate.Party.Translations);
            candidate.Party.DomainOfInfluenceId = Guid.Empty;
        }

        candidates.MatchSnapshot();
    }

    [Fact]
    public async Task TestCandidateCreateShouldTruncateCandidateNumber()
    {
        var id = Guid.Parse("413525c6-8da4-4c49-bbdc-d6bc43ceacff");
        await TestEventPublisher.Publish(
            new ProportionalElectionCandidateCreated
            {
                ProportionalElectionCandidate = new ProportionalElectionCandidateEventData
                {
                    Id = id.ToString(),
                    ProportionalElectionListId = ProportionalElectionMockedData.ListIdStGallenProportionalElectionInContestStGallen,
                    Position = 2,
                    FirstName = "firstName",
                    LastName = "lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Accumulated = false,
                    Locality = "locality",
                    Number = "number1toolong",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    PartyId = DomainOfInfluenceMockedData.PartyIdBundAndere,
                    Origin = "origin",
                    CheckDigit = 6,
                },
            });

        var candidate = await RunOnDb(
            db => db.ProportionalElectionCandidates.FirstAsync(x => x.Id == id),
            Languages.German);

        candidate.Number.Should().Be("number1too");
    }
}
