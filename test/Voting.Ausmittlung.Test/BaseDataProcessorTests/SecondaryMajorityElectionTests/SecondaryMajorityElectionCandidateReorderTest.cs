// (c) Copyright 2022 by Abraxas Informatik AG
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

public class SecondaryMajorityElectionCandidateReorderTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionCandidateReorderTest(TestApplicationFactory factory)
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
    public async Task TestAggregate()
    {
        await TestEventPublisher.Publish(new SecondaryMajorityElectionCandidatesReordered
        {
            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
            CandidateOrders = new EntityOrdersEventData
            {
                Orders =
                    {
                        new EntityOrderEventData
                        {
                            Id = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                            Position = 2,
                        },
                        new EntityOrderEventData
                        {
                            Id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
                            Position = 1,
                        },
                        new EntityOrderEventData
                        {
                            Id = MajorityElectionMockedData.SecondaryElectionCandidateId3StGallenMajorityElectionInContestBund,
                            Position = 3,
                        },
                        new EntityOrderEventData
                        {
                            Id = MajorityElectionMockedData.SecondaryElectionCandidateId4StGallenMajorityElectionInContestBund,
                            Position = 4,
                        },
                    },
            },
        });

        var candidates = await RunOnDb(
            db => db.SecondaryMajorityElectionCandidates
                .Include(x => x.Translations)
                .Where(x => x.SecondaryMajorityElectionId == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund))
                .OrderBy(x => x.Position)
                .ToListAsync(),
            Languages.German);
        SetDynamicIdToDefaultValue(candidates.SelectMany(x => x.Translations));
        candidates.MatchSnapshot();
    }
}
