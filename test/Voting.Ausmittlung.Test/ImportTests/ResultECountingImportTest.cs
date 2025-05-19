// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Services.Write.Import;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Common;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ImportTests;

// a lot of validation tests are only present in the e-voting tests as the logic is shared.
public class ResultECountingImportTest : BaseRestTest
{
    private const string TestFileOk = "ImportTests/ExampleFiles/ech0222_e_counting_import_ok.xml";
    private const string TestFileInvalid = "ImportTests/ExampleFiles/ech0222_e_counting_import_invalid.xml";
    private const string TestFileOtherContestId = "ImportTests/ExampleFiles/ech0222_e_counting_import_other_contest_id.xml";

    public ResultECountingImportTest(TestApplicationFactory factory)
        : base(factory)
    {
        GetService<ECountingResultImportWriter>();
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
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());

        // start submission and set result states
        await new ResultService.ResultServiceClient(CreateGrpcChannel(RolesMockedData.ErfassungElectionAdmin))
            .GetListAsync(new GetResultListRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            });

        EventPublisherMock.Clear();
    }

    [Fact]
    public async Task TestAsErfassungElectionAdminShouldWork()
    {
        // set a state to ready for correction
        await SetProportionalElectionResultState(ProportionalElectionResultMockedData.IdUzwilElectionResultInContestStGallen, CountingCircleResultState.ReadyForCorrection);

        EventPublisherMock.Clear();

        using var resp = await ErfassungElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk));
        resp.EnsureSuccessStatusCode();

        var started = EventPublisherMock.GetSinglePublishedEvent<ResultImportStarted>();
        var importId = started.ImportId;
        started.ImportId = string.Empty;
        started.MatchSnapshot("started");

        var proportionalElectionImported = EventPublisherMock.GetPublishedEvents<ProportionalElectionResultImported>().ToList();
        proportionalElectionImported.All(x => x.ImportId == importId).Should().BeTrue();

        foreach (var ev in proportionalElectionImported)
        {
            ev.ImportId = string.Empty;
        }

        proportionalElectionImported.MatchSnapshot("proportionalElectionImported");

        var voteImported = EventPublisherMock.GetPublishedEvents<VoteResultImported>().ToList();
        voteImported.All(x => x.ImportId == importId).Should().BeTrue();

        foreach (var ev in voteImported)
        {
            ev.ImportId = string.Empty;
        }

        voteImported.MatchSnapshot("voteImported");

        var majorityElectionImported = EventPublisherMock.GetPublishedEvents<MajorityElectionResultImported>().ToList();
        majorityElectionImported.All(x => x.ImportId == importId).Should().BeTrue();

        foreach (var ev in majorityElectionImported)
        {
            ev.ImportId = string.Empty;

            foreach (var writeIn in ev.WriteIns)
            {
                writeIn.WriteInMappingId.Should().NotBeEmpty();
                writeIn.WriteInMappingId = string.Empty;
            }
        }

        majorityElectionImported.MatchSnapshot("majorityElectionImported");

        var secondaryMajorityElectionImported = EventPublisherMock.GetPublishedEvents<SecondaryMajorityElectionResultImported>().ToList();
        secondaryMajorityElectionImported.All(x => x.ImportId == importId).Should().BeTrue();

        foreach (var ev in secondaryMajorityElectionImported)
        {
            ev.ImportId = string.Empty;

            foreach (var writeIn in ev.WriteIns)
            {
                writeIn.WriteInMappingId.Should().NotBeEmpty();
                writeIn.WriteInMappingId = string.Empty;
            }
        }

        secondaryMajorityElectionImported.MatchSnapshot("secondaryMajorityElectionImported");

        var completed = EventPublisherMock.GetSinglePublishedEvent<ResultImportCompleted>();
        completed.ImportId.Should().Be(importId);
        completed.ImportId = string.Empty;
        completed.MatchSnapshot("completed");

        var completedCountingCircles = EventPublisherMock.GetPublishedEvents<ResultImportCountingCircleCompleted>().ToList();
        foreach (var ccCompleted in completedCountingCircles)
        {
            ccCompleted.ImportId.Should().Be(importId);
            ccCompleted.ImportId = string.Empty;
        }

        completedCountingCircles.MatchSnapshot("completedCountingCircles");
    }

    [Fact]
    public async Task InvalidImportFileShouldThrow()
    {
        await AssertProblemDetails(
            () => ErfassungElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileInvalid)),
            HttpStatusCode.BadRequest,
            "List of possible elements expected: 'senderId'");
    }

    [Fact]
    public async Task ContestLockedShouldThrow()
    {
        await ModifyDbEntities<Contest>(
            x => x.Id == ContestMockedData.GuidStGallenEvoting,
            x => x.State = ContestState.PastLocked);
        await AssertProblemDetails(
            () => ErfassungElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk)),
            HttpStatusCode.BadRequest,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task CountingCircleInactiveECountingShouldThrow()
    {
        await ModifyDbEntities<ContestCountingCircleDetails>(
            x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil,
            x => x.ECounting = false);
        await AssertProblemDetails(
            () => ErfassungElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk)),
            HttpStatusCode.FailedDependency,
            $"The counting circle with id {CountingCircleMockedData.IdUzwil} was not found or does not have eCounting enabled.");
    }

    [Fact]
    public Task OtherTenantShouldThrow()
    {
        var otherTenantClient = base.CreateHttpClient(
            tenant: SecureConnectTestDefaults.MockedTenantGenf.Id,
            roles: RolesMockedData.MonitoringElectionAdmin);
        return AssertStatus(
            () => otherTenantClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk)),
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SubmissionDoneShouldThrow()
    {
        // set a state to submission done
        await SetProportionalElectionResultState(ProportionalElectionResultMockedData.IdUzwilElectionResultInContestStGallen, CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            () => ErfassungElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk)),
            HttpStatusCode.FailedDependency);
    }

    [Fact]
    public Task OtherContestIdShouldThrow()
    {
        return AssertProblemDetails(
            () => ErfassungElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOtherContestId)),
            HttpStatusCode.BadRequest,
            "contestIds do not match");
    }

    [Fact]
    public void DuplicateCountingCircleResultsShouldThrow()
    {
        TrySetFakeAuth();

        var import = new VotingImport(
            "my-mock-ech-message-id",
            ContestMockedData.GuidStGallenEvoting);
        AssertException<ValidationException>(
            () => import.AddResults([
                new VotingImportElectionResult(
                    Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen),
                    CountingCircleMockedData.IdUzwil,
                    [
                        new(null, false, Array.Empty<VotingImportElectionBallotPosition>())
                    ]),
                new VotingImportElectionResult(
                    Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen),
                    CountingCircleMockedData.IdUzwil,
                    [
                        new(null, false, Array.Empty<VotingImportElectionBallotPosition>())
                    ]),
            ]),
            "Duplicate counting circle result provided for the same political business");
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        using var resp = await ErfassungElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk));
        resp.EnsureSuccessStatusCode();

        var importStarted = EventPublisherMock.GetSinglePublishedEvent<ResultImportStarted>();
        await TestEventPublisher.Publish(GetNextEventNumber(), importStarted);

        var import = await RunOnDb(db => db.ResultImports.FirstAsync(x => x.Id == Guid.Parse(importStarted.ImportId)));
        var importId = import.Id;
        import.Id = Guid.Empty;
        import.Completed.Should().BeFalse();
        import.MatchSnapshot("started");

        var contest = await RunOnDb(db =>
            db.Contests.SingleAsync(c => c.Id == ContestMockedData.GuidStGallenEvoting));
        contest.ECountingResultsImported.Should().BeFalse();

        await RunEvents<ProportionalElectionResultImported>(false);
        await RunEvents<VoteResultImported>(false);
        await RunEvents<MajorityElectionResultImported>(false);
        await RunEvents<SecondaryMajorityElectionResultImported>(false);

        var vote = await GetVoteWithResults();
        vote.Results.MatchSnapshot("voteResults");

        var proportionalElection = await GetProportionalElectionWithResults();
        proportionalElection.Results.MatchSnapshot("proportionalElectionResults");

        var majorityElection = await GetMajorityElectionWithResults();
        majorityElection.Results.MatchSnapshot("majorityElectionResults");

        var importCompleted = EventPublisherMock.GetSinglePublishedEvent<ResultImportCompleted>();
        await TestEventPublisher.Publish(GetNextEventNumber(), importCompleted);
        await RunEvents<ResultImportCountingCircleCompleted>(false);

        import = await RunOnDb(db => db.ResultImports
            .Include(x => x.ImportedCountingCircles.OrderBy(cc => cc.CountingCircleId))
            .FirstAsync(x => x.Id == Guid.Parse(importStarted.ImportId)));
        import.Id = Guid.Empty;
        import.Completed.Should().BeTrue();
        foreach (var importedCc in import.ImportedCountingCircles)
        {
            importedCc.ResultImportId = Guid.Empty;
            importedCc.Id = Guid.Empty;
        }

        import.MatchSnapshot("completed");

        var writeIns = await RunOnDb(db => db.MajorityElectionWriteInMappings.OrderBy(x => x.WriteInCandidateName).ToListAsync());
        foreach (var writeIn in writeIns)
        {
            writeIn.Result = new MajorityElectionResult();
            writeIn.ResultId = Guid.Empty;
            writeIn.Id = Guid.Empty;
            writeIn.ImportId = Guid.Empty;
        }

        writeIns.ShouldMatchChildSnapshot("writeIns");

        contest = await RunOnDb(db =>
            db.Contests.SingleAsync(c => c.Id == ContestMockedData.GuidStGallenEvoting));
        contest.ECountingResultsImported.Should().BeTrue();

        await AssertHasPublishedEventProcessedMessage(ResultImportCountingCircleCompleted.Descriptor, importId);
    }

    [Fact]
    public async Task ProcessorImportTwiceShouldOverwrite()
    {
        async Task<string> RunImport()
        {
            using var resp = await ErfassungElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk));
            resp.EnsureSuccessStatusCode();

            var importCreated = EventPublisherMock.GetSinglePublishedEvent<ResultImportCreated>();
            await TestEventPublisher.Publish(GetNextEventNumber(), importCreated);

            var importStarted = EventPublisherMock.GetSinglePublishedEvent<ResultImportStarted>();
            await TestEventPublisher.Publish(GetNextEventNumber(), importStarted);

            await RunEvents<ProportionalElectionResultImported>(false);
            await RunEvents<VoteResultImported>(false);
            await RunEvents<MajorityElectionResultImported>(false);
            await RunEvents<SecondaryMajorityElectionResultImported>(false);

            var importCompleted = EventPublisherMock.GetSinglePublishedEvent<ResultImportCompleted>();
            await TestEventPublisher.Publish(GetNextEventNumber(), importCompleted);
            return importStarted.ImportId;
        }

        async Task AssertResults()
        {
            var proportionalElection = await GetProportionalElectionWithResults();
            var proportionalElectionResult = proportionalElection.Results.Single();
            proportionalElectionResult.ECountingSubTotal.TotalCountOfBallots.Should().Be(1);
            proportionalElectionResult.TotalCountOfBallots.Should().Be(1);

            var majorityElection = await GetMajorityElectionWithResults();
            var majorityElectionResult = majorityElection.Results.Single();
            majorityElectionResult.ECountingSubTotal.TotalCandidateVoteCountInclIndividual.Should().Be(7);
            majorityElectionResult.TotalCandidateVoteCountInclIndividual.Should().Be(7);
            majorityElectionResult.CandidateResults.First().ECountingExclWriteInsVoteCount.Should().Be(7);

            var vote = await GetVoteWithResults();
            var voteBallotResult = vote.Results.Single().Results.Single();
            var questionResult = voteBallotResult.QuestionResults.Single();
            questionResult.ECountingSubTotal.TotalCountOfAnswerYes.Should().Be(1);
            questionResult.TotalCountOfAnswerYes.Should().Be(5301);
        }

        var firstImportId = await RunImport();
        await AssertResults();
        EventPublisherMock.Clear();
        var secondImportId = await RunImport();
        await AssertResults();

        var succeedEv = EventPublisherMock.GetSinglePublishedEvent<ResultImportSucceeded>();
        succeedEv.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
        succeedEv.CountingCircleId.Should().Be(CountingCircleMockedData.IdUzwil);
        succeedEv.ImportId.Should().Be(firstImportId);
        succeedEv.SuccessorImportId.Should().Be(secondImportId);

        // result imports should be kept and not overwritten
        (await RunOnDb(db => db.ResultImports.CountAsync()))
            .Should()
            .Be(2);
    }

    protected override HttpClient CreateHttpClient(
        bool authorize = true,
        string? tenant = "000000000000000000",
        string? userId = "default-user-id",
        params string[] roles)
        => base.CreateHttpClient(authorize, SecureConnectTestDefaults.MockedTenantUzwil.Id, userId, roles);

    protected override GrpcChannel CreateGrpcChannel(
        bool authorize = true,
        string? tenant = "000000000000000000",
        string? userId = "default-user-id",
        params string[] roles)
        => base.CreateGrpcChannel(authorize, SecureConnectTestDefaults.MockedTenantUzwil.Id, userId, roles);

    protected override async Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return await httpClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private void TrySetFakeAuth()
    {
        if (GetService<IAuth>().IsAuthenticated)
        {
            return;
        }

        GetService<IAuthStore>().SetValues(
            "mock-token",
            "fake",
            SecureConnectTestDefaults.MockedTenantUzwil.Id,
            [RolesMockedData.MonitoringElectionAdmin]);
    }

    private Uri BuildUri(Guid? contestId = null)
        => new($"api/result_import/e-counting/{contestId?.ToString() ?? ContestMockedData.IdStGallenEvoting}/{CountingCircleMockedData.IdUzwil}", UriKind.RelativeOrAbsolute);

    private async Task<ProportionalElection> GetProportionalElectionWithResults()
    {
        var election = await RunOnDb(
            db => db.ProportionalElections
                .AsSplitQuery()
                .Include(x => x.Results.Where(y => y.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil))
                .ThenInclude(x => x.UnmodifiedListResults)
                .Include(x => x.Results)
                .ThenInclude(x => x.ListResults)
                .ThenInclude(x => x.CandidateResults)
                .ThenInclude(x => x.VoteSources)
                .FirstAsync(x =>
                    x.Id == Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestStGallen)),
            Languages.German);
        SortAndCleanIds(election);
        return election;
    }

    private async Task<MajorityElection> GetMajorityElectionWithResults()
    {
        var election = await RunOnDb(
            db => db.MajorityElections
                .AsSplitQuery()
                .Include(x => x.Results.Where(y => y.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil))
                .ThenInclude(x => x.CandidateResults)
                .Include(x => x.SecondaryMajorityElections)
                .Include(x => x.Results.Where(y => y.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil))
                .ThenInclude(x => x.CandidateResults)
                .ThenInclude(x => x.Candidate)
                .ThenInclude(x => x.Translations)
                .Include(x => x.Results)
                .FirstAsync(x =>
                    x.Id == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen)),
            Languages.German);
        SortAndCleanIds(election);
        return election;
    }

    private async Task<Vote> GetVoteWithResults()
    {
        var vote = await RunOnDb(
            db => db.Votes
                .AsSplitQuery()
                .Include(x => x.Results.Where(y => y.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil))
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.QuestionResults)
                .Include(x => x.Results).ThenInclude(x => x.Results).ThenInclude(x => x.TieBreakQuestionResults)
                .FirstAsync(x =>
                    x.Id == Guid.Parse(VoteMockedData.IdUzwilVoteInContestStGallen)),
            Languages.German);
        SortAndCleanIds(vote);
        return vote;
    }

    private void SortAndCleanIds(ProportionalElection election)
    {
        foreach (var result in election.Results)
        {
            result.ProportionalElection = null!;
            result.CountingCircleId = Guid.Empty;
            result.UnmodifiedListResults = result.UnmodifiedListResults.OrderBy(x => x.ListId).ToList();

            foreach (var unmodifiedListResult in result.UnmodifiedListResults)
            {
                unmodifiedListResult.Id = Guid.Empty;
                unmodifiedListResult.ResultId = Guid.Empty;
            }

            result.ListResults = result.ListResults.OrderBy(x => x.ListId).ToList();

            foreach (var listResult in result.ListResults)
            {
                listResult.Id = Guid.Empty;
                listResult.ResultId = Guid.Empty;
                listResult.CandidateResults = listResult.CandidateResults.OrderBy(x => x.CandidateId).ToList();

                foreach (var candidateResult in listResult.CandidateResults)
                {
                    candidateResult.Id = Guid.Empty;
                    candidateResult.ListResultId = Guid.Empty;
                    candidateResult.VoteSources = candidateResult.VoteSources.OrderBy(x => x.ListId).ToList();

                    foreach (var voteSource in candidateResult.VoteSources)
                    {
                        voteSource.Id = Guid.Empty;
                        voteSource.CandidateResultId = Guid.Empty;
                    }
                }
            }
        }
    }

    private void SortAndCleanIds(MajorityElection election)
    {
        foreach (var result in election.Results)
        {
            result.MajorityElection = null!;
            result.CountingCircleId = Guid.Empty;
            result.CandidateResults = result.CandidateResults.OrderBy(x => x.Candidate.Position).ToList();

            foreach (var writeInBallot in result.WriteInBallots)
            {
                writeInBallot.ImportId = Guid.Empty;
            }

            foreach (var writeInMapping in result.WriteInMappings)
            {
                writeInMapping.ImportId = Guid.Empty;
            }

            foreach (var candidateResult in result.CandidateResults)
            {
                candidateResult.Id = Guid.Empty;
                candidateResult.ElectionResultId = Guid.Empty;

                foreach (var translation in candidateResult.Candidate.Translations)
                {
                    translation.Id = Guid.Empty;
                }
            }
        }
    }

    private void SortAndCleanIds(Vote vote)
    {
        foreach (var result in vote.Results)
        {
            result.Vote = null!;
            result.CountingCircleId = Guid.Empty;
            result.Results = result.Results.OrderBy(x => x.BallotId).ToList();

            foreach (var ballotResult in result.Results)
            {
                ballotResult.Id = Guid.Empty;
                ballotResult.VoteResultId = Guid.Empty;
                ballotResult.QuestionResults = ballotResult.QuestionResults.OrderBy(x => x.QuestionId).ToList();
                ballotResult.TieBreakQuestionResults = ballotResult.TieBreakQuestionResults.OrderBy(x => x.QuestionId).ToList();

                foreach (var questionResult in ballotResult.QuestionResults)
                {
                    questionResult.Id = Guid.Empty;
                    questionResult.BallotResultId = Guid.Empty;
                }

                foreach (var tieBreakQuestionResult in ballotResult.TieBreakQuestionResults)
                {
                    tieBreakQuestionResult.Id = Guid.Empty;
                    tieBreakQuestionResult.BallotResultId = Guid.Empty;
                }
            }
        }
    }

    private async Task SetProportionalElectionResultState(string resultIdStr, CountingCircleResultState state)
    {
        TrySetFakeAuth();
        var id = Guid.Parse(resultIdStr);
        await ModifyDbEntities(
            (ProportionalElectionResult result) =>
                result.Id == id,
            result => result.State = state);

        var contestId = (await RunOnDb(db => db.ProportionalElectionResults
            .Include(r => r.ProportionalElection)
            .FirstAsync(r => r.Id == id))).ProportionalElection.ContestId;

        var resultAgg = await AggregateRepositoryMock.GetById<ProportionalElectionResultAggregate>(id);
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
