// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.MockedData.Mapping;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultValidateEnterCountOfVotersTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultValidateEnterCountOfVotersTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);

        await UpdateResult(result =>
        {
            result.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual = 1895;
            result.ConventionalSubTotal.IndividualVoteCount = 5;
            result.ConventionalSubTotal.EmptyVoteCountExclWriteIns = 70;
            result.ConventionalSubTotal.InvalidVoteCount = 30;

            result.ConventionalCountOfDetailedEnteredBallots = 1500;
            result.ConventionalCountOfBallotGroupVotes = 500;

            var candidateResult = result.CandidateResults.First();
            candidateResult.ConventionalVoteCount = 1500;

            var secondaryResults = result.SecondaryMajorityElectionResults.ToList();
            secondaryResults[0].ConventionalSubTotal.TotalCandidateVoteCountExclIndividual += 5986;
            secondaryResults[1].ConventionalSubTotal.TotalCandidateVoteCountExclIndividual += 1994;
            secondaryResults[2].ConventionalSubTotal.TotalCandidateVoteCountExclIndividual += 3700;
        });
    }

    [Fact]
    public async Task ShouldReturnIsValid()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest());
        result.MatchSnapshot();
        result.IsValid.Should().BeTrue();

        result.ValidationResults.Any(r => r.Validation
                is SharedProto.Validation.MajorityElectionCandidateVotesNotNull
                or SharedProto.Validation.MajorityElectionEmptyVoteCountNotNull
                or SharedProto.Validation.MajorityElectionInvalidVoteCountNotNull)
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task ShouldReturnComparisonVoterParticipations()
    {
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
        var ccId = CountingCircleMockedData.GuidStGallen;
        Func<ProtoModels.ValidationResult, bool> selector = x => x.Validation == SharedProto.Validation.ComparisonVoterParticipations;

        await SetReadModelResultStateForAllResultsInContest(contestId, CountingCircleResultState.SubmissionOngoing);

        var (_, res1) = await ValidateAndSubmitResult(
            Guid.Parse(MajorityElectionMockedData.IdBundMajorityElectionInContestBund),
            ccId,
            100,
            new EnterPoliticalBusinessCountOfVotersRequest
            {
                ConventionalAccountedBallots = 50,
                ConventionalBlankBallots = 0,
                ConventionalInvalidBallots = 1,
                ConventionalReceivedBallots = 51,
            },
            selector);

        res1.Any().Should().BeFalse();

        var (_, res2) = await ValidateAndSubmitResult(
            Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund),
            ccId,
            100,
            new EnterPoliticalBusinessCountOfVotersRequest
            {
                ConventionalAccountedBallots = 60,
                ConventionalBlankBallots = 0,
                ConventionalInvalidBallots = 0,
                ConventionalReceivedBallots = 60,
            },
            selector);

        res2.Should().HaveCount(1);
        res2.MatchSnapshot("result2");
    }

    [Fact]
    public async Task ShouldReturnComparisonVotingCardsAndValidBallots()
    {
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
        var ccId = CountingCircleMockedData.GuidStGallen;

        await SetReadModelResultStateForAllResultsInContest(contestId, CountingCircleResultState.SubmissionOngoing);

        var (_, res) = await ValidateAndSubmitResult(
            Guid.Parse(MajorityElectionMockedData.IdBundMajorityElectionInContestBund),
            ccId,
            100,
            new EnterPoliticalBusinessCountOfVotersRequest
            {
                ConventionalAccountedBallots = 90,
                ConventionalBlankBallots = 0,
                ConventionalInvalidBallots = 1,
                ConventionalReceivedBallots = 91,
            },
            x => x.Validation == SharedProto.Validation.ComparisonValidVotingCardsWithAccountedBallots);

        res.Should().HaveCount(1);
        res.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotReceivedBallotsLessOrEqualValidVotingCards()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalReceivedBallots = 20000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessReceivedBallotsLessOrEqualValidVotingCards)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsLessOrEqualValidVotingCards()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 20000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessAccountedBallotsLessOrEqualValidVotingCards)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 6000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 100));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes && IsInMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotSecondaryNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 100));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes && IsInFirstSecondaryMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsGreaterOrEqualCandidateVotes()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 200));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionAccountedBallotsGreaterOrEqualCandidateVotes && IsInMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotSecondaryAccountedBallotsGreaterOrEqualCandidateVotes()
    {
        await ModifyDbEntities<SecondaryMajorityElectionCandidateResult>(
            x => x.CandidateId == Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund),
            x => x.ConventionalVoteCount = 100);

        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 25));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.MajorityElectionAccountedBallotsGreaterOrEqualCandidateVotes && IsInFirstSecondaryMajorityElectionGroup(r))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotBundlesNotInProcess()
    {
        await UpdateResult(x => x.CountOfBundlesNotReviewedOrDeleted = 1);
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest());
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessBundlesNotInProcess)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsValidAsContestManagerDuringTestingPhase()
    {
        var result = await BundErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest());
        result.MatchSnapshot();
        result.IsValid.Should().BeTrue();

        result.ValidationResults.Any(r => r.Validation
                is SharedProto.Validation.MajorityElectionCandidateVotesNotNull
                or SharedProto.Validation.MajorityElectionEmptyVoteCountNotNull
                or SharedProto.Validation.MajorityElectionInvalidVoteCountNotNull)
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
                x.Request.ElectionResultId = MajorityElectionResultMockedData.IdUzwilElectionResultInContestUzwil)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
                x.Request.ElectionResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .ValidateEnterCountOfVotersAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    private ValidateEnterMajorityElectionCountOfVotersRequest NewValidRequest(
        Action<ValidateEnterMajorityElectionCountOfVotersRequest>? customizer = null)
    {
        var r = new ValidateEnterMajorityElectionCountOfVotersRequest
        {
            Request = new EnterMajorityElectionCountOfVotersRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                {
                    ConventionalReceivedBallots = 3000,
                    ConventionalAccountedBallots = 2000,
                    ConventionalBlankBallots = 750,
                    ConventionalInvalidBallots = 250,
                },
            },
        };

        customizer?.Invoke(r);
        return r;
    }

    private async Task UpdateResult(Action<MajorityElectionResult> customizer)
    {
        await RunOnDb(async db =>
        {
            var result = db.MajorityElectionResults
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.CandidateResults)
                .Include(x => x.SecondaryMajorityElectionResults.OrderBy(y => y.SecondaryMajorityElection.PoliticalBusinessNumber))
                .ThenInclude(x => x.CandidateResults)
                .First(x => x.Id == MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund);

            customizer(result);

            await db.SaveChangesAsync();
        });
    }

    private async Task<(Guid PbResId, List<ProtoModels.ValidationResult> ValidationResults)> ValidateAndSubmitResult(
        Guid pbId,
        Guid ccId,
        int totalCountOfVoters,
        EnterPoliticalBusinessCountOfVotersRequest countOfVotersProto,
        Func<ProtoModels.ValidationResult, bool> selector)
    {
        var mapper = GetService<TestMapper>();

        var pbRes = await RunOnDb(async db =>
            (await db.MajorityElectionResults
                .Include(x => x.CountingCircle)
                .ToListAsync())
                .Single(x => x.CountingCircle.BasisCountingCircleId == ccId && x.PoliticalBusinessId == pbId));
        var pbResId = pbRes.Id;

        var pbResRequest = new EnterMajorityElectionCountOfVotersRequest
        {
            ElectionResultId = pbResId.ToString(),
            CountOfVoters = countOfVotersProto,
        };

        var pbResValidateResponse = await StGallenErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(new ValidateEnterMajorityElectionCountOfVotersRequest
        {
            Request = pbResRequest,
        });

        var countOfVoters = mapper.Map<PoliticalBusinessNullableCountOfVoters>(pbResRequest.CountOfVoters);
        countOfVoters.UpdateVoterParticipation(totalCountOfVoters);

        await ModifyDbEntities<MajorityElectionResult>(
            x => x.Id == pbResId,
            x =>
            {
                x.State = CountingCircleResultState.SubmissionDone;
                x.CountOfVoters = countOfVoters;
            });

        return (
            pbResId,
            pbResValidateResponse.ValidationResults
                .Where(selector)
                .ToList());
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
