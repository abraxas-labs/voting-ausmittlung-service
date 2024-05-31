// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
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

public class MajorityElectionResultValidateEnterCandidateResultsTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultValidateEnterCandidateResultsTest(TestApplicationFactory factory)
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
        await ModifyDbEntities<MajorityElection>(
            me => me.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund),
            me => me.NumberOfMandates = 2);
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidBundesurnengang,
            x => x.MajorityElectionInvalidVotes = true,
            true);
    }

    [Fact]
    public async Task ShouldReturnIsValid()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidBundesurnengang,
            x => x.MajorityElectionInvalidVotes = true,
            true);
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest());
        result.MatchSnapshot();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotReceivedBallotsLessOrEqualValidVotingCards()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalReceivedBallots = 20000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessReceivedBallotsLessOrEqualValidVotingCards)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsLessOrEqualValidVotingCards()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 20000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessAccountedBallotsLessOrEqualValidVotingCards)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 6000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotCandidateVotesNotNull()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x => x.Request.CandidateResults[0].VoteCount = null));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionCandidateVotesNotNull)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenEmptyVoteCountNotNull()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x => x.Request.EmptyVoteCount = null));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionEmptyVoteCountNotNull)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenInvalidVoteCountNotNull()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidBundesurnengang,
            x => x.MajorityElectionInvalidVotes = true,
            true);
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x => x.Request.InvalidVoteCount = null));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionInvalidVoteCountNotNull)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsValidWhenEmptyVoteCountAreNullWithSingleMandate()
    {
        await ModifyDbEntities<MajorityElection>(
            me => me.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund),
            me => me.NumberOfMandates = 1);

        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
        {
            x.Request.EmptyVoteCount = null;
            x.Request.InvalidVoteCount = 2;
            x.Request.IndividualVoteCount = 48;
        }));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 100));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsGreaterOrEqualCandidateVotes()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 1));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionAccountedBallotsGreaterOrEqualCandidateVotes)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNoCandidatesExist()
    {
        await RunOnDb(async db =>
        {
            var majorityElection = await db.MajorityElections
                .AsTracking()
                .Include(x => x.MajorityElectionCandidates)
                .FirstAsync(x =>
                    x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund));
            majorityElection.MajorityElectionCandidates.Clear();
            await db.SaveChangesAsync();
        });

        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
        {
            x.Request.CandidateResults.Clear();
            x.Request.SecondaryElectionCandidateResults[0].CandidateResults.Clear();
        }));

        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionHasCandidates)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsValidAsContestManagerDuringTestingPhase()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidBundesurnengang,
            x => x.MajorityElectionInvalidVotes = true,
            true);
        var result = await BundErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest());
        result.MatchSnapshot();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
                x.Request.ElectionResultId = MajorityElectionResultMockedData.IdUzwilElectionResultInContestUzwil)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
                x.Request.ElectionResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .ValidateEnterCandidateResultsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    private ValidateEnterMajorityElectionCandidateResultsRequest NewValidRequest(
        Action<ValidateEnterMajorityElectionCandidateResultsRequest>? customizer = null)
    {
        var r = new ValidateEnterMajorityElectionCandidateResultsRequest
        {
            Request = new EnterMajorityElectionCandidateResultsRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                IndividualVoteCount = 10,
                EmptyVoteCount = 38,
                InvalidVoteCount = 77,
                CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                {
                    ConventionalReceivedBallots = 125,
                    ConventionalAccountedBallots = 75,
                    ConventionalBlankBallots = 20,
                    ConventionalInvalidBallots = 30,
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
            },
        };

        customizer?.Invoke(r);
        return r;
    }
}
