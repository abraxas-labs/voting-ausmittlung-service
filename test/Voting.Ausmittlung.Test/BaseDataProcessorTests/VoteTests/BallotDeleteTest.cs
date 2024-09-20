// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.VoteTests;

public class BallotDeleteTest : VoteProcessorBaseTest
{
    public BallotDeleteTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestDeleted()
    {
        await TestEventPublisher.Publish(
            new BallotDeleted
            {
                BallotId = VoteMockedData.BallotIdGossauVoteInContestGossau,
                VoteId = VoteMockedData.IdGossauVoteInContestGossau,
            });

        var data = await GetData(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau));
        data.MatchSnapshot();
    }
}
