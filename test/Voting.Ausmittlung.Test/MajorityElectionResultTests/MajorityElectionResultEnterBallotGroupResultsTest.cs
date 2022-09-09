// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultEnterBallotGroupResultsTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultEnterBallotGroupResultsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ErfassungElectionAdminClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            ResultEntryParams = new DefineMajorityElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSampleSize = 2,
                AutomaticBallotBundleNumberGeneration = true,
            },
        });
        await RunEvents<MajorityElectionResultEntryDefined>();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await ErfassungElectionAdminClient.EnterBallotGroupResultsAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionBallotGroupResultsEntered>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await ErfassungElectionAdminClient.EnterBallotGroupResultsAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionBallotGroupResultsEntered>();
        });
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterBallotGroupResultsAsync(
                NewValidRequest(r => r.ElectionResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterBallotGroupResultsAsync(
                NewValidRequest(r => r.ElectionResultId = IdBadFormat)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterBallotGroupResultsAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterBallotGroupResultsAsync(
                NewValidRequest(r =>
                    r.ElectionResultId = MajorityElectionResultMockedData.IdUzwilElectionResultInContestUzwil)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowIfFinalResultsEntry()
    {
        await ErfassungElectionAdminClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults,
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        });
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterBallotGroupResultsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "this is only allowed for detailed result entry");
    }

    [Fact]
    public async Task TestShouldThrowDuplicate()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterBallotGroupResultsAsync(
                NewValidRequest(r => r.Results.Add(r.Results[0]))),
            StatusCode.InvalidArgument,
            "duplicated ballot groups provided");
    }

    [Fact]
    public async Task TestShouldThrowUnknownBallotGroup()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterBallotGroupResultsAsync(
                NewValidRequest(r => r.Results[0].BallotGroupId = MajorityElectionMockedData.BallotGroupIdKircheMajorityElectionInContestKirche)),
            StatusCode.InvalidArgument,
            "ballot groups provided which don't exist");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterBallotGroupResultsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionBallotGroupResultsEntered
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                Results =
                {
                        new MajorityElectionBallotGroupResultEventData
                        {
                            VoteCount = 100,
                            BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
        var ballotGroupResults = await ErfassungElectionAdminClient.GetBallotGroupsAsync(
            new GetMajorityElectionBallotGroupResultsRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            });
        ballotGroupResults.BallotGroupResults
            .First(x => x.BallotGroup.Id == MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund)
            .VoteCount
            .Should()
            .Be(100);

        var electionResult = await ErfassungElectionAdminClient.GetAsync(new GetMajorityElectionResultRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        });
        electionResult.ConventionalCountOfBallotGroupVotes.Should().Be(100);
        electionResult.ConventionalSubTotal.TotalCandidateVoteCountInclIndividual.Should().Be(100);
        electionResult.CandidateResults
            .First(c => c.Candidate.Id == MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund)
            .VoteCount
            .Should()
            .Be(100);
        electionResult.SecondaryMajorityElectionResults
            .SelectMany(r => r.CandidateResults)
            .First(c => c.Candidate.Id == MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund)
            .VoteCount
            .Should()
            .Be(100);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionBallotGroupResultsEntered
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                Results =
                {
                        new MajorityElectionBallotGroupResultEventData
                        {
                            VoteCount = 110,
                            BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
        ballotGroupResults = await ErfassungElectionAdminClient.GetBallotGroupsAsync(
            new GetMajorityElectionBallotGroupResultsRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            });
        ballotGroupResults.BallotGroupResults
            .First(x => x.BallotGroup.Id == MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund)
            .VoteCount
            .Should()
            .Be(110);

        electionResult = await ErfassungElectionAdminClient.GetAsync(new GetMajorityElectionResultRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        });
        electionResult.ConventionalSubTotal.TotalCandidateVoteCountInclIndividual.Should().Be(110);
        electionResult.CandidateResults
            .First(c => c.Candidate.Id == MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund)
            .VoteCount
            .Should()
            .Be(110);
        electionResult.SecondaryMajorityElectionResults
            .SelectMany(r => r.CandidateResults)
            .First(c => c.Candidate.Id == MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund)
            .VoteCount
            .Should()
            .Be(110);
        electionResult.ConventionalCountOfBallotGroupVotes.Should().Be(110);
    }

    [Fact]
    public async Task TestProcessorWithIndividualCandidate()
    {
        var electionResultId = MajorityElectionResultMockedData.IdUzwilElectionResultInContestStGallen;
        var ballotGroupId = MajorityElectionMockedData.BallotGroupIdUzwilMajorityElectionInContestStGallen;
        var primaryCandidateId = MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen;
        var secondaryCandidateId = MajorityElectionMockedData.SecondaryElectionCandidateIdUzwilMajorityElectionInContestStGallen;

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionBallotGroupResultsEntered
            {
                ElectionResultId = electionResultId,
                Results =
                {
                        new MajorityElectionBallotGroupResultEventData
                        {
                            VoteCount = 100,
                            BallotGroupId = ballotGroupId,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
        var ballotGroupResults = await ErfassungElectionAdminClient.GetBallotGroupsAsync(
            new GetMajorityElectionBallotGroupResultsRequest
            {
                ElectionResultId = electionResultId,
            });
        ballotGroupResults.BallotGroupResults
            .First(x => x.BallotGroup.Id == ballotGroupId)
            .VoteCount
            .Should()
            .Be(100);

        var electionResult = await ErfassungElectionAdminClient.GetAsync(new GetMajorityElectionResultRequest
        {
            ElectionResultId = electionResultId,
        });
        electionResult.ConventionalCountOfBallotGroupVotes.Should().Be(100);
        electionResult.IndividualVoteCount.Should().Be(100);
        electionResult.CandidateResults
            .First(c => c.Candidate.Id == primaryCandidateId)
            .VoteCount
            .Should()
            .Be(100);
        electionResult.SecondaryMajorityElectionResults
            .SelectMany(r => r.CandidateResults)
            .First(c => c.Candidate.Id == secondaryCandidateId)
            .VoteCount
            .Should()
            .Be(100);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionBallotGroupResultsEntered
            {
                ElectionResultId = electionResultId,
                Results =
                {
                        new MajorityElectionBallotGroupResultEventData
                        {
                            VoteCount = 110,
                            BallotGroupId = ballotGroupId,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
        ballotGroupResults = await ErfassungElectionAdminClient.GetBallotGroupsAsync(
            new GetMajorityElectionBallotGroupResultsRequest
            {
                ElectionResultId = electionResultId,
            });
        ballotGroupResults.BallotGroupResults
            .First(x => x.BallotGroup.Id == ballotGroupId)
            .VoteCount
            .Should()
            .Be(110);

        electionResult = await ErfassungElectionAdminClient.GetAsync(new GetMajorityElectionResultRequest
        {
            ElectionResultId = electionResultId,
        });
        electionResult.CandidateResults
            .First(c => c.Candidate.Id == primaryCandidateId)
            .VoteCount
            .Should()
            .Be(110);
        electionResult.SecondaryMajorityElectionResults
            .SelectMany(r => r.CandidateResults)
            .First(c => c.Candidate.Id == secondaryCandidateId)
            .VoteCount
            .Should()
            .Be(110);
        electionResult.ConventionalCountOfBallotGroupVotes.Should().Be(110);
        electionResult.IndividualVoteCount.Should().Be(110);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .EnterBallotGroupResultsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    private EnterMajorityElectionBallotGroupResultsRequest NewValidRequest(
        Action<EnterMajorityElectionBallotGroupResultsRequest>? customizer = null)
    {
        var r = new EnterMajorityElectionBallotGroupResultsRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            Results =
                {
                    new EnterMajorityElectionBallotGroupResultRequest
                    {
                        BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                        VoteCount = 103,
                    },
                },
        };
        customizer?.Invoke(r);
        return r;
    }
}
