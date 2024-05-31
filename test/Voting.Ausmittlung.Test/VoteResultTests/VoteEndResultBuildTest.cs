// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteEndResultBuildTest : VoteEndResultBaseTest
{
    public VoteEndResultBuildTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestBuildPopularMajorityEndResult()
    {
        await SeedVote(VoteResultAlgorithm.PopularMajority);
        await StartResultSubmissions();
        await FinishAllResultSubmission();

        var initEndResult = await GetEndResult();
        initEndResult.MatchSnapshot("init");

        await SetAllAuditedTentatively();
        var afterAllAudited = await GetEndResult();
        afterAllAudited.MatchSnapshot("afterAllAudited");
    }

    [Fact]
    public async Task TestBuildCountingCircleUnanimityEndResult()
    {
        await SeedVote(VoteResultAlgorithm.CountingCircleUnanimity);
        await StartResultSubmissions();

        await FinishResultSubmission(VoteEndResultMockedData.StGallenHaggenResultId);
        await FinishResultSubmission(VoteEndResultMockedData.StGallenStFidenResultId);
        await FinishResultSubmission(VoteEndResultMockedData.StGallenAuslandschweizerResultId);
        await FinishResultSubmission(VoteEndResultMockedData.GossauResultId);
        await FinishResultSubmission(
            VoteEndResultMockedData.StGallenResultId,
            (40, 20),
            (15, 10),
            (10, 15));
        await FinishResultSubmission(
            VoteEndResultMockedData.UzwilResultId,
            (0, 20),
            (15, 10),
            (15, 10));

        var initEndResult = await GetEndResult();
        initEndResult.MatchSnapshot("init");

        await SetAllAuditedTentatively();

        var afterAudited = await GetEndResult();
        afterAudited.MatchSnapshot("afterAudited");

        var ballot2Questions = await RunOnDb(db => db.BallotQuestionEndResults
            .Where(x => x.BallotEndResult.BallotId == Guid.Parse(VoteEndResultMockedData.BallotId2))
            .OrderBy(x => x.Question.Number)
            .ToListAsync());
        var ballot2TieBreakQuestions = await RunOnDb(db => db.TieBreakQuestionEndResults
            .Where(x => x.BallotEndResult.BallotId == Guid.Parse(VoteEndResultMockedData.BallotId2))
            .OrderBy(x => x.Question.Number)
            .ToListAsync());

        ballot2Questions[0].HasCountingCircleUnanimity.Should().BeTrue();
        ballot2Questions[0].Accepted.Should().BeTrue();

        ballot2Questions[1].HasCountingCircleUnanimity.Should().BeFalse();
        ballot2Questions[1].Accepted.Should().BeFalse();

        ballot2TieBreakQuestions[0].HasCountingCircleQ1Majority.Should().BeTrue();
        ballot2TieBreakQuestions[0].HasCountingCircleQ2Majority.Should().BeFalse();
        ballot2TieBreakQuestions[0].Q1Accepted.Should().BeTrue();
    }

    [Fact]
    public async Task TestBuildCountingCircleMajorityEndResult()
    {
        await SeedVote(VoteResultAlgorithm.CountingCircleMajority);
        await StartResultSubmissions();

        await FinishResultSubmission(VoteEndResultMockedData.GossauResultId);
        await FinishResultSubmission(VoteEndResultMockedData.StGallenStFidenResultId);
        await FinishResultSubmission(VoteEndResultMockedData.StGallenHaggenResultId);
        await FinishResultSubmission(
            VoteEndResultMockedData.StGallenAuslandschweizerResultId,
            (40, 20),
            (10, 15),
            (10, 15));
        await FinishResultSubmission(
            VoteEndResultMockedData.StGallenResultId,
            (40, 20),
            (10, 15),
            (10, 15));
        await FinishResultSubmission(
            VoteEndResultMockedData.UzwilResultId,
            (0, 20),
            (10, 15),
            (15, 10));

        var initEndResult = await GetEndResult();
        initEndResult.MatchSnapshot("init");

        await SetAllAuditedTentatively();

        var afterAudited = await GetEndResult();
        afterAudited.MatchSnapshot("afterAudited");

        var ballot2Questions = await RunOnDb(db => db.BallotQuestionEndResults
            .Where(x => x.BallotEndResult.BallotId == Guid.Parse(VoteEndResultMockedData.BallotId2))
            .OrderBy(x => x.Question.Number)
            .ToListAsync());
        var ballot2TieBreakQuestions = await RunOnDb(db => db.TieBreakQuestionEndResults
            .Where(x => x.BallotEndResult.BallotId == Guid.Parse(VoteEndResultMockedData.BallotId2))
            .OrderBy(x => x.Question.Number)
            .ToListAsync());

        ballot2Questions[0].HasCountingCircleMajority.Should().BeFalse();
        ballot2Questions[0].Accepted.Should().BeFalse();

        ballot2Questions[1].HasCountingCircleMajority.Should().BeTrue();
        ballot2Questions[1].Accepted.Should().BeTrue();

        ballot2TieBreakQuestions[0].HasCountingCircleQ1Majority.Should().BeTrue();
        ballot2TieBreakQuestions[0].HasCountingCircleQ2Majority.Should().BeFalse();
        ballot2TieBreakQuestions[0].Q1Accepted.Should().BeTrue();
    }

    [Fact]
    public async Task TestBuildPopularAndCountingCircleMajorityEndResult()
    {
        await SeedVote(VoteResultAlgorithm.PopularAndCountingCircleMajority);
        await StartResultSubmissions();

        await FinishResultSubmission(VoteEndResultMockedData.GossauResultId);
        await FinishResultSubmission(VoteEndResultMockedData.StGallenStFidenResultId);
        await FinishResultSubmission(VoteEndResultMockedData.StGallenHaggenResultId);
        await FinishResultSubmission(
            VoteEndResultMockedData.StGallenAuslandschweizerResultId,
            (40, 20),
            (10, 15),
            (10, 15));
        await FinishResultSubmission(
            VoteEndResultMockedData.StGallenResultId,
            (40, 20),
            (10, 15),
            (10, 15));
        await FinishResultSubmission(
            VoteEndResultMockedData.UzwilResultId,
            (0, 100),
            (11, 15),
            (15, 10));

        var initEndResult = await GetEndResult();
        initEndResult.MatchSnapshot("init");

        await SetAllAuditedTentatively();

        var afterAudited = await GetEndResult();
        afterAudited.MatchSnapshot("afterAudited");

        var ballot1Questions = await RunOnDb(db => db.BallotQuestionEndResults
            .Where(x => x.BallotEndResult.BallotId == Guid.Parse(VoteEndResultMockedData.BallotId1))
            .OrderBy(x => x.Question.Number)
            .ToListAsync());

        var ballot2Questions = await RunOnDb(db => db.BallotQuestionEndResults
            .Where(x => x.BallotEndResult.BallotId == Guid.Parse(VoteEndResultMockedData.BallotId2))
            .OrderBy(x => x.Question.Number)
            .ToListAsync());
        var ballot2TieBreakQuestions = await RunOnDb(db => db.TieBreakQuestionEndResults
            .Where(x => x.BallotEndResult.BallotId == Guid.Parse(VoteEndResultMockedData.BallotId2))
            .OrderBy(x => x.Question.Number)
            .ToListAsync());

        ballot1Questions[0].HasCountingCircleMajority.Should().BeTrue();
        ballot1Questions[0].Accepted.Should().BeFalse();

        ballot2Questions[0].HasCountingCircleMajority.Should().BeFalse();
        ballot2Questions[0].Accepted.Should().BeFalse();

        ballot2Questions[1].HasCountingCircleMajority.Should().BeTrue();
        ballot2Questions[1].Accepted.Should().BeTrue();

        ballot2TieBreakQuestions[0].HasCountingCircleQ1Majority.Should().BeTrue();
        ballot2TieBreakQuestions[0].HasCountingCircleQ2Majority.Should().BeFalse();
        ballot2TieBreakQuestions[0].Q1Accepted.Should().BeTrue();
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        // Skip this test, tested in another class
        yield break;
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        // Skip this test, tested in another class
        yield break;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultService.VoteResultServiceClient(channel)
            .GetEndResultAsync(NewValidRequest());
    }

    private async Task<ProtoModels.VoteEndResult> GetEndResult()
    {
        return await MonitoringElectionAdminClient.GetEndResultAsync(NewValidRequest());
    }

    private GetVoteEndResultRequest NewValidRequest()
    {
        return new GetVoteEndResultRequest
        {
            VoteId = VoteEndResultMockedData.VoteId,
        };
    }
}
