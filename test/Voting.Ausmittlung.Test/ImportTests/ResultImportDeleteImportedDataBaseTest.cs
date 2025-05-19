// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using FluentAssertions;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Ausmittlung.Test.ImportTests;

public abstract class ResultImportDeleteImportedDataBaseTest : BaseTest<ResultImportService.ResultImportServiceClient>
{
    private readonly VotingDataSource _dataSource;

    protected ResultImportDeleteImportedDataBaseTest(
        VotingDataSource dataSource,
        TestApplicationFactory factory)
        : base(factory)
    {
        _dataSource = dataSource;
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);
        await ProportionalElectionResultBallotMockedData.Seed(RunScoped);
        await ProportionalElectionUnmodifiedListResultMockedData.Seed(RunScoped);
        await PermissionMockedData.Seed(RunScoped);
    }

    protected override GrpcChannel CreateGrpcChannel(
        bool authorize = true,
        string? tenant = "000000000000000000",
        string? userId = "default-user-id",
        params string[] roles)
        => base.CreateGrpcChannel(authorize, SecureConnectTestDefaults.MockedTenantUzwil.Id, userId, roles);

    protected Task SetMajorityElectionResultState(Guid contestId, Guid resultId, CountingCircleResultState state)
        => SetResultState<MajorityElectionResultAggregate, MajorityElectionResult>(contestId, resultId, state);

    protected Task SetProportionalElectionResultState(Guid contestId, Guid resultId, CountingCircleResultState state)
        => SetResultState<ProportionalElectionResultAggregate, ProportionalElectionResult>(contestId, resultId, state);

    protected Task SetVoteResultState(Guid contestId, Guid resultId, CountingCircleResultState state)
        => SetResultState<VoteResultAggregate, VoteResult>(contestId, resultId, state);

    protected async Task AssertProportionalElectionResultZero(Guid electionId)
    {
        var proportionalElection = await RunOnDb(
            db => db.ProportionalElections
                .AsSplitQuery()
                .Include(x => x.EndResult!)
                .ThenInclude(x => x.ListEndResults)
                .ThenInclude(x => x.CandidateEndResults)
                .Include(x => x.Results)
                .ThenInclude(x => x.ListResults)
                .ThenInclude(x => x.CandidateResults)
                .FirstAsync(x => x.Id == electionId),
            Languages.German);
        proportionalElection.EndResult!.CountOfVoters.GetSubTotal(_dataSource).ReceivedBallots.Should().Be(0);

        var endResultSubTotal = proportionalElection.EndResult.GetSubTotal(_dataSource);
        endResultSubTotal.TotalCountOfBlankRowsOnListsWithoutParty.Should().Be(0);
        endResultSubTotal.TotalCountOfLists.Should().Be(0);
        foreach (var listResult in proportionalElection.EndResult!.ListEndResults)
        {
            listResult.GetSubTotal(_dataSource).TotalVoteCount.Should().Be(0);

            foreach (var candidateEndResult in listResult.CandidateEndResults)
            {
                candidateEndResult.GetSubTotal(_dataSource).VoteCount.Should().Be(0);
            }
        }

        foreach (var result in proportionalElection.Results)
        {
            result.CountOfVoters.GetNonNullableSubTotal(_dataSource).ReceivedBallots.Should().Be(0);
            result.GetSubTotal(_dataSource).TotalCountOfBlankRowsOnListsWithoutParty.Should().Be(0);
            result.GetSubTotal(_dataSource).TotalCountOfLists.Should().Be(0);
            foreach (var listResult in result.ListResults)
            {
                listResult.GetSubTotal(_dataSource).TotalVoteCount.Should().Be(0);

                foreach (var candidateResult in listResult.CandidateResults)
                {
                    candidateResult.GetSubTotal(_dataSource).VoteCount.Should().Be(0);
                }
            }
        }
    }

    protected async Task AssertMajorityElectionResultZero(Guid electionId)
    {
        var majorityElection = await RunOnDb(
            db => db.MajorityElections
                .AsSplitQuery()
                .Include(x => x.EndResult!)
                .ThenInclude(x => x.CandidateEndResults)
                .Include(x => x.Results)
                .ThenInclude(x => x.CandidateResults)
                .Include(x => x.Results)
                .ThenInclude(x => x.WriteInMappings)
                .Include(x => x.Results)
                .ThenInclude(x => x.WriteInBallots)
                .ThenInclude(x => x.WriteInPositions)
                .FirstAsync(x => x.Id == electionId),
            Languages.German);
        majorityElection.EndResult!.CountOfVoters.GetSubTotal(_dataSource).ReceivedBallots.Should().Be(0);

        var endResultSubTotal = majorityElection.EndResult!.GetSubTotal(_dataSource);
        endResultSubTotal.TotalCandidateVoteCountInclIndividual.Should().Be(0);
        endResultSubTotal.InvalidVoteCount.Should().Be(0);
        endResultSubTotal.EmptyVoteCountInclWriteIns.Should().Be(0);
        foreach (var candidateResult in majorityElection.EndResult!.CandidateEndResults)
        {
            candidateResult.GetVoteCountOfDataSource(_dataSource).Should().Be(0);
        }

        foreach (var result in majorityElection.Results)
        {
            result.CountOfVoters.GetNonNullableSubTotal(_dataSource).ReceivedBallots.Should().Be(0);
            result.WriteInMappings.Should().HaveCount(0);
            result.WriteInBallots.Should().HaveCount(0);

            var subTotal = result.GetNonNullableSubTotal(_dataSource);
            subTotal.TotalCandidateVoteCountInclIndividual.Should().Be(0);
            subTotal.InvalidVoteCount.Should().Be(0);
            subTotal.EmptyVoteCountInclWriteIns.Should().Be(0);
            foreach (var candidateResult in result.CandidateResults)
            {
                candidateResult.GetVoteCountOfDataSource(_dataSource).Should().Be(0);
            }
        }
    }

    protected async Task AssertVoteResultZero(Guid voteId)
    {
        var vote = await RunOnDb(
            db => db.Votes
                .AsSplitQuery()
                .Include(x => x.EndResult!)
                .ThenInclude(x => x.BallotEndResults)
                .ThenInclude(x => x.QuestionEndResults)
                .Include(x => x.EndResult!)
                .ThenInclude(x => x.BallotEndResults)
                .ThenInclude(x => x.TieBreakQuestionEndResults)
                .Include(x => x.Results)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.QuestionResults)
                .Include(x => x.Results)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.TieBreakQuestionResults)
                .FirstAsync(x => x.Id == voteId),
            Languages.German);
        foreach (var ballotEndResult in vote.EndResult!.BallotEndResults)
        {
            ballotEndResult.CountOfVoters.GetSubTotal(_dataSource).ReceivedBallots.Should().Be(0);
            foreach (var questionEndResult in ballotEndResult.QuestionEndResults)
            {
                var subTotal = questionEndResult.GetSubTotal(_dataSource);
                subTotal.TotalCountOfAnswerYes.Should().Be(0);
                subTotal.TotalCountOfAnswerNo.Should().Be(0);
                subTotal.TotalCountOfAnswerUnspecified.Should().Be(0);
            }

            foreach (var questionEndResult in ballotEndResult.TieBreakQuestionEndResults)
            {
                var subTotal = questionEndResult.GetSubTotal(_dataSource);
                subTotal.TotalCountOfAnswerQ1.Should().Be(0);
                subTotal.TotalCountOfAnswerQ2.Should().Be(0);
                subTotal.TotalCountOfAnswerUnspecified.Should().Be(0);
            }
        }

        foreach (var result in vote.Results)
        {
            foreach (var ballotResult in result.Results)
            {
                ballotResult.CountOfVoters.GetNonNullableSubTotal(_dataSource).ReceivedBallots.Should().Be(0);
                foreach (var questionResult in ballotResult.QuestionResults)
                {
                    var subTotal = questionResult.GetNonNullableSubTotal(_dataSource);
                    subTotal.TotalCountOfAnswerYes.Should().Be(0);
                    subTotal.TotalCountOfAnswerNo.Should().Be(0);
                    subTotal.TotalCountOfAnswerUnspecified.Should().Be(0);
                }

                foreach (var questionResult in ballotResult.TieBreakQuestionResults)
                {
                    var subTotal = questionResult.GetNonNullableSubTotal(_dataSource);
                    subTotal.TotalCountOfAnswerQ1.Should().Be(0);
                    subTotal.TotalCountOfAnswerQ2.Should().Be(0);
                    subTotal.TotalCountOfAnswerUnspecified.Should().Be(0);
                }
            }
        }
    }

    private async Task SetResultState<TAggregate, TResult>(Guid contestId, Guid resultId, CountingCircleResultState state)
        where TAggregate : CountingCircleResultAggregate
        where TResult : CountingCircleResult
    {
        TrySetFakeAuth(SecureConnectTestDefaults.MockedTenantStGallen.Id, RolesMockedData.MonitoringElectionAdmin);
        await ModifyDbEntities(
            (TResult result) =>
                result.Id == resultId,
            result => result.State = state);

        var resultAgg = await AggregateRepositoryMock.GetOrCreateById<TAggregate>(resultId);
        if (resultAgg.State == state)
        {
            return;
        }

        switch (state)
        {
            case CountingCircleResultState.SubmissionDone:
                resultAgg.SubmissionFinished(contestId);
                break;
            case CountingCircleResultState.ReadyForCorrection:
                resultAgg.SubmissionFinished(contestId);
                resultAgg.FlagForCorrection(contestId);
                break;
            case CountingCircleResultState.CorrectionDone:
                resultAgg.SubmissionFinished(contestId);
                resultAgg.FlagForCorrection(contestId);
                resultAgg.CorrectionFinished(string.Empty, contestId);
                break;
            case CountingCircleResultState.AuditedTentatively:
                resultAgg.SubmissionFinished(contestId);
                resultAgg.AuditedTentatively(contestId);
                break;
            case CountingCircleResultState.Plausibilised:
                resultAgg.SubmissionFinished(contestId);
                resultAgg.AuditedTentatively(contestId);
                resultAgg.Plausibilise(contestId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        await AggregateRepositoryMock.Save(resultAgg);
    }
}
