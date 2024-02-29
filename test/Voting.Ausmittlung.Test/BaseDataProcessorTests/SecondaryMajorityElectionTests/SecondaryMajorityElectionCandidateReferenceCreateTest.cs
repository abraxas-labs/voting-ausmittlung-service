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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateReferenceCreateTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionCandidateReferenceCreateTest(TestApplicationFactory factory)
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
    public async Task TestCreateCandidateReference()
    {
        var id = Guid.Parse("2e2d586f-dc6d-45ce-a0b8-39a70d9cf46a");
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateReferenceCreated
            {
                MajorityElectionCandidateReference = new MajorityElectionCandidateReferenceEventData
                {
                    Id = id.ToString(),
                    SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                    Position = 3,
                    Incumbent = false,
                    CandidateId = MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                },
            });

        var candidate = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == id),
            Languages.German);
        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task TestCreateCandidateReferenceAfterSubmissionStarted()
    {
        await MajorityElectionEndResultMockedData.Seed(RunScoped);

        var id = Guid.Parse("4c03f2b6-adde-42d2-a762-d0c065892394");
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateReferenceCreated
            {
                MajorityElectionCandidateReference = new MajorityElectionCandidateReferenceEventData
                {
                    Id = id.ToString(),
                    SecondaryMajorityElectionId = MajorityElectionEndResultMockedData.SecondaryElectionId,
                    Position = 5,
                    Incumbent = false,
                    CandidateId = MajorityElectionEndResultMockedData.CandidateId3,
                },
            });

        var candidate = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates
                .Where(x => x.Id == id)
                .Include(x => x.EndResult)
                .Include(x => x.Translations)
                .FirstAsync(),
            Languages.German);
        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot(c => c.EndResult!.Id, c => c.EndResult!.SecondaryMajorityElectionEndResultId);
    }
}
