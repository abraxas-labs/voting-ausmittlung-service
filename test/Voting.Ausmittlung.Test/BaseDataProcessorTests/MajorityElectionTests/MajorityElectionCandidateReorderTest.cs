// (c) Copyright by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionTests;

public class MajorityElectionCandidateReorderTest : BaseDataProcessorTest
{
    public MajorityElectionCandidateReorderTest(TestApplicationFactory factory)
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
    public async Task TestReorderCandidates()
    {
        await TestEventPublisher.Publish(new MajorityElectionCandidatesReordered
        {
            MajorityElectionId = MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen,
            CandidateOrders = new EntityOrdersEventData
            {
                Orders =
                    {
                        new EntityOrderEventData
                        {
                            Id = MajorityElectionMockedData.CandidateId1GossauMajorityElectionInContestStGallen,
                            Position = 2,
                        },
                        new EntityOrderEventData
                        {
                            Id = MajorityElectionMockedData.CandidateId2GossauMajorityElectionInContestStGallen,
                            Position = 1,
                        },
                    },
            },
        });

        var candidates = await RunOnDb(
            db => db.MajorityElectionCandidates
                .Include(c => c.Translations)
                .Where(c => c.MajorityElectionId ==
                            Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen))
                .OrderBy(x => x.Number)
                .ToListAsync(),
            Languages.German);
        SetDynamicIdToDefaultValue(candidates.SelectMany(c => c.Translations));
        candidates.MatchSnapshot();
    }
}
