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

public class MajorityElectionCandidateCreateTest : BaseDataProcessorTest
{
    public MajorityElectionCandidateCreateTest(TestApplicationFactory factory)
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
    public async Task TestCreateCandidate()
    {
        var id1 = Guid.Parse("e1141bcd-8ce7-4717-9faf-5774d36ce7d8");
        var id2 = Guid.Parse("b55acdab-385b-49ef-99bc-e57d60863067");
        await TestEventPublisher.Publish(
            new MajorityElectionCandidateCreated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id1.ToString(),
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    Position = 2,
                    FirstName = "firstName",
                    LastName = "lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Party = { LanguageUtil.MockAllLanguages("Grüne") },
                    Number = "number1",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Origin = "origin",
                    CheckDigit = 9,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
                },
            },
            new MajorityElectionCandidateCreated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id2.ToString(),
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    Position = 3,
                    FirstName = "firstName",
                    LastName = "lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Number = "number1",
                    Party = { LanguageUtil.MockAllLanguages("BDP") },
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Origin = "origin",
                    CheckDigit = 9,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
                },
            });

        var results = await RunOnDb(
            db => db.MajorityElectionCandidates
                .Where(x => x.Id == id1 || x.Id == id2)
                .Include(c => c.Translations)
                .OrderBy(x => x.Position)
                .ToListAsync(),
            Languages.German);

        SetDynamicIdToDefaultValue(results.SelectMany(x => x.Translations));
        results.MatchSnapshot();
    }

    [Fact]
    public async Task TestCreateCandidateAfterSubmissionStarted()
    {
        await MajorityElectionEndResultMockedData.Seed(RunScoped);

        var id = Guid.Parse("8e492d58-4cc3-4aff-928e-b813f26ddc12");
        await TestEventPublisher.Publish(
            new MajorityElectionCandidateCreated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id.ToString(),
                    MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
                    Position = 11,
                    FirstName = "late first name",
                    LastName = "late last name",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1965, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Party = { LanguageUtil.MockAllLanguages("CVP") },
                    Number = "number1",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Origin = "origin",
                    CheckDigit = 9,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
                },
            });

        var candidate = await RunOnDb(
            db => db.MajorityElectionCandidates
                .Where(x => x.Id == id)
                .Include(x => x.EndResult)
                .Include(c => c.Translations)
                .FirstAsync(),
            Languages.German);

        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot(c => c.EndResult!.Id, c => c.EndResult!.MajorityElectionEndResultId);
    }

    [Fact]
    public async Task TestCreateCandidateShouldTruncateCandidateNumber()
    {
        var id = Guid.Parse("e1141bcd-8ce7-4717-9faf-5774d36ce7d8");
        await TestEventPublisher.Publish(
            new MajorityElectionCandidateCreated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id.ToString(),
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    Position = 2,
                    FirstName = "firstName",
                    LastName = "lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Party = { LanguageUtil.MockAllLanguages("Grüne") },
                    Number = "number1toolong",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Origin = "origin",
                    CheckDigit = 9,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
                },
            });

        var candidate = await RunOnDb(
            db => db.MajorityElectionCandidates.FirstAsync(x => x.Id == id),
            Languages.German);

        candidate.Number.Should().Be("number1too");
    }

    [Fact]
    public async Task TestCreateCandidateAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ContestTestingPhaseEnded
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
            });

        var id = Guid.Parse("8e492d58-4cc3-4aff-928e-b813f26ddc12");
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionCandidateCreated
            {
                MajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id.ToString(),
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                    Position = 2,
                    FirstName = "firstName",
                    LastName = "lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1960, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Party = { LanguageUtil.MockAllLanguages("Grüne") },
                    Number = "number1",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Origin = "origin",
                    CheckDigit = 9,
                    Street = "street",
                    HouseNumber = "1a",
                    Country = "CH",
                },
            });

        var candidate = await RunOnDb(
            db => db.MajorityElectionCandidates
                .Where(x => x.Id == id)
                .FirstAsync(),
            Languages.German);

        candidate.CreatedDuringActiveContest.Should().BeTrue();
    }
}
