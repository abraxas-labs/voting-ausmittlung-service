﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.SecondaryMajorityElectionTests;

public class SecondaryMajorityElectionCandidateReferenceDeleteTest : BaseDataProcessorTest
{
    public SecondaryMajorityElectionCandidateReferenceDeleteTest(TestApplicationFactory factory)
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
    public async Task TestDeleteCandidateReference()
    {
        var id = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund;
        await TestEventPublisher.Publish(new SecondaryMajorityElectionCandidateDeleted
        { SecondaryMajorityElectionCandidateId = id });

        var idGuid = Guid.Parse(id);
        var candidate = await RunOnDb(db =>
            db.SecondaryMajorityElectionCandidates.FirstOrDefaultAsync(c => c.Id == idGuid));
        candidate.Should().BeNull();
    }
}
