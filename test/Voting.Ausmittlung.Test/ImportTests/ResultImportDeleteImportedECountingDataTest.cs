// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;
using ResultImportType = Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultImportDeleteImportedECountingDataTest : BaseIntegrationTest
{
    private readonly VotingDataSource _dataSource = VotingDataSource.ECounting;

    public ResultImportDeleteImportedECountingDataTest(TestApplicationFactory factory)
        : base(factory)
    {
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
        await ResultImportECountingMockedData.Seed(RunScoped);
        await ResultImportECountingMockedData.SeedUzwilAggregates(RunScoped);

        // start submission and set result states
        await new ResultService.ResultServiceClient(Factory.CreateGrpcChannel(
            true,
            SecureConnectTestDefaults.MockedTenantUzwil.Id,
            "user-id",
            new[] { RolesMockedData.ErfassungElectionAdmin }))
            .GetListAsync(new GetResultListRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            });

        EventPublisherMock.Clear();
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        var eventCounter = await ResultImportMockedData.SeedECounting(RunScoped, CreateHttpClient);

        var id = "759b344f-511a-41f6-8836-43870949e52c";
        await TestEventPublisher.Publish(
            eventCounter,
            new ResultImportDataDeleted
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
                ImportType = ResultImportType.Ecounting,
                ImportId = id,
                EventInfo = GetMockedEventInfo(),
            });

        var import = await RunOnDb(db => db.ResultImports.FirstAsync(x => x.Id == Guid.Parse(id)));
        import.Deleted.Should().BeTrue();
        import.MatchSnapshot();

        await AssertProportionalElectionResultZero(Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestStGallen));
        await AssertMajorityElectionResultZero(Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen));
        await AssertVoteResultZero(Guid.Parse(VoteMockedData.IdUzwilVoteInContestStGallen));
    }

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
}
