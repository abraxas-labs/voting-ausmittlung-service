// (c) Copyright 2024 by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultValidateEnterCountOfVotersTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultValidateEnterCountOfVotersTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);

        await UpdateResult(x =>
        {
            x.ConventionalSubTotal.TotalCountOfUnmodifiedLists = 200;
            x.ConventionalSubTotal.TotalCountOfModifiedLists = 300;
        });

        await ModifyDbEntities(
            (Contest c) => c.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting),
            c => c.EVotingResultsImported = true);
    }

    [Fact]
    public async Task ShouldReturnIsValid()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest());
        result.MatchSnapshot();
        result.IsValid.Should().BeTrue();
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
            x.Request.CountOfVoters.ConventionalAccountedBallots = 22000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessAccountedBallotsLessOrEqualValidVotingCards)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 9000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.PoliticalBusinessAccountedBallotsEqualReceivedMinusBlankMinusInvalidBallots)
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotAccountedBallotsEqualModifiedPlusUnmodifiedLists()
    {
        var result = await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
            x.Request.CountOfVoters.ConventionalAccountedBallots = 2000));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.ProportionalElectionAccountedBallotsEqualModifiedPlusUnmodifiedLists)
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
        var result = await StGallenErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest());
        result.MatchSnapshot();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(NewValidRequest(x =>
                x.Request.ElectionResultId = ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil)),
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

    [Fact]
    public async Task ShouldReturnComparisonVoterParticipations()
    {
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
        var ccId = CountingCircleMockedData.GuidStGallen;
        Func<ProtoModels.ValidationResult, bool> selector = x => x.Validation == SharedProto.Validation.ComparisonVoterParticipations;

        await SetReadModelResultStateForAllResultsInContest(contestId, CountingCircleResultState.SubmissionOngoing);

        var (_, res1) = await ValidateAndSubmitResult(
            Guid.Parse(ProportionalElectionMockedData.IdBundProportionalElectionInContestBund),
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
            Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund),
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
            Guid.Parse(ProportionalElectionMockedData.IdBundProportionalElectionInContestBund),
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
        res.Should().MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .ValidateEnterCountOfVotersAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private async Task<(Guid PbResId, List<ProtoModels.ValidationResult> ValidationResults)> ValidateAndSubmitResult(
        Guid pbId,
        Guid ccId,
        int totalCountOfVoters,
        EnterPoliticalBusinessCountOfVotersRequest countOfVotersProto,
        Func<ProtoModels.ValidationResult, bool> selector)
    {
        var mapper = GetService<TestMapper>();

        var pbRes = await RunOnDb(async db =>
            (await db.ProportionalElectionResults
                .Include(x => x.CountingCircle)
                .ToListAsync())
                .Single(x => x.CountingCircle.BasisCountingCircleId == ccId && x.PoliticalBusinessId == pbId));
        var pbResId = pbRes.Id;

        var pbResRequest = new EnterProportionalElectionCountOfVotersRequest
        {
            ElectionResultId = pbResId.ToString(),
            CountOfVoters = countOfVotersProto,
        };

        var pbResValidateResponse = await StGallenErfassungElectionAdminClient.ValidateEnterCountOfVotersAsync(new ValidateEnterProportionalElectionCountOfVotersRequest
        {
            Request = pbResRequest,
        });

        var countOfVoters = mapper.Map<PoliticalBusinessNullableCountOfVoters>(pbResRequest.CountOfVoters);
        countOfVoters.UpdateVoterParticipation(totalCountOfVoters);

        await ModifyDbEntities<ProportionalElectionResult>(
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

    private ValidateEnterProportionalElectionCountOfVotersRequest NewValidRequest(
        Action<ValidateEnterProportionalElectionCountOfVotersRequest>? customizer = null)
    {
        var r = new ValidateEnterProportionalElectionCountOfVotersRequest
        {
            Request = new EnterProportionalElectionCountOfVotersRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                {
                    ConventionalReceivedBallots = 2000,
                    ConventionalAccountedBallots = 500,
                    ConventionalBlankBallots = 1000,
                    ConventionalInvalidBallots = 500,
                },
            },
        };

        customizer?.Invoke(r);
        return r;
    }

    private async Task UpdateResult(Action<ProportionalElectionResult> customizer)
    {
        await RunOnDb(async db =>
        {
            var result = db.ProportionalElectionResults
                .AsTracking()
                .First(x => x.Id == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen);

            customizer(result);

            await db.SaveChangesAsync();
        });
    }
}
