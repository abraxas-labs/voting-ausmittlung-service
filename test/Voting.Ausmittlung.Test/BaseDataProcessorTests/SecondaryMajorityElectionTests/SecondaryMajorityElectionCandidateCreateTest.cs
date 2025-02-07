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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateCreateTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionCandidateCreateTest(TestApplicationFactory factory)
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
        var id1 = Guid.Parse("4eced12b-1bac-45d7-9070-4de347382fe8");
        var id2 = Guid.Parse("bba268f7-5774-44f2-96fd-c9078ed7f765");
        var id3 = Guid.Parse("c4b1fd49-0433-469e-953d-3030aedd6e5a");
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateCreated
            {
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id1.ToString(),
                    MajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
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
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Party = { LanguageUtil.MockAllLanguages("SP") },
                    Origin = "origin",
                    CheckDigit = 9,
                },
            },
            new SecondaryMajorityElectionCandidateCreated
            {
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id2.ToString(),
                    MajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                    Position = 4,
                    FirstName = "first",
                    LastName = "last",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1961, 1, 28, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Number = "number1",
                    Sex = SharedProto.SexType.Male,
                    Title = "title",
                    ZipCode = "zip code",
                    Party = { LanguageUtil.MockAllLanguages("CVP") },
                    Origin = "origin",
                    CheckDigit = 9,
                },
            },
            new SecondaryMajorityElectionCandidateCreated
            {
                IsOnSeparateBallot = true,
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id3.ToString(),
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot,
                    Position = 6,
                    FirstName = "first new",
                    LastName = "last new",
                    PoliticalFirstName = "pol first name new",
                    PoliticalLastName = "pol last name new",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1961, 1, 28, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Number = "number1",
                    Sex = SharedProto.SexType.Male,
                    Title = "title",
                    ZipCode = "zip code",
                    Party = { LanguageUtil.MockAllLanguages("CVP") },
                    Origin = "origin",
                    CheckDigit = 9,
                },
            });

        var candidates = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates
                .Include(x => x.Translations)
                .Where(x => x.Id == id1 || x.Id == id2)
                .OrderBy(x => x.Id)
                .ToListAsync(),
            Languages.Italian);
        SetDynamicIdToDefaultValue(candidates.SelectMany(x => x.Translations));
        candidates.MatchSnapshot("candidates");

        var candidatesOnSeparateBallots = await RunOnDb(
            db => db.MajorityElectionCandidates
                .Include(x => x.Translations)
                .Where(x => x.Id == id3)
                .OrderBy(x => x.Id)
                .ToListAsync(),
            Languages.German);
        SetDynamicIdToDefaultValue(candidatesOnSeparateBallots.SelectMany(x => x.Translations));
        candidatesOnSeparateBallots.MatchSnapshot("candidatesOnSeparateBallots");
    }

    [Fact]
    public async Task TestCreateCandidateAfterSubmissionStarted()
    {
        await MajorityElectionEndResultMockedData.Seed(RunScoped);

        var id = Guid.Parse("94f728b7-a0f1-4df3-8300-cd0f18155a1c");
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateCreated
            {
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id.ToString(),
                    MajorityElectionId = MajorityElectionEndResultMockedData.SecondaryElectionId,
                    Position = 5,
                    FirstName = "late firstName",
                    LastName = "late lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1965, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Number = "number1",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Party = { LanguageUtil.MockAllLanguages("SP") },
                    Origin = "origin",
                    CheckDigit = 9,
                },
            });

        var candidate = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates
                .Include(x => x.Translations)
                .Where(x => x.Id == id)
                .Include(x => x.EndResult)
                .FirstAsync(),
            Languages.German);
        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot(c => c.EndResult!.Id, c => c.EndResult!.SecondaryMajorityElectionEndResultId);
    }

    [Fact]
    public async Task TestCreateCandidateAfterSubmissionStartedOnSeparateBallot()
    {
        await MajorityElectionEndResultMockedData.Seed(RunScoped, secondaryOnSeparateBallot: true);

        var id = Guid.Parse("16aa4c4a-2404-4ea6-82cf-006e033688bb");
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateCreated
            {
                IsOnSeparateBallot = true,
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id.ToString(),
                    MajorityElectionId = MajorityElectionEndResultMockedData.SecondaryElectionId,
                    Position = 5,
                    FirstName = "late firstName",
                    LastName = "late lastName",
                    PoliticalFirstName = "pol first name",
                    PoliticalLastName = "pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("occupation title") },
                    DateOfBirth = new DateTime(1965, 1, 13, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = true,
                    Locality = "locality",
                    Number = "number1",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Party = { LanguageUtil.MockAllLanguages("SP") },
                    Origin = "origin",
                    CheckDigit = 9,
                },
            });

        var candidate = await RunOnDb(
            db => db.MajorityElectionCandidates
                .Include(x => x.Translations)
                .Where(x => x.Id == id)
                .Include(x => x.EndResult)
                .FirstAsync(),
            Languages.German);
        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot(c => c.EndResult!.Id, c => c.EndResult!.MajorityElectionEndResultId);
    }

    [Fact]
    public async Task TestCreateCandidateShouldTruncateCandidateNumber()
    {
        var id = Guid.Parse("4eced12b-1bac-45d7-9070-4de347382fe8");
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateCreated
            {
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id.ToString(),
                    MajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
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
                    Number = "number1toolong",
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Party = { LanguageUtil.MockAllLanguages("SP") },
                    Origin = "origin",
                    CheckDigit = 9,
                },
            });

        var candidate = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates.FirstAsync(x => x.Id == id),
            Languages.Italian);
        candidate.Number.Should().Be("number1too");
    }

    [Fact]
    public async Task TestCreateCandidateAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ContestTestingPhaseEnded
            {
                ContestId = ContestMockedData.IdBundesurnengang,
            });

        var id = Guid.Parse("94f728b7-a0f1-4df3-8300-cd0f18155a1c");
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new SecondaryMajorityElectionCandidateCreated
            {
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id.ToString(),
                    MajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
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
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Party = { LanguageUtil.MockAllLanguages("SP") },
                    Origin = "origin",
                    CheckDigit = 9,
                },
            });

        var candidate = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates
                .Where(x => x.Id == id)
                .FirstAsync(),
            Languages.German);

        candidate.CreatedDuringActiveContest.Should().BeTrue();
    }

    [Fact]
    public async Task TestCreateCandidateAfterTestingPhaseEndedOnSeparateBallot()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ContestTestingPhaseEnded
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
            });

        var id = Guid.Parse("96424fd2-7834-43f5-9b58-7deed03130e2");
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new SecondaryMajorityElectionCandidateCreated
            {
                IsOnSeparateBallot = true,
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = id.ToString(),
                    MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot,
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
                    Sex = SharedProto.SexType.Female,
                    Title = "title",
                    ZipCode = "zip code",
                    Party = { LanguageUtil.MockAllLanguages("SP") },
                    Origin = "origin",
                    CheckDigit = 9,
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
