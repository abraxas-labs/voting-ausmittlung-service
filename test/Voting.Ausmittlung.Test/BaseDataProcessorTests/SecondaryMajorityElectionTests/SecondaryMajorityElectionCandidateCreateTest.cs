// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
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
        candidates.MatchSnapshot();
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
}
