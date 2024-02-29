// (c) Copyright 2024 by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateUpdateTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionCandidateUpdateTest(TestApplicationFactory factory)
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
            new SecondaryMajorityElectionCandidateUpdated
            {
                SecondaryMajorityElectionCandidate = new MajorityElectionCandidateEventData
                {
                    Id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
                    MajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                    FirstName = "new first name",
                    LastName = "new last name",
                    PoliticalFirstName = "new pol first name",
                    PoliticalLastName = "new pol last name",
                    Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                    OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                    DateOfBirth = new DateTime(1961, 8, 6, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                    Incumbent = false,
                    Position = 1,
                    Locality = "locality",
                    Number = "numberNew",
                    Sex = SharedProto.SexType.Male,
                    Title = "new title",
                    ZipCode = "new zip code",
                    Party = { LanguageUtil.MockAllLanguages("NEW") },
                    Origin = "origin",
                    CheckDigit = 0,
                },
            });

        var candidate = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund)),
            Languages.German);
        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task TestUpdateCandidateAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated
            {
                EventInfo = GetMockedEventInfo(),
                Id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
                SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                FirstName = "new first name",
                LastName = "new last name",
                PoliticalFirstName = "new pol first name",
                PoliticalLastName = "new pol last name",
                Occupation = { LanguageUtil.MockAllLanguages("new occupation") },
                OccupationTitle = { LanguageUtil.MockAllLanguages("new occupation title") },
                DateOfBirth = new DateTime(1961, 8, 6, 0, 0, 0, DateTimeKind.Utc).ToTimestamp(),
                Incumbent = false,
                Locality = "locality",
                Sex = SharedProto.SexType.Male,
                Title = "new title",
                ZipCode = "new zip code",
                Party = { LanguageUtil.MockAllLanguages("NEW") },
                Origin = "origin",
            });

        var candidate = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund)),
            Languages.German);
        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot();
    }
}
