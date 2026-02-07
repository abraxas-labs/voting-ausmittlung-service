// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
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
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionCandidateVotesNotNull && IsInMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotSecondaryCandidateVotesNotNull()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x => x.Request.SecondaryElectionCandidateResults[0].CandidateResults[0].VoteCount = null));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionCandidateVotesNotNull && IsInFirstSecondaryMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenSecondaryCandidateMoreVotesThanPrimary()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x => x.Request.SecondaryElectionCandidateResults[0].CandidateResults[0].VoteCount = x.Request.CandidateResults[0].VoteCount + 1));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionSecondaryReferencedCandidateMoreVotesThanPrimary && IsInFirstSecondaryMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsValidWhenCandidateVotesNotNullWithIndividualVotesDisabled()
    {
        await ModifyDbEntities<MajorityElection>(
            x => x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund),
            x => x.IndividualCandidatesDisabled = true);

        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x => x.Request.IndividualVoteCount = null));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionCandidateVotesNotNull && IsInMajorityElectionGroup(r))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnIsValidWhenSecondaryCandidateVotesNotNullWithIndividualVotesDisabled()
    {
        await ModifyDbEntities<MajorityElection>(
            x => x.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund),
            x => x.IndividualCandidatesDisabled = true);

        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x => x.Request.IndividualVoteCount = null));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionCandidateVotesNotNull && IsInFirstSecondaryMajorityElectionGroup(r))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenEmptyVoteCountNotNull()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x => x.Request.EmptyVoteCount = null));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionEmptyVoteCountNotNull && IsInMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenSecondaryEmptyVoteCountNotNull()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x => x.Request.SecondaryElectionCandidateResults[0].EmptyVoteCount = null));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionEmptyVoteCountNotNull && IsInFirstSecondaryMajorityElectionGroup(r))
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
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionInvalidVoteCountNotNull && IsInMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenSecondaryInvalidVoteCountNotNull()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidBundesurnengang,
            x => x.MajorityElectionInvalidVotes = true,
            true);
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x => x.Request.SecondaryElectionCandidateResults[0].InvalidVoteCount = null));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionInvalidVoteCountNotNull && IsInFirstSecondaryMajorityElectionGroup(r))
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
            x.Request.CountOfVoters.ConventionalAccountedBallots = 255;
            x.Request.CountOfVoters.ConventionalReceivedBallots = 305;
            x.Request.EmptyVoteCount = null;
            x.Request.InvalidVoteCount = 2;
            x.Request.IndividualVoteCount = 48;
            x.Request.SecondaryElectionCandidateResults[0].IndividualVoteCount += 270;
            x.Request.SecondaryElectionCandidateResults[1].IndividualVoteCount += 90;
            x.Request.SecondaryElectionCandidateResults[2].IndividualVoteCount += 180;
        }));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnIsValidWhenSecondaryEmptyVoteCountAreNullWithSingleMandate()
    {
        await ModifyDbEntities<SecondaryMajorityElection>(
            me => me.Id == Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund),
            me => me.NumberOfMandates = 1);

        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
        {
            x.Request.SecondaryElectionCandidateResults[0].EmptyVoteCount = null;
            x.Request.SecondaryElectionCandidateResults[0].InvalidVoteCount = 5;
            x.Request.SecondaryElectionCandidateResults[0].IndividualVoteCount = 115;
            x.Request.SecondaryElectionCandidateResults[0].CandidateResults[0].VoteCount = 15;
            x.Request.SecondaryElectionCandidateResults[0].CandidateResults[1].VoteCount = 10;
            x.Request.SecondaryElectionCandidateResults[0].CandidateResults[2].VoteCount = 20;
            x.Request.SecondaryElectionCandidateResults[0].CandidateResults[3].VoteCount = 0;
        }));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 100));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes && IsInMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotSecondaryNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 50));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes && IsInFirstSecondaryMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsGreaterOrEqualCandidateVotes()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 1));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionAccountedBallotsGreaterOrEqualCandidateVotes && IsInMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotSecondaryAccountedBallotsGreaterOrEqualCandidateVotes()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCandidateResultsAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 1));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionAccountedBallotsGreaterOrEqualCandidateVotes && IsInFirstSecondaryMajorityElectionGroup(r))
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
        yield return RolesMockedData.ErfassungElectionSupporter;
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
                    ConventionalReceivedBallots = 215,
                    ConventionalAccountedBallots = 165,
                    ConventionalBlankBallots = 20,
                    ConventionalInvalidBallots = 30,
                },
                CandidateResults =
                    {
                        new EnterMajorityElectionCandidateResultRequest
                        {
                            VoteCount = 100,
                            CandidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
                        },
                        new EnterMajorityElectionCandidateResultRequest
                        {
                            VoteCount = 105,
                            CandidateId = MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                        },
                    },
                SecondaryElectionCandidateResults =
                    {
                        new EnterSecondaryMajorityElectionCandidateResultsRequest
                        {
                            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                            InvalidVoteCount = 12,
                            EmptyVoteCount = 10,
                            IndividualVoteCount = 35,
                            CandidateResults =
                            {
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 100,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                                },
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 105,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
                                },
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 145,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId3StGallenMajorityElectionInContestBund,
                                },
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 88,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId4StGallenMajorityElectionInContestBund,
                                },
                            },
                        },
                        new EnterSecondaryMajorityElectionCandidateResultsRequest
                        {
                            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund2,
                            InvalidVoteCount = 13,
                            EmptyVoteCount = 12,
                            IndividualVoteCount = 20,
                            CandidateResults =
                            {
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 23,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund2,
                                },
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 97,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund2,
                                },
                            },
                        },
                        new EnterSecondaryMajorityElectionCandidateResultsRequest
                        {
                            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund3,
                            InvalidVoteCount = 10,
                            EmptyVoteCount = 115,
                            IndividualVoteCount = 120,
                            CandidateResults =
                            {
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 30,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund3,
                                },
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 55,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund3,
                                },
                            },
                        },
                    },
            },
        };

        customizer?.Invoke(r);
        return r;
    }

    private bool IsInMajorityElectionGroup(ProtoModels.ValidationResult result)
    {
        return result.ValidationGroup == SharedProto.ValidationGroup.MajorityElection && result.GroupValue == "201: Mw SG de";
    }

    private bool IsInFirstSecondaryMajorityElectionGroup(ProtoModels.ValidationResult result)
    {
        return result.ValidationGroup == SharedProto.ValidationGroup.SecondaryMajorityElection && result.GroupValue == "n1: short de";
    }
}
