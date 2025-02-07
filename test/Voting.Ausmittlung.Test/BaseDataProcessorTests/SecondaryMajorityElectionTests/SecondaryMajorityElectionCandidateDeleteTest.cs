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
        var exists = await RunOnDb(db =>
            db.SecondaryMajorityElectionCandidates.AnyAsync(c => c.Id == Guid.Parse(id)));
        exists.Should().BeTrue();
        await TestEventPublisher.Publish(new SecondaryMajorityElectionCandidateDeleted
        { SecondaryMajorityElectionCandidateId = id });

        exists = await RunOnDb(db =>
            db.SecondaryMajorityElectionCandidates.AnyAsync(c => c.Id == Guid.Parse(id)));
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task TestDeleteCandidateOnSeparateBallot()
    {
        var id = MajorityElectionMockedData.CandidateIdStGallenMajorityElectionInContestStGallenSecondaryOnSeparateBallot;
        var exists = await RunOnDb(db =>
            db.MajorityElectionCandidates.AnyAsync(c => c.Id == Guid.Parse(id)));
        exists.Should().BeTrue();
        await TestEventPublisher.Publish(new SecondaryMajorityElectionCandidateDeleted
        { SecondaryMajorityElectionCandidateId = id, IsOnSeparateBallot = true });

        exists = await RunOnDb(db => db.MajorityElectionCandidates.AnyAsync(c => c.Id == Guid.Parse(id)));
        exists.Should().BeFalse();
    }
}
