// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Core.Services.Write.Import;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultImportTest : BaseRestTest
{
    private const string TestFileOk = "ImportTests/ExampleFiles/ech0222_import_ok.xml";
    private const string TestFileOkEmpty = "ImportTests/ExampleFiles/ech0222_import_ok_empty.xml";
    private const string TestFileOtherContestId = "ImportTests/ExampleFiles/ech0222_import_other_contest_id.xml";

    private static readonly ResultImportMeta ImportMeta = new(
        Guid.Parse(ContestMockedData.IdStGallenEvoting),
        "test.xml",
        Stream.Null);

    private readonly HttpClient _monitoringElectionAdminStGallenClient;
    private readonly HttpClient _monitoringElectionAdminUzwilClient;
    private readonly ResultImportWriter _importWriter;

    private int _eventNumberCounter;

    public ResultImportTest(TestApplicationFactory factory)
        : base(factory)
    {
        _monitoringElectionAdminStGallenClient = CreateHttpClient(
            tenant: SecureConnectTestDefaults.MockedTenantStGallen.Id,
            roles: RolesMockedData.MonitoringElectionAdmin);

        _monitoringElectionAdminUzwilClient = CreateHttpClient(
            tenant: SecureConnectTestDefaults.MockedTenantUzwil.Id,
            roles: RolesMockedData.MonitoringElectionAdmin);

        _importWriter = GetService<ResultImportWriter>();
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

        // activate e voting for all for easier testing
        // we deactivate it in some tests again to test the flag
        await ModifyDbEntities((ContestCountingCircleDetails _) => true, details => details.EVoting = true);
        await ModifyDbEntities((Contest _) => true, contest => contest.EVoting = true);

        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task TestAsMonitoringElectionAdminShouldWork()
    {
        // set a state to submission finished
        await SetProportionalElectionResultState(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen, CountingCircleResultState.SubmissionDone);
        await SetProportionalElectionResultState(ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil, CountingCircleResultState.CorrectionDone);

        EventPublisherMock.Clear();

        using var resp = await _monitoringElectionAdminStGallenClient.PostFile(BuildUri(), TestFileOk);
        resp.EnsureSuccessStatusCode();

        var stateChanges = EventPublisherMock.GetPublishedEvents<ProportionalElectionResultFlaggedForCorrection>();
        stateChanges.Select(x => x.ElectionResultId)
            .Should()
            .BeInAscendingOrder(
                ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil);
        EventPublisherMock.GetPublishedEvents<MajorityElectionResultFlaggedForCorrection>()
            .Should()
            .HaveCount(0);
        EventPublisherMock.GetPublishedEvents<VoteResultFlaggedForCorrection>()
            .Should()
            .HaveCount(0);

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
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            // set a state to submission finished
            await SetProportionalElectionResultState(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen, CountingCircleResultState.SubmissionDone);
            await SetProportionalElectionResultState(ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil, CountingCircleResultState.CorrectionDone);

            EventPublisherMock.Clear();

            using var resp = await _monitoringElectionAdminStGallenClient.PostFile(BuildUri(), TestFileOk);
            resp.EnsureSuccessStatusCode();

            var stateChanges = EventPublisherMock.GetPublishedEvents<ProportionalElectionResultFlaggedForCorrection>();
            stateChanges.Select(x => x.ElectionResultId)
                .Should()
                .BeInAscendingOrder(
                    ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                    ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil);
            EventPublisherMock.GetPublishedEvents<MajorityElectionResultFlaggedForCorrection>()
                .Should()
                .HaveCount(0);
            EventPublisherMock.GetPublishedEvents<VoteResultFlaggedForCorrection>()
                .Should()
                .HaveCount(0);

            var started = EventPublisherMock.GetSinglePublishedEventWithMetadata<ResultImportStarted>();
            var proportionalElectionImported = EventPublisherMock.GetPublishedEventsWithMetadata<ProportionalElectionResultImported>().ToList();
            var voteImported = EventPublisherMock.GetPublishedEventsWithMetadata<VoteResultImported>().ToList();
            var majorityElectionImported = EventPublisherMock.GetPublishedEventsWithMetadata<MajorityElectionResultImported>().ToList();
            var secondaryMajorityElectionImported = EventPublisherMock.GetPublishedEventsWithMetadata<SecondaryMajorityElectionResultImported>().ToList();
            var completed = EventPublisherMock.GetSinglePublishedEventWithMetadata<ResultImportCompleted>();

            var events = new List<EventWithMetadata>();
            events.Add(EventPublisherMock.GetSinglePublishedEventWithMetadata<ResultImportStarted>());
            events.AddRange(EventPublisherMock.GetPublishedEventsWithMetadata<ProportionalElectionResultImported>());
            events.AddRange(EventPublisherMock.GetPublishedEventsWithMetadata<VoteResultImported>());
            events.AddRange(EventPublisherMock.GetPublishedEventsWithMetadata<MajorityElectionResultImported>());
            events.AddRange(EventPublisherMock.GetPublishedEventsWithMetadata<SecondaryMajorityElectionResultImported>());
            events.Add(EventPublisherMock.GetSinglePublishedEventWithMetadata<ResultImportCompleted>());
            return events.ToArray();
        });
    }

    [Fact]
    public async Task TestAsMonitoringElectionAdminWithEmptyFileShouldWork()
    {
        using var resp = await _monitoringElectionAdminStGallenClient.PostFile(BuildUri(), TestFileOkEmpty);
        resp.EnsureSuccessStatusCode();

        // should have one started and one completed event but no others
        EventPublisherMock.GetSinglePublishedEvent<ResultImportStarted>().Should().NotBeNull();
        EventPublisherMock.GetPublishedEvents<VoteResultImported>().Should().BeEmpty();
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultImported>().Should().BeEmpty();
        EventPublisherMock.GetPublishedEvents<MajorityElectionResultImported>().Should().BeEmpty();
        EventPublisherMock.GetPublishedEvents<SecondaryMajorityElectionResultImported>().Should().BeEmpty();
        EventPublisherMock.GetSinglePublishedEvent<ResultImportCompleted>().Should().NotBeNull();
    }

    [Fact]
    public async Task ContestLockedShouldThrow()
    {
        await ModifyDbEntities<Contest>(
            x => x.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting),
            x => x.State = ContestState.PastLocked);
        await AssertProblemDetails(
            () => _monitoringElectionAdminStGallenClient.PostFile(BuildUri(), TestFileOk),
            HttpStatusCode.BadRequest,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task ContestEVotingDisabledShouldThrow()
    {
        await ModifyDbEntities<Contest>(
            x => x.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting),
            x => x.EVoting = false);
        await AssertProblemDetails(
            () => _monitoringElectionAdminStGallenClient.PostFile(BuildUri(), TestFileOk),
            HttpStatusCode.FailedDependency,
            "eVoting is not active on the Contest with the id 95825eb0-0f52-461a-a5f8-23fb35fa69e1");
    }

    [Fact]
    public async Task CountingCircleInactiveEVotingShouldThrow()
    {
        await ModifyDbEntities<ContestCountingCircleDetails>(
            x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidGossau,
            x => x.EVoting = false);
        await AssertProblemDetails(
            () => _monitoringElectionAdminStGallenClient.PostFile(BuildUri(), TestFileOk),
            HttpStatusCode.FailedDependency,
            $"eVoting is not active on the CountingCircle with the id {CountingCircleMockedData.IdGossau}");
    }

    [Fact]
    public Task OtherTenantShouldThrow()
    {
        return AssertStatus(
            () => _monitoringElectionAdminUzwilClient.PostFile(BuildUri(), TestFileOk),
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CountingCircleResultAuditedShouldThrow()
    {
        // set a state to submission finished
        await SetProportionalElectionResultState(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen, CountingCircleResultState.AuditedTentatively);

        await AssertProblemDetails(
            () => _monitoringElectionAdminStGallenClient.PostFile(BuildUri(), TestFileOk),
            HttpStatusCode.FailedDependency,
            $"A result is in an invalid state for an eVoting import to be possible ({ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen})");
    }

    [Fact]
    public Task OtherContestIdShouldThrow()
    {
        return AssertProblemDetails(
            () => _monitoringElectionAdminStGallenClient.PostFile(BuildUri(), TestFileOtherContestId),
            HttpStatusCode.BadRequest,
            "contestIds do not match");
    }

    [Fact]
    public Task ContestNotFoundShouldThrow()
    {
        var req = NewSimpleProportionalElectionImportData(Guid.NewGuid());
        return Assert.ThrowsAsync<EntityNotFoundException>(() => _importWriter.Import(req, ImportMeta));
    }

    [Fact]
    public Task CountingCircleNotFoundShouldThrow()
    {
        TrySetFakeAuth();
        var req = NewSimpleProportionalElectionImportData(countingCircleId: Guid.NewGuid());
        return Assert.ThrowsAsync<EntityNotFoundException>(() => _importWriter.Import(req, ImportMeta));
    }

    [Fact]
    public async Task MajorityElectionShouldMapDuplicatesToInvalidIfInvalidAvailable()
    {
        await ModifyDbEntities(
            (MajorityElection el) => el.Id == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen),
            el => el.InvalidVotes = true);

        TrySetFakeAuth();

        var positions = new[]
        {
                EVotingElectionBallotPosition.ForWriteIn("hans mueller"),
                EVotingElectionBallotPosition.ForWriteIn("Hans Mueller"),
                EVotingElectionBallotPosition.ForCandidateId(Guid.Parse(MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen)),
                EVotingElectionBallotPosition.ForCandidateId(Guid.Parse(MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen)),
        };
        var req = NewSimpleMajorityElectionImportData(new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    positions),
        });

        await _importWriter.Import(req, ImportMeta);

        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultImported>();
        ev.EmptyVoteCount.Should().Be(1); // 1 from missing position
        ev.InvalidVoteCount.Should().Be(2); // 1 write in duplicate, 1 candidate duplicate
        ev.CandidateResults
            .Single(x => x.CandidateId == MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen)
            .VoteCount
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task MajorityElectionShouldMapDuplicatesToEmptyIfInvalidNotAvailable()
    {
        TrySetFakeAuth();

        var positions = new[]
        {
                EVotingElectionBallotPosition.ForWriteIn("hans mueller"),
                EVotingElectionBallotPosition.ForWriteIn("Hans Mueller"),
                EVotingElectionBallotPosition.ForWriteIn("hans müller"),
                EVotingElectionBallotPosition.ForCandidateId(Guid.Parse(MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen)),
        };
        var req = NewSimpleMajorityElectionImportData(new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    positions),
        });

        await _importWriter.Import(req, ImportMeta);

        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultImported>();
        ev.EmptyVoteCount.Should().Be(2); // 1 from duplicate write in + 1 from missing positions
        ev.InvalidVoteCount.Should().Be(0);
        ev.CandidateResults
            .Single(x => x.CandidateId == MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestStGallen)
            .VoteCount
            .Should()
            .Be(1);
    }

    [Fact]
    public Task MajorityElectionImportWithTooManyPositionsShouldThrow()
    {
        TrySetFakeAuth();
        var ballotPositions = Enumerable.Repeat(EVotingElectionBallotPosition.Empty, 10).ToList();
        var req = NewSimpleMajorityElectionImportData(new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    ballotPositions),
        });
        return AssertException<ValidationException>(
            () => _importWriter.Import(req, ImportMeta),
            "the number of ballot positions exceeds the number of mandates (10 vs 5)");
    }

    [Fact]
    public async Task MajorityElectionImportWithEmptyPositionsButSingleMandateShouldThrow()
    {
        await ModifyDbEntities(
            (MajorityElection el) => el.Id == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen),
            el => el.NumberOfMandates = 1);

        TrySetFakeAuth();
        var req = NewSimpleMajorityElectionImportData(new[]
        {
            new EVotingElectionBallot(
                null,
                false,
                new[] { EVotingElectionBallotPosition.Empty }),
        });
        await AssertException<ValidationException>(
            () => _importWriter.Import(req, ImportMeta),
            "empty position provided with single mandate");
    }

    [Fact]
    public Task ProportionalElectionImportWithTooManyPositionsShouldThrow()
    {
        TrySetFakeAuth();
        var req = NewSimpleProportionalElectionImportData();
        var result = req.PoliticalBusinessResults.OfType<EVotingElectionResult>().First();
        var ballotPositions = Enumerable.Repeat(EVotingElectionBallotPosition.Empty, 10).ToList();
        result.Ballots = new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    ballotPositions),
        };
        return AssertException<ValidationException>(
            () => _importWriter.Import(req, ImportMeta),
            "the number of ballot positions exceeds the number of mandates (10 vs 3)");
    }

    [Fact]
    public Task ProportionalElectionImportWithWriteInShouldThrow()
    {
        TrySetFakeAuth();
        var req = NewSimpleProportionalElectionImportData();
        var result = req.PoliticalBusinessResults.OfType<EVotingElectionResult>().First();
        var ballotPositions = new[]
        {
                EVotingElectionBallotPosition.ForWriteIn("test"),
                EVotingElectionBallotPosition.Empty,
                EVotingElectionBallotPosition.Empty,
        };
        result.Ballots = new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    ballotPositions),
        };
        return AssertException<ValidationException>(
            () => _importWriter.Import(req, ImportMeta),
            "proportional election ballot position cannot contain write-ins");
    }

    [Fact]
    public Task ProportionalElectionImportWithUnmodifiedUnknownListShouldThrow()
    {
        TrySetFakeAuth();
        var req = NewSimpleProportionalElectionImportData();
        var ballot = req.PoliticalBusinessResults.OfType<EVotingElectionResult>().First().Ballots.First();
        ballot.Unmodified = true;
        ballot.ListId = Guid.Parse("4869ef1b-0497-4e24-8e01-a03013ed79a2");
        return Assert.ThrowsAsync<EntityNotFoundException>(() => _importWriter.Import(req, ImportMeta));
    }

    [Fact]
    public Task ProportionalElectionImportWithUnmodifiedBallotNoListShouldThrow()
    {
        TrySetFakeAuth();
        var req = NewSimpleProportionalElectionImportData();
        var ballot = req.PoliticalBusinessResults.OfType<EVotingElectionResult>().First().Ballots.First();
        ballot.Unmodified = true;
        ballot.ListId = null;
        return AssertException<ValidationException>(
            () => _importWriter.Import(req, ImportMeta),
            "an unmodified ballot does not have a list assigned");
    }

    [Fact]
    public Task ProportionalElectionImportWithTripledCandidateShouldThrow()
    {
        TrySetFakeAuth();
        var candidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen);
        var ballotPositions = new[]
        {
                EVotingElectionBallotPosition.ForCandidateId(candidateId),
                EVotingElectionBallotPosition.ForCandidateId(candidateId),
                EVotingElectionBallotPosition.ForCandidateId(candidateId),
        };
        var req = NewSimpleProportionalElectionImportData();
        var result = req.PoliticalBusinessResults.OfType<EVotingElectionResult>().First();
        result.Ballots = new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    ballotPositions),
        };
        return AssertException<ValidationException>(
            () => _importWriter.Import(req, ImportMeta),
            "was found more than 2 times on a single ballot");
    }

    [Fact]
    public Task VoteImportWithUnknownQuestionNumberThrow()
    {
        TrySetFakeAuth();
        var req = NewSimpleVoteImportData();
        var result = req.PoliticalBusinessResults.OfType<EVotingVoteResult>().First();
        var ballotResult = result.BallotResults.First();

        var questionAnswers = new[]
        {
                new EVotingVoteBallotQuestionAnswer(3, BallotQuestionAnswer.Yes),
        };

        ballotResult.Ballots = new[]
        {
                new EVotingVoteBallot(questionAnswers, Array.Empty<EVotingVoteBallotTieBreakQuestionAnswer>()),
        };

        return Assert.ThrowsAsync<EntityNotFoundException>(() => _importWriter.Import(req, ImportMeta));
    }

    [Fact]
    public Task VoteImportWithUnknownTieBreakQuestionNumberThrow()
    {
        TrySetFakeAuth();
        var req = NewSimpleVoteImportData();
        var result = req.PoliticalBusinessResults.OfType<EVotingVoteResult>().First();
        var ballotResult = result.BallotResults.First();

        var tieBreakQuestionAnswers = new[]
        {
                new EVotingVoteBallotTieBreakQuestionAnswer(2, TieBreakQuestionAnswer.Q1),
        };

        ballotResult.Ballots = new[]
        {
                new EVotingVoteBallot(Array.Empty<EVotingVoteBallotQuestionAnswer>(), tieBreakQuestionAnswers),
        };

        return Assert.ThrowsAsync<EntityNotFoundException>(() => _importWriter.Import(req, ImportMeta));
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        using var resp = await _monitoringElectionAdminStGallenClient.PostFile(BuildUri(), TestFileOk);
        resp.EnsureSuccessStatusCode();

        var importStarted = EventPublisherMock.GetSinglePublishedEvent<ResultImportStarted>();
        await TestEventPublisher.Publish(GetNextEventNumber(), importStarted);

        var import = await RunOnDb(db => db.ResultImports.FirstAsync(x => x.Id == Guid.Parse(importStarted.ImportId)));
        import.Id = Guid.Empty;
        import.Completed.Should().BeFalse();
        import.MatchSnapshot("started");

        var contest = await RunOnDb(db =>
            db.Contests.SingleAsync(c => c.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting)));
        contest.EVotingResultsImported.Should().BeFalse();

        var propImportEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionResultImported>().ToArray();
        await TestEventPublisher.Publish(GetNextEventNumber(propImportEvents.Length), propImportEvents);

        var proportionalElection = await GetProportionalElectionWithResults();
        proportionalElection.Results.MatchSnapshot("proportionalElectionResults");

        var voteImportEvents = EventPublisherMock.GetPublishedEvents<VoteResultImported>().ToArray();
        await TestEventPublisher.Publish(GetNextEventNumber(voteImportEvents.Length), voteImportEvents);

        var vote = await GetVoteWithResults();
        vote.Results.MatchSnapshot("voteResults");

        var majorityImportEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionResultImported>().ToArray();
        await TestEventPublisher.Publish(GetNextEventNumber(majorityImportEvents.Length), majorityImportEvents);

        var secondaryMajorityImportEvents = EventPublisherMock.GetPublishedEvents<SecondaryMajorityElectionResultImported>().ToArray();
        await TestEventPublisher.Publish(GetNextEventNumber(secondaryMajorityImportEvents.Length), secondaryMajorityImportEvents);

        var majorityElection = await GetMajorityElectionWithResults();
        majorityElection.Results.MatchSnapshot("majorityElectionResults");

        var importCompleted = EventPublisherMock.GetSinglePublishedEvent<ResultImportCompleted>();
        await TestEventPublisher.Publish(GetNextEventNumber(), importCompleted);

        import = await RunOnDb(db => db.ResultImports.FirstAsync(x => x.Id == Guid.Parse(importStarted.ImportId)));
        import.Id = Guid.Empty;
        import.Completed.Should().BeTrue();
        import.MatchSnapshot("completed");

        contest = await RunOnDb(db =>
            db.Contests.SingleAsync(c => c.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting)));
        contest.EVotingResultsImported.Should().BeTrue();

        var writeIns = await RunOnDb(db => db.MajorityElectionWriteInMappings.OrderBy(x => x.WriteInCandidateName).ToListAsync());
        foreach (var writeIn in writeIns)
        {
            writeIn.ResultId = Guid.Empty;
            writeIn.Id = Guid.Empty;
        }

        writeIns.ShouldMatchChildSnapshot("writeIns");

        var secondaryWriteIns = await RunOnDb(db => db.SecondaryMajorityElectionWriteInMappings.OrderBy(x => x.WriteInCandidateName).ToListAsync());
        foreach (var writeIn in secondaryWriteIns)
        {
            writeIn.ResultId = Guid.Empty;
            writeIn.Id = Guid.Empty;
        }

        secondaryWriteIns.ShouldMatchChildSnapshot("secondaryWriteIns");
    }

    [Fact]
    public async Task ProcessorImportTwiceShouldOverwrite()
    {
        async Task<string> RunImport()
        {
            using var resp = await _monitoringElectionAdminStGallenClient.PostFile(BuildUri(), TestFileOk);
            resp.EnsureSuccessStatusCode();

            var importCreated = EventPublisherMock.GetSinglePublishedEvent<ResultImportCreated>();
            await TestEventPublisher.Publish(GetNextEventNumber(), importCreated);

            var importStarted = EventPublisherMock.GetSinglePublishedEvent<ResultImportStarted>();
            await TestEventPublisher.Publish(GetNextEventNumber(), importStarted);

            var propImportEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionResultImported>().ToArray();
            await TestEventPublisher.Publish(GetNextEventNumber(propImportEvents.Length), propImportEvents);

            var voteImportEvents = EventPublisherMock.GetPublishedEvents<VoteResultImported>().ToArray();
            await TestEventPublisher.Publish(GetNextEventNumber(voteImportEvents.Length), voteImportEvents);

            var majorityImportEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionResultImported>().ToArray();
            await TestEventPublisher.Publish(GetNextEventNumber(majorityImportEvents.Length), majorityImportEvents);

            var secondaryMajorityImportEvents = EventPublisherMock.GetPublishedEvents<SecondaryMajorityElectionResultImported>().ToArray();
            await TestEventPublisher.Publish(GetNextEventNumber(secondaryMajorityImportEvents.Length), secondaryMajorityImportEvents);

            var importCompleted = EventPublisherMock.GetSinglePublishedEvent<ResultImportCompleted>();
            await TestEventPublisher.Publish(GetNextEventNumber(), importCompleted);
            return importStarted.ImportId;
        }

        async Task AssertResults()
        {
            var proportionalElection = await GetProportionalElectionWithResults();
            var proportionalElectionResult = proportionalElection.Results.First();
            proportionalElectionResult.EVotingSubTotal.TotalCountOfBallots.Should().Be(6);
            proportionalElectionResult.TotalCountOfBallots.Should().Be(6);

            var majorityElection = await GetMajorityElectionWithResults();
            var majorityElectionResult = majorityElection.Results.First();
            majorityElectionResult.EVotingSubTotal.TotalCandidateVoteCountInclIndividual.Should().Be(3);
            majorityElectionResult.TotalCandidateVoteCountInclIndividual.Should().Be(3);

            var vote = await GetVoteWithResults();
            var voteBallotResult = vote.Results.First().Results.First();
            var questionResult = voteBallotResult.QuestionResults.First();
            questionResult.EVotingSubTotal.TotalCountOfAnswerYes.Should().Be(1);
            questionResult.TotalCountOfAnswerYes.Should().Be(5501);
        }

        var firstImportId = await RunImport();
        await AssertResults();
        EventPublisherMock.Clear();
        var secondImportId = await RunImport();
        await AssertResults();

        var succeedEv = EventPublisherMock.GetSinglePublishedEvent<ResultImportSucceeded>();
        succeedEv.ContestId.Should().Be(ContestMockedData.IdStGallenEvoting);
        succeedEv.ImportId.Should().Be(firstImportId);
        succeedEv.SuccessorImportId.Should().Be(secondImportId);

        // result imports should be kept and not overwritten
        (await RunOnDb(db => db.ResultImports.CountAsync()))
            .Should()
            .Be(2);
    }

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
        => httpClient.PostFile(BuildUri(), TestFileOk);

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.ErfassungCreator;
        yield return NoRole;
    }

    private void TrySetFakeAuth()
    {
        if (GetService<IAuth>().IsAuthenticated)
        {
            return;
        }

        GetService<IAuthStore>().SetValues(
            "fake",
            SecureConnectTestDefaults.MockedTenantStGallen.Id,
            new[] { RolesMockedData.MonitoringElectionAdmin });
    }

    private Uri BuildUri(Guid? contestId = null)
        => new Uri($"api/result_import/{contestId?.ToString() ?? ContestMockedData.IdStGallenEvoting}", UriKind.RelativeOrAbsolute);

    private EVotingImport NewSimpleProportionalElectionImportData(
        Guid? contestId = null,
        Guid? countingCircleId = null)
    {
        var ballots = new[]
        {
                new EVotingElectionBallot(
                    Guid.Parse(ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen),
                    true,
                    Array.Empty<EVotingElectionBallotPosition>()),
        };

        var results = new[]
        {
                new EVotingElectionResult(
                    Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen),
                    countingCircleId ?? CountingCircleMockedData.GuidGossau,
                    ballots),
        };

        return new EVotingImport(
            "my-mock-ech-message-id",
            contestId ?? Guid.Parse(ContestMockedData.IdStGallenEvoting),
            results);
    }

    private EVotingImport NewSimpleMajorityElectionImportData(EVotingElectionBallot[]? ballots = null)
    {
        ballots ??= new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    Array.Empty<EVotingElectionBallotPosition>()),
        };

        var results = new[]
        {
                new EVotingElectionResult(
                    Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen),
                    CountingCircleMockedData.GuidUzwil,
                    ballots),
        };

        return new EVotingImport(
            "my-mock-ech-message-id",
            Guid.Parse(ContestMockedData.IdStGallenEvoting),
            results);
    }

    private EVotingImport NewSimpleVoteImportData()
    {
        var ballots = new[]
        {
                new EVotingVoteBallot(
                    Array.Empty<EVotingVoteBallotQuestionAnswer>(),
                    Array.Empty<EVotingVoteBallotTieBreakQuestionAnswer>()),
        };

        var ballotResults = new[]
        {
                new EVotingVoteBallotResult(
                    Guid.Parse(VoteMockedData.BallotIdGossauVoteInContestStGallen),
                    ballots),
        };

        var results = new[]
        {
                new EVotingVoteResult(
                    Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
                    CountingCircleMockedData.GuidGossau,
                    ballotResults),
        };

        return new EVotingImport(
            "my-mock-ech-message-id",
            Guid.Parse(ContestMockedData.IdStGallenEvoting),
            results);
    }

    private int GetNextEventNumber(int delta = 1)
    {
        var value = _eventNumberCounter;
        _eventNumberCounter += delta;
        return value;
    }

    private async Task<ProportionalElection> GetProportionalElectionWithResults()
    {
        var election = await RunOnDb(
            db => db.ProportionalElections
                .AsSplitQuery()
                .Include(x => x.Results)
                .ThenInclude(x => x.UnmodifiedListResults)
                .Include(x => x.Results)
                .ThenInclude(x => x.ListResults)
                .ThenInclude(x => x.CandidateResults)
                .ThenInclude(x => x.VoteSources)
                .FirstAsync(x =>
                    x.Id == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen)),
            Languages.German);
        SortAndCleanIds(election);
        return election;
    }

    private async Task<MajorityElection> GetMajorityElectionWithResults()
    {
        var election = await RunOnDb(
            db => db.MajorityElections
                .AsSplitQuery()
                .Include(x => x.Results)
                .ThenInclude(x => x.CandidateResults)
                .Include(x => x.SecondaryMajorityElections)
                .Include(x => x.Results)
                .ThenInclude(x => x.CandidateResults)
                .ThenInclude(x => x.Candidate)
                .ThenInclude(x => x.Translations)
                .FirstAsync(x =>
                    x.Id == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen)),
            Languages.German);
        SortAndCleanIds(election);
        return election;
    }

    private async Task<Vote> GetVoteWithResults()
    {
        var vote = await RunOnDb(
            db => db.Votes
                .AsSplitQuery()
                .Include(x => x.Results).ThenInclude(x => x.Results).ThenInclude(x => x.QuestionResults)
                .Include(x => x.Results).ThenInclude(x => x.Results).ThenInclude(x => x.TieBreakQuestionResults)
                .Include(x => x.Results)
                .FirstAsync(x =>
                    x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)),
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
            case CountingCircleResultState.CorrectionDone:
                resultAgg.SubmissionFinished(contestId);
                resultAgg.FlagForCorrection(contestId);
                resultAgg.CorrectionFinished(string.Empty, contestId);
                break;
            case CountingCircleResultState.AuditedTentatively:
                resultAgg.SubmissionFinished(contestId);
                resultAgg.AuditedTentatively(contestId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        await AggregateRepositoryMock.Save(resultAgg);
    }
}
