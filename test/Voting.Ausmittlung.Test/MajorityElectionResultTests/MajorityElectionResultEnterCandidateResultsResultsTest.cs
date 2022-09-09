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
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultEnterCandidateResultsResultsTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultEnterCandidateResultsResultsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ErfassungElectionAdminClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults,
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        });
        await RunEvents<MajorityElectionResultEntryDefined>();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await ErfassungElectionAdminClient.EnterCandidateResultsAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionCandidateResultsEntered>().MatchSnapshot("results");
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultCountOfVotersEntered>().MatchSnapshot("count-of-voters");

        await RunEvents<MajorityElectionCandidateResultsEntered>(false);
        await RunEvents<MajorityElectionResultCountOfVotersEntered>(false);

        await ErfassungElectionAdminClient.EnterCandidateResultsAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await ErfassungElectionAdminClient.EnterCandidateResultsAsync(NewValidRequest());
            return new[]
            {
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionCandidateResultsEntered>(),
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultCountOfVotersEntered>(),
            };
        });
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(
                NewValidRequest(r => r.ElectionResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(
                NewValidRequest(r => r.ElectionResultId = IdBadFormat)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(
                NewValidRequest(r =>
                    r.ElectionResultId = MajorityElectionResultMockedData.IdUzwilElectionResultInContestUzwil)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowUnknownCandidate()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(
                NewValidRequest(r => r.CandidateResults[0].CandidateId = MajorityElectionMockedData.CandidateIdGossauMajorityElectionInContestGossau)),
            StatusCode.InvalidArgument,
            "candidates provided which don't exist");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowUnknownSecondaryResult()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(
                NewValidRequest(r => r.SecondaryElectionCandidateResults[0].SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdKircheMajorityElectionInContestKirche)),
            StatusCode.InvalidArgument,
            "secondary election results provided which don't exist");
    }

    [Fact]
    public async Task TestShouldThrowUnknownSecondaryCandidate()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(
                NewValidRequest(r => r.SecondaryElectionCandidateResults[0].CandidateResults[0].CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateIdKircheMajorityElectionInContestKirche)),
            StatusCode.InvalidArgument,
            "candidates provided which don't exists");
    }

    [Fact]
    public async Task TestShouldThrowIfDetailedResultsEntry()
    {
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
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "candidate results can only be entered if result entry is set to final results");
    }

    [Fact]
    public async Task TestShouldThrowDuplicate()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(
                NewValidRequest(r => r.CandidateResults.Add(r.CandidateResults[0]))),
            StatusCode.InvalidArgument,
            "duplicated candidate provided");
    }

    [Fact]
    public async Task TestShouldThrowSecondaryDuplicate()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(
                NewValidRequest(r => r.SecondaryElectionCandidateResults.Add(r.SecondaryElectionCandidateResults[0]))),
            StatusCode.InvalidArgument,
            "duplicated secondary election result provided");
    }

    [Fact]
    public async Task TestShouldThrowSecondaryDuplicateCandidate()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(
                NewValidRequest(r => r.SecondaryElectionCandidateResults[0].CandidateResults.Add(r.SecondaryElectionCandidateResults[0].CandidateResults[0]))),
            StatusCode.InvalidArgument,
            "duplicated candidate provided");
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
            async () => await ErfassungElectionAdminClient.EnterCandidateResultsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionCandidateResultsEntered
            {
                EmptyVoteCount = 3,
                InvalidVoteCount = 5,
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                CandidateResults =
                {
                        new MajorityElectionCandidateResultCountEventData
                        {
                            VoteCount = 10,
                            CandidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
                        },
                        new MajorityElectionCandidateResultCountEventData
                        {
                            VoteCount = 15,
                            CandidateId = MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                        },
                },
                SecondaryElectionCandidateResults =
                {
                        new SecondaryMajorityElectionCandidateResultsEventData
                        {
                            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                            IndividualVoteCount = 2,
                            CandidateResults =
                            {
                                new MajorityElectionCandidateResultCountEventData
                                {
                                    VoteCount = 11,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                                },
                                new MajorityElectionCandidateResultCountEventData
                                {
                                    VoteCount = 7,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
                                },
                            },
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });

        var candidateResults = await RunOnDb(db => db.MajorityElectionCandidateResults
            .Where(cr => cr.ElectionResultId == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund))
            .ToListAsync());
        candidateResults
            .First(c => c.CandidateId == Guid.Parse(MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund))
            .VoteCount
            .Should()
            .Be(10);
        candidateResults
            .First(c => c.CandidateId == Guid.Parse(MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund))
            .VoteCount
            .Should()
            .Be(15);

        var secondaryCandidateResults = await RunOnDb(db => db.SecondaryMajorityElectionCandidateResults
            .Where(cr => cr.ElectionResult.PrimaryResultId == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund))
            .ToListAsync());
        secondaryCandidateResults
            .First(c => c.CandidateId == Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund))
            .VoteCount
            .Should()
            .Be(11);
        secondaryCandidateResults
            .First(c => c.CandidateId == Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund))
            .VoteCount
            .Should()
            .Be(7);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .EnterCandidateResultsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    private EnterMajorityElectionCandidateResultsRequest NewValidRequest(
        Action<EnterMajorityElectionCandidateResultsRequest>? customizer = null)
    {
        var r = new EnterMajorityElectionCandidateResultsRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            IndividualVoteCount = 1,
            EmptyVoteCount = 5,
            InvalidVoteCount = 8,
            CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
            {
                ConventionalReceivedBallots = 1,
                ConventionalAccountedBallots = 300,
            },
            CandidateResults =
                    {
                        new EnterMajorityElectionCandidateResultRequest
                        {
                            VoteCount = 10,
                            CandidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
                        },
                        new EnterMajorityElectionCandidateResultRequest
                        {
                            VoteCount = 15,
                            CandidateId = MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                        },
                    },
            SecondaryElectionCandidateResults =
                    {
                        new EnterSecondaryMajorityElectionCandidateResultsRequest
                        {
                            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                            InvalidVoteCount = 12,
                            EmptyVoteCount = 13,
                            IndividualVoteCount = 35,
                            CandidateResults =
                            {
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 11,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                                },
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 7,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
                                },
                            },
                        },
                    },
        };
        customizer?.Invoke(r);
        return r;
    }
}
