// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ContestTests;

public class ContestTestingPhaseEndedTest : ContestProcessorBaseTest
{
    private static readonly Guid _contestId = Guid.Parse(ContestMockedData.IdGossau);

    public ContestTestingPhaseEndedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunOnDb(async db =>
        {
            var result = await db.SimpleCountingCircleResults
                .AsTracking()
                .Include(x => x.Comments)
                .FirstAsync(x => x.PoliticalBusiness!.ContestId == _contestId);
            result.State = CountingCircleResultState.ReadyForCorrection;
            result.HasComments = true;
            result.SubmissionDoneTimestamp = DateTime.SpecifyKind(new DateTime(2020, 2, 2), DateTimeKind.Utc);
            result.Comments!.Add(new()
            {
                Content = "my-comment",
                CreatedAt = DateTime.SpecifyKind(new DateTime(2020, 2, 3), DateTimeKind.Utc),
                CreatedBy = new() { FirstName = "Hans", LastName = "Muster", SecureConnectId = "123" },
                CreatedByMonitoringAuthority = false,
            });

            db.ContestDomainOfInfluenceDetails.Add(new()
            {
                ContestId = _contestId,
                DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(_contestId, Guid.Parse(DomainOfInfluenceMockedData.IdGossau)),
                VotingCards =
                {
                    new()
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                        CountOfReceivedVotingCards = 100,
                    },
                },
                CountOfVotersInformationSubTotals =
                {
                    new()
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 50,
                    },
                },
            });

            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task TestTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(
            new ContestTestingPhaseEnded
            {
                ContestId = ContestMockedData.IdGossau,
            });

        var data = await GetData(
            c => c.Id == _contestId,
            q => q
                .Include(x => x.CountingCircleDetails)
                .ThenInclude(x => x.CountingCircle)
                .Include(x => x.Details));

        foreach (var contest in data)
        {
            contest.DomainOfInfluenceId = Guid.Empty;
            foreach (var detail in contest.CountingCircleDetails)
            {
                detail.CountingCircleId = detail.CountingCircle.BasisCountingCircleId;
                detail.CountingCircle = null!;
            }
        }

        SetDynamicIdToDefaultValue(data.SelectMany(x => x.Translations));
        data.MatchSnapshot("contest");

        var results = await GetResults(_contestId);
        results.MatchSnapshot("results");

        var countingCircles = await RunOnDb(async db => await db.CountingCircles
            .Where(cc => cc.SnapshotContestId == _contestId)
            .ToListAsync());
        countingCircles.All(x => x.MustUpdateContactPersons).Should().BeTrue();

        var simpleResults = await RunOnDb(db => db.SimpleCountingCircleResults.Include(x => x.Comments)
            .Where(x => x.PoliticalBusiness!.ContestId == _contestId)
            .ToListAsync());
        foreach (var simpleResult in simpleResults)
        {
            simpleResult.HasComments.Should().BeFalse();
            simpleResult.Comments.Should().BeEmpty();
            simpleResult.State.Should().Be(CountingCircleResultState.Initial);
            simpleResult.SubmissionDoneTimestamp.Should().BeNull();
        }

        var contestDoiDetailsCount = await RunOnDb(db => db.ContestDomainOfInfluenceDetails.Where(x => x.ContestId == _contestId).CountAsync());
        contestDoiDetailsCount.Should().Be(0);
    }

    [Fact]
    public async Task TestTransientCatchUpInReplay()
    {
        await TestEventPublisher.Publish(
            true,
            new ContestTestingPhaseEnded
            {
                ContestId = _contestId.ToString(),
            });

        var entry = ContestCache.Get(_contestId);
        entry.State.Should().Be(ContestState.Active);
        entry.KeyData.Should().BeNull();
        entry.MatchSnapshot();

        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeySigned>().Should().HaveCount(0);
    }

    [Fact]
    public async Task TestTransientCatchUpInLiveProcessing()
    {
        var entry = ContestCache.Get(_contestId);
        entry.KeyData = null;

        await TestEventPublisher.Publish(
            false,
            new ContestTestingPhaseEnded
            {
                ContestId = _contestId.ToString(),
            });

        entry.State.Should().Be(ContestState.Active);
        entry.KeyData.Should().NotBeNull();
        entry.KeyData!.Key.Id.Should().NotBeNullOrWhiteSpace();
        entry.KeyData.Key.PublicKey.Should().NotBeNullOrEmpty();
        entry.KeyData.Key.PrivateKey.Should().NotBeNullOrEmpty();
        entry.KeyData = null;
        entry.MatchSnapshot("cache-entry");

        var ev = EventPublisherMock.GetSinglePublishedEvent<EventSignaturePublicKeySigned>();
        ev.HsmSignature.Should().NotBeEmpty();
        ev.KeyId.Should().NotBeNullOrWhiteSpace();
        ev.HsmSignature = ByteString.Empty;
        ev.KeyId = string.Empty;
        ev.MatchSnapshot("event");
    }

    private async Task<List<CountingCircleResult>> GetResults(Guid contestId)
    {
        return await RunOnDb(async db =>
        {
            var voteResults = await db.VoteResults
                .Include(x => x.Results)
                .Where(vr => vr.Vote.ContestId == contestId)
                .OrderBy(vr => vr.VoteId)
                .ThenBy(vr => vr.CountingCircle.BasisCountingCircleId)
                .ToListAsync();
            foreach (var voteResult in voteResults)
            {
                voteResult.Id = Guid.Empty;
                voteResult.CountingCircleId = Guid.Empty;
                foreach (var ballotResult in voteResult.Results)
                {
                    ballotResult.Id = Guid.Empty;
                    ballotResult.VoteResultId = Guid.Empty;
                }
            }

            var proportionalElectionResults = await db.ProportionalElectionResults
                .AsSplitQuery()
                .Include(x => x.Bundles)
                .Include(x => x.UnmodifiedListResults)
                .Where(x => x.ProportionalElection.ContestId == contestId)
                .OrderBy(x => x.ProportionalElectionId)
                .ThenBy(x => x.CountingCircle.BasisCountingCircleId)
                .ToListAsync();
            foreach (var proportionalElectionResult in proportionalElectionResults)
            {
                proportionalElectionResult.Id = Guid.Empty;
                proportionalElectionResult.CountingCircleId = Guid.Empty;
                foreach (var unmodifiedListResult in proportionalElectionResult.UnmodifiedListResults)
                {
                    unmodifiedListResult.Id = Guid.Empty;
                    unmodifiedListResult.ResultId = Guid.Empty;
                }
            }

            var majorityElectionResults = await db.MajorityElectionResults
                .AsSplitQuery()
                .Include(x => x.Bundles)
                .Include(x => x.BallotGroupResults)
                .Where(x => x.MajorityElection.ContestId == contestId)
                .OrderBy(x => x.MajorityElectionId)
                .ThenBy(x => x.CountingCircle.BasisCountingCircleId)
                .ToListAsync();
            foreach (var majorityElectionResult in majorityElectionResults)
            {
                majorityElectionResult.Id = Guid.Empty;
                majorityElectionResult.CountingCircleId = Guid.Empty;
                foreach (var unmodifiedListResult in majorityElectionResult.BallotGroupResults)
                {
                    unmodifiedListResult.Id = Guid.Empty;
                    unmodifiedListResult.ElectionResultId = Guid.Empty;
                }
            }

            return voteResults.Cast<CountingCircleResult>()
                .Concat(proportionalElectionResults)
                .Concat(majorityElectionResults)
                .ToList();
        });
    }
}
