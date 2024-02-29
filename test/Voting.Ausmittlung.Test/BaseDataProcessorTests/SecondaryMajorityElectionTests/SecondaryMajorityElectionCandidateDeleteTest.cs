// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateDeleteTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionCandidateDeleteTest(TestApplicationFactory factory)
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
    public async Task TestDeleteCandidate()
    {
        var id = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund;
        await TestEventPublisher.Publish(new SecondaryMajorityElectionCandidateDeleted
        { SecondaryMajorityElectionCandidateId = id });

        var idGuid = Guid.Parse(id);
        var candidate = await RunOnDb(db =>
            db.SecondaryMajorityElectionCandidates.FirstOrDefaultAsync(c => c.Id == idGuid));
        candidate.Should().BeNull();
    }
}
