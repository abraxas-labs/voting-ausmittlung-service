// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultDefineEntryTest : MajorityElectionResultBaseTest
{
    public MajorityElectionResultDefineEntryTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
    }

    [Fact]
    public async Task TestProcessorDetailedEntry()
    {
        await RunOnDb(async db =>
        {
            db.MajorityElectionResultBundles.Add(new MajorityElectionResultBundle
            {
                Number = 1,
                ElectionResultId = Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund),
            });
            db.MajorityElectionResultBundles.Add(new MajorityElectionResultBundle
            {
                Number = 2,
                ElectionResultId = Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund),
            });
            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultEntryDefined
            {
                ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
                ResultEntryParams = new MajorityElectionResultEntryParamsEventData
                {
                    BallotBundleSize = 10,
                    BallotBundleSampleSize = 5,
                    BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                    AutomaticEmptyVoteCounting = true,
                    AutomaticBallotBundleNumberGeneration = true,
                },
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                EventInfo = GetMockedEventInfo(),
            });

        var entry = await RunOnDb(db => db.MajorityElectionResults
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund)));
        entry.MatchSnapshot(x => x.CountingCircleId);

        await ShouldHaveResettedConventionalResults();
    }

    [Fact]
    public async Task TestProcessorFinalResults()
    {
        await RunOnDb(async db =>
        {
            db.MajorityElectionResultBundles.Add(new MajorityElectionResultBundle
            {
                Number = 1,
                ElectionResultId = Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund),
            });
            db.MajorityElectionResultBundles.Add(new MajorityElectionResultBundle
            {
                Number = 2,
                ElectionResultId = Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund),
            });
            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultEntryDefined
            {
                ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults,
                ResultEntryParams = new MajorityElectionResultEntryParamsEventData
                {
                    BallotBundleSize = 10,
                    BallotBundleSampleSize = 5,
                    BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                    AutomaticEmptyVoteCounting = true,
                    AutomaticBallotBundleNumberGeneration = true,
                },
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                EventInfo = GetMockedEventInfo(),
            });

        var entry = await RunOnDb(db => db.MajorityElectionResults
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund)));
        entry.MatchSnapshot(x => x.CountingCircleId);

        await ShouldHaveResettedConventionalResults();
    }

    [Fact]
    public async Task TestShouldBeOk()
    {
        await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultEntryDefined>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultEntryDefined>();
        });
    }

    [Fact]
    public async Task TestShouldBeOkFinalResults()
    {
        await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest(x =>
        {
            x.ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults;
            x.ResultEntryParams = null;
        }));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultEntryDefined>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await BundErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultEntryDefined>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowIfDetailedWithoutParams()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest(x => x.ResultEntryParams = null)),
            StatusCode.InvalidArgument,
            "details are required if result entry is set to detailed");
    }

    [Fact]
    public async Task TestShouldThrowIfFinalResultsWithParams()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest(x => x.ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults)),
            StatusCode.InvalidArgument,
            "can't provide details if result entry is set to final results");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunOnDb(async db =>
        {
            db.MajorityElectionResultBundles.RemoveRange(db.MajorityElectionResultBundles);
            await db.SaveChangesAsync();
        });
        await RunToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest(x =>
                x.ElectionResultId = MajorityElectionResultMockedData.IdUzwilElectionResultInContestUzwil)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestGreaterBallotBundleSampleSizeShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest(x =>
                x.ResultEntryParams.BallotBundleSampleSize = x.ResultEntryParams.BallotBundleSize + 1)),
            StatusCode.InvalidArgument,
            "'Ballot Bundle Sample Size' must be less than or equal to '10'");
    }

    [Fact]
    public async Task TestNoRespectForEnforcedResultEntrySettingShouldThrow()
    {
        await RunOnDb(async db =>
        {
            var election = await db.MajorityElections
                .AsTracking()
                .FirstAsync(x =>
                    x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund));
            election.EnforceResultEntryForCountingCircles = true;
            election.ResultEntry = MajorityElectionResultEntry.FinalResults;
            await db.SaveChangesAsync();
        });
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "enforced result entry setting not respected");
    }

    [Fact]
    public async Task TestNoRespectForEnforcedEmptyVoteCountingSettingShouldThrow()
    {
        await RunOnDb(async db =>
        {
            var election = await db.MajorityElections
                .AsTracking()
                .FirstAsync(x =>
                    x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund));
            election.EnforceEmptyVoteCountingForCountingCircles = true;
            election.AutomaticEmptyVoteCounting = true;
            await db.SaveChangesAsync();
        });
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "enforced AutomaticEmptyVoteCounting setting not respected");
    }

    [Fact]
    public async Task TestNoRespectForEnforcedReviewProcedureSettingShouldThrow()
    {
        await RunOnDb(async db =>
        {
            var election = await db.MajorityElections
                .AsTracking()
                .FirstAsync(x =>
                    x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund));
            election.EnforceReviewProcedureForCountingCircles = true;
            election.ReviewProcedure = MajorityElectionReviewProcedure.Physically;
            await db.SaveChangesAsync();
        });
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "enforced ReviewProcedure setting not respected");
    }

    [Fact]
    public async Task TestNoRespectForEnforcedCandidateCheckDigitSettingShouldThrow()
    {
        await RunOnDb(async db =>
        {
            var election = await db.MajorityElections
                .AsTracking()
                .FirstAsync(x =>
                    x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund));
            election.EnforceCandidateCheckDigitForCountingCircles = true;
            election.CandidateCheckDigit = false;
            await db.SaveChangesAsync();
        });
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "enforced CandidateCheckDigit setting not respected");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .DefineEntryAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private DefineMajorityElectionResultEntryRequest NewValidRequest(Action<DefineMajorityElectionResultEntryRequest>? customizer = null)
    {
        var r = new DefineMajorityElectionResultEntryRequest
        {
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new DefineMajorityElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotBundleSampleSize = 5,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = false,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
                CandidateCheckDigit = true,
            },
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        };
        customizer?.Invoke(r);
        return r;
    }

    private async Task ShouldHaveResettedConventionalResults()
    {
        var result = await RunOnDb(db => db.MajorityElectionResults
                .AsSplitQuery()
                .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
                .Include(x => x.CandidateResults)
                .Include(x => x.Bundles)
                .Include(x => x.BallotGroupResults)
                .FirstAsync(x => x.Id == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund)));

        // all results except e voting should be reset
        var defaultValue = result.Entry == MajorityElectionResultEntry.Detailed ? 0 : (int?)null;

        var candidateResults = result.CandidateResults.OfType<MajorityElectionCandidateResultBase>()
            .Concat(result.SecondaryMajorityElectionResults.SelectMany(x => x.CandidateResults));

        candidateResults.Any(r => r.ConventionalVoteCount != defaultValue)
            .Should().BeFalse();

        result.BallotGroupResults.Any(r => r.VoteCount != 0)
            .Should().BeFalse();

        result.Bundles.Any()
            .Should().BeFalse();

        result.ConventionalSubTotal.IndividualVoteCount.Should().Be(defaultValue);
        result.ConventionalSubTotal.EmptyVoteCountExclWriteIns.Should().Be(defaultValue);
        result.ConventionalSubTotal.InvalidVoteCount.Should().Be(defaultValue);
        result.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual.Should().Be(0);

        result.SecondaryMajorityElectionResults.Any(r =>
                r.ConventionalSubTotal.IndividualVoteCount != defaultValue ||
                r.ConventionalSubTotal.EmptyVoteCountExclWriteIns != defaultValue ||
                r.ConventionalSubTotal.InvalidVoteCount != defaultValue ||
                r.TotalCandidateVoteCountExclIndividual != 0)
            .Should().BeFalse();
    }
}
