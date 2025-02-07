// (c) Copyright by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateReferenceUpdateTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionCandidateReferenceUpdateTest(TestApplicationFactory factory)
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
    public async Task TestUpdateCandidateReference()
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateReferenceUpdated
            {
                MajorityElectionCandidateReference = new MajorityElectionCandidateReferenceEventData
                {
                    Id = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                    SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                    CandidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
                    Incumbent = true,
                    Number = "1.2",
                    CheckDigit = 4,
                    Position = 1,
                },
            });
        var candidate = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund)),
            Languages.German);
        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot();
    }

    [Fact]
    public async Task TestUpdateCandidateReferenceOnSeparateBallot()
    {
        await TestEventPublisher.Publish(
            new SecondaryMajorityElectionCandidateReferenceUpdated
            {
                MajorityElectionCandidateReference = new MajorityElectionCandidateReferenceEventData
                {
                    Id = MajorityElectionMockedData.CandidateIdReferencedStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot,
                    SecondaryMajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot,
                    CandidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
                    Incumbent = true,
                    Position = 1,
                    Number = "1.2",
                    CheckDigit = 4,
                    IsOnSeparateBallot = true,
                },
            });
        var candidate = await RunOnDb(
            db => db.MajorityElectionCandidates
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionMockedData.CandidateIdReferencedStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot)),
            Languages.German);
        SetDynamicIdToDefaultValue(candidate.Translations);
        candidate.MatchSnapshot();
    }
}
