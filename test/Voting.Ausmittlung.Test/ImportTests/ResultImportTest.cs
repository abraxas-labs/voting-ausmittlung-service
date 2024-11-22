// (c) Copyright by Abraxas Informatik AG
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
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Messaging.Messages;
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
using MajorityElectionWriteInMappingTarget = Abraxas.Voting.Ausmittlung.Shared.V1.MajorityElectionWriteInMappingTarget;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultImportTest : BaseRestTest
{
    private const string TestFileOk = "ImportTests/ExampleFiles/ech0222_import_ok.xml";
    private const string TestFileInvalid = "ImportTests/ExampleFiles/ech0222_import_invalid.xml";
    private const string TestFileOtherContestId = "ImportTests/ExampleFiles/ech0222_import_other_contest_id.xml";
    private const string TestFileEch0110Ok = "ImportTests/ExampleFiles/ech0110_import_ok.xml";

    private static readonly ResultImportMeta ImportMeta = new(
        Guid.Parse(ContestMockedData.IdUzwilEvoting),
        "test-eCH-0222.xml",
        Stream.Null,
        "test-eCH-0110.xml",
        Stream.Null);

    private readonly ResultImportWriter _importWriter;
    private bool _initializedAuthorizationTest;

    public ResultImportTest(TestApplicationFactory factory)
        : base(factory)
    {
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

        using var resp = await MonitoringElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk), ("ech0110File", TestFileEch0110Ok));
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

        var importedVotingCard = EventPublisherMock.GetPublishedEvents<CountingCircleVotingCardsImported>().First();
        importedVotingCard.ImportId.Should().Be(importId);
        importedVotingCard.MatchSnapshot("votingCards", x => x.ImportId);

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

            using var resp = await MonitoringElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk), ("ech0110File", TestFileEch0110Ok));
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

            var events = new List<EventWithMetadata>
            {
                EventPublisherMock.GetSinglePublishedEventWithMetadata<ResultImportStarted>(),
            };
            events.AddRange(EventPublisherMock.GetPublishedEventsWithMetadata<ProportionalElectionResultImported>());
            events.AddRange(EventPublisherMock.GetPublishedEventsWithMetadata<VoteResultImported>());
            events.AddRange(EventPublisherMock.GetPublishedEventsWithMetadata<MajorityElectionResultImported>());
            events.AddRange(EventPublisherMock.GetPublishedEventsWithMetadata<SecondaryMajorityElectionResultImported>());
            events.AddRange(EventPublisherMock.GetPublishedEventsWithMetadata<CountingCircleVotingCardsImported>());
            events.Add(EventPublisherMock.GetSinglePublishedEventWithMetadata<ResultImportCompleted>());
            return events.ToArray();
        });
    }

    [Fact]
    public async Task InvalidImportFileShouldThrow()
    {
        await AssertProblemDetails(
            () => MonitoringElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileInvalid), ("ech0110File", TestFileEch0110Ok)),
            HttpStatusCode.BadRequest,
            "List of possible elements expected: 'senderId'");
    }

    [Fact]
    public async Task ContestLockedShouldThrow()
    {
        await ModifyDbEntities<Contest>(
            x => x.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting),
            x => x.State = ContestState.PastLocked);
        await AssertProblemDetails(
            () => MonitoringElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk), ("ech0110File", TestFileEch0110Ok)),
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
            () => MonitoringElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk), ("ech0110File", TestFileEch0110Ok)),
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
            () => MonitoringElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk), ("ech0110File", TestFileEch0110Ok)),
            HttpStatusCode.FailedDependency,
            $"eVoting is not active on the CountingCircle with the id {CountingCircleMockedData.IdGossau}");
    }

    [Fact]
    public Task OtherTenantShouldThrow()
    {
        var otherTenantClient = base.CreateHttpClient(
            tenant: SecureConnectTestDefaults.MockedTenantUzwil.Id,
            roles: RolesMockedData.MonitoringElectionAdmin);
        return AssertStatus(
            () => otherTenantClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk), ("ech0110File", TestFileEch0110Ok)),
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CountingCircleResultAuditedShouldThrow()
    {
        // set a state to submission finished
        await SetProportionalElectionResultState(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen, CountingCircleResultState.AuditedTentatively);

        await AssertProblemDetails(
            () => MonitoringElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk), ("ech0110File", TestFileEch0110Ok)),
            HttpStatusCode.FailedDependency,
            $"A result is in an invalid state for an eVoting import to be possible ({ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen})");
    }

    [Fact]
    public Task OtherContestIdShouldThrow()
    {
        return AssertProblemDetails(
            () => MonitoringElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOtherContestId), ("ech0110File", TestFileEch0110Ok)),
            HttpStatusCode.BadRequest,
            "contestIds do not match");
    }

    [Fact]
    public Task MissingEch0110ShouldThrow()
    {
        return AssertProblemDetails(
            () => MonitoringElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOtherContestId)),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public Task ContestNotFoundShouldThrow()
    {
        var req = NewValidProportionalElectionImportData(Guid.NewGuid());
        var votingCards = NewValidVotingCardImportData();
        var countOfVotersInformation = NewValidCountOfVotersImportData();
        return Assert.ThrowsAsync<EntityNotFoundException>(() => _importWriter.Import(req, votingCards, countOfVotersInformation, ImportMeta));
    }

    [Fact]
    public async Task UnknownCountingCirclesShouldBeIgnored()
    {
        TrySetFakeAuth();

        var results = new List<EVotingPoliticalBusinessResult>()
        {
            new EVotingElectionResult(
                Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds),
                "10007", // A "test counting circle"
                new EVotingElectionBallot[]
                {
                    new(null, false, Array.Empty<EVotingElectionBallotPosition>()),
                }),
            new EVotingElectionResult(
                Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds),
                "unknown-counting-circle-id",
                new EVotingElectionBallot[]
                {
                    new(null, false, Array.Empty<EVotingElectionBallotPosition>()),
                }),
            new EVotingElectionResult(
                Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds),
                CountingCircleMockedData.IdUzwil,
                new EVotingElectionBallot[]
                {
                    new(null, false, Array.Empty<EVotingElectionBallotPosition>()),
                }),
        };

        var ccVotingCards = new List<EVotingCountingCircleVotingCards>
        {
            new("10007", 134),
            new("unknown-counting-circle-id", 555),
            new(CountingCircleMockedData.IdUzwil, 123),
        };

        var import = new EVotingImport(
            "my-mock-ech-message-id",
            Guid.Parse(ContestMockedData.IdUzwilEvoting),
            results);
        var votingCards = new EVotingVotingCardImport("mock-eCH-0110-message-id", Guid.Parse(ContestMockedData.IdUzwilEvoting), ccVotingCards);
        var countOfVotersInformation = NewValidCountOfVotersImportData();

        await _importWriter.Import(import, votingCards, countOfVotersInformation, ImportMeta);

        var importStarted = EventPublisherMock.GetSinglePublishedEvent<ResultImportStarted>();
        importStarted.MatchSnapshot("event", x => x.ImportId);

        var vcEvent = EventPublisherMock.GetSinglePublishedEvent<CountingCircleVotingCardsImported>();
        vcEvent.CountingCircleVotingCards.Should().HaveCount(1);

        await RunEvents<ResultImportStarted>();
        var importInDb = await RunOnDb(db => db.ResultImports
            .Include(x => x.IgnoredCountingCircles.OrderBy(c => c.CountingCircleId))
            .FirstAsync(x => x.Id == Guid.Parse(importStarted.ImportId)));

        foreach (var ignoredCountingCircle in importInDb.IgnoredCountingCircles)
        {
            ignoredCountingCircle.Id = Guid.Empty;
            ignoredCountingCircle.ResultImportId = Guid.Empty;
        }

        importInDb.MatchSnapshot("db", x => x.Id);
    }

    [Fact]
    public Task DuplicateCountingCircleResultsShouldThrow()
    {
        TrySetFakeAuth();

        var results = new List<EVotingPoliticalBusinessResult>()
        {
            new EVotingElectionResult(
                Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen),
                CountingCircleMockedData.IdUzwil,
                new EVotingElectionBallot[]
                {
                    new(null, false, Array.Empty<EVotingElectionBallotPosition>()),
                }),
            new EVotingElectionResult(
                Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen),
                CountingCircleMockedData.IdUzwil,
                new EVotingElectionBallot[]
                {
                    new(null, false, Array.Empty<EVotingElectionBallotPosition>()),
                }),
        };

        var import = new EVotingImport(
            "my-mock-ech-message-id",
            Guid.Parse(ContestMockedData.IdUzwilEvoting),
            results);
        var votingCardReq = NewValidVotingCardImportData();
        var countOfVotersInformation = NewValidCountOfVotersImportData();

        return AssertException<ValidationException>(
            () => _importWriter.Import(import, votingCardReq, countOfVotersInformation, ImportMeta),
            "Duplicate counting circle results provided");
    }

    [Fact]
    public async Task MajorityElectionShouldMapDuplicatesToInvalidIfInvalidAvailable()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidUzwilEvoting,
            x => x.MajorityElectionInvalidVotes = true,
            true);

        TrySetFakeAuth();

        var positions = new[]
        {
                EVotingElectionBallotPosition.ForWriteIn("hans mueller"),
                EVotingElectionBallotPosition.ForWriteIn("Hans Mueller"),
                EVotingElectionBallotPosition.ForCandidateId(Guid.Parse(MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestUzwil)),
                EVotingElectionBallotPosition.ForCandidateId(Guid.Parse(MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestUzwil)),
        };
        var req = NewValidMajorityElectionImportData(new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    positions),
        });

        var votingCardReq = NewValidVotingCardImportData();
        var countOfVotersInformation = NewValidCountOfVotersImportData(
            basisCountingCircleId: CountingCircleMockedData.IdUzwil,
            politicalBusinessIds: new() { MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds });

        await _importWriter.Import(req, votingCardReq, countOfVotersInformation, ImportMeta);

        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultImported>();
        ev.EmptyVoteCount.Should().Be(1); // 1 from missing position
        ev.InvalidVoteCount.Should().Be(1); // 1 from candidate duplicate
        ev.CandidateResults
            .Single(x => x.CandidateId == MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestUzwil)
            .VoteCount
            .Should()
            .Be(1);

        var writeInBallotEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionWriteInBallotImported>().ToList();
        writeInBallotEvents.Should().HaveCount(1);
        var writeInBallot = writeInBallotEvents[0];
        writeInBallot.CandidateIds.Should().HaveCount(1);
        writeInBallot.WriteInMappingIds.Should().HaveCount(2);
        writeInBallot.InvalidVoteCount.Should().Be(1); // same as in MajorityElectionResultImported event, since it is only one ballot
        writeInBallot.EmptyVoteCount.Should().Be(1);

        await RunEvents<MajorityElectionResultImported>(false);
        await RunEvents<MajorityElectionWriteInBallotImported>(false);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionWriteInsMapped
            {
                CountingCircleId = CountingCircleMockedData.IdUzwil,
                MajorityElectionId = MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds,
                WriteInMappings =
                {
                    new MajorityElectionWriteInMappedEventData
                    {
                        CandidateId = MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestUzwil,
                        WriteInMappingId = ev.WriteIns[0].WriteInMappingId,
                        Target = MajorityElectionWriteInMappingTarget.Candidate,
                    },
                },
            });

        var result = await RunOnDb(async db =>
        {
            return await db.MajorityElectionResults
                .Include(r => r.CandidateResults)
                .FirstAsync(r => r.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil
                    && r.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds));
        });

        result.InvalidVoteCount.Should().Be(3); // 1 from duplicated candidate, 2 from mapped duplicated write-ins
        result.CountOfElectionsWithUnmappedWriteIns.Should().Be(0);
        result.HasUnmappedWriteIns.Should().BeFalse();

        var candidateResult = result.CandidateResults.First();
        candidateResult.VoteCount.Should().Be(1);
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
                EVotingElectionBallotPosition.ForCandidateId(Guid.Parse(MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestUzwil)),
        };
        var req = NewValidMajorityElectionImportData(new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    positions),
        });

        var votingCardReq = NewValidVotingCardImportData();
        var countOfVotersInformation = NewValidCountOfVotersImportData(
            basisCountingCircleId: CountingCircleMockedData.IdUzwil,
            politicalBusinessIds: new() { MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds });

        await _importWriter.Import(req, votingCardReq, countOfVotersInformation, ImportMeta);

        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultImported>();
        ev.EmptyVoteCount.Should().Be(1); // 1 from missing positions
        ev.InvalidVoteCount.Should().Be(0);
        ev.CandidateResults
            .Single(x => x.CandidateId == MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestUzwil)
            .VoteCount
            .Should()
            .Be(1);

        var writeInBallotEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionWriteInBallotImported>().ToList();
        writeInBallotEvents.Should().HaveCount(1);
        var writeInBallot = writeInBallotEvents[0];
        writeInBallot.CandidateIds.Should().HaveCount(1);
        writeInBallot.WriteInMappingIds.Should().HaveCount(3);
        writeInBallot.InvalidVoteCount.Should().Be(0);
        writeInBallot.EmptyVoteCount.Should().Be(1); // same as in MajorityElectionResultImported event, since it is only one ballot

        await RunEvents<MajorityElectionResultImported>(false);
        await RunEvents<MajorityElectionWriteInBallotImported>(false);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionWriteInsMapped
            {
                CountingCircleId = CountingCircleMockedData.IdUzwil,
                MajorityElectionId = MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds,
                WriteInMappings =
                {
                    new MajorityElectionWriteInMappedEventData
                    {
                        CandidateId = MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestUzwil,
                        WriteInMappingId = ev.WriteIns[0].WriteInMappingId,
                        Target = MajorityElectionWriteInMappingTarget.Candidate,
                    },
                    new MajorityElectionWriteInMappedEventData
                    {
                        CandidateId = MajorityElectionMockedData.CandidateIdUzwilMajorityElectionInContestUzwil,
                        WriteInMappingId = ev.WriteIns[1].WriteInMappingId,
                        Target = MajorityElectionWriteInMappingTarget.Candidate,
                    },
                },
            });

        var result = await RunOnDb(async db =>
        {
            return await db.MajorityElectionResults
                .Include(r => r.CandidateResults)
                .FirstAsync(r => r.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil
                                 && r.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds));
        });

        result.EmptyVoteCount.Should().Be(4); // 1 from missing positions, 3 from mapped duplicated write-ins
        result.InvalidVoteCount.Should().Be(0);
        result.CountOfElectionsWithUnmappedWriteIns.Should().Be(0);
        result.HasUnmappedWriteIns.Should().BeFalse();

        var candidateResult = result.CandidateResults.First();
        candidateResult.VoteCount.Should().Be(1);
    }

    [Fact]
    public Task MajorityElectionImportWithTooManyPositionsShouldThrow()
    {
        TrySetFakeAuth();
        var ballotPositions = Enumerable.Repeat(EVotingElectionBallotPosition.Empty, 10).ToList();
        var req = NewValidMajorityElectionImportData(new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    ballotPositions),
        });
        var votingCardReq = NewValidVotingCardImportData();
        var countOfVotersInformation = NewValidCountOfVotersImportData(
            basisCountingCircleId: CountingCircleMockedData.IdUzwil,
            politicalBusinessIds: new() { MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds });
        return AssertException<ValidationException>(
            () => _importWriter.Import(req, votingCardReq, countOfVotersInformation, ImportMeta),
            "the number of ballot positions exceeds the number of mandates (10 vs 5)");
    }

    [Fact]
    public Task ProportionalElectionImportWithTooManyPositionsShouldThrow()
    {
        TrySetFakeAuth();
        var req = NewValidProportionalElectionImportData();
        var result = req.PoliticalBusinessResults.OfType<EVotingElectionResult>().First();
        var candidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId11UzwilProportionalElectionInContestUzwil);
        var ballotPositions = Enumerable.Repeat(EVotingElectionBallotPosition.ForCandidateId(candidateId), 10).ToList();
        result.Ballots = new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    ballotPositions),
        };
        return AssertException<ValidationException>(
            () => _importWriter.Import(req, NewValidVotingCardImportData(), NewValidCountOfVotersImportData(), ImportMeta),
            "the number of ballot positions exceeds the number of mandates (10 vs 5)");
    }

    [Fact]
    public Task ProportionalElectionImportWithWriteInShouldThrow()
    {
        TrySetFakeAuth();
        var req = NewValidProportionalElectionImportData();
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
            () => _importWriter.Import(req, NewValidVotingCardImportData(), NewValidCountOfVotersImportData(), ImportMeta),
            "proportional election ballot position cannot contain write-ins");
    }

    [Fact]
    public Task ProportionalElectionImportWithUnmodifiedUnknownListShouldThrow()
    {
        TrySetFakeAuth();
        var req = NewValidProportionalElectionImportData();
        var ballot = req.PoliticalBusinessResults.OfType<EVotingElectionResult>().First().Ballots.First();
        ballot.Unmodified = true;
        ballot.ListId = Guid.Parse("4869ef1b-0497-4e24-8e01-a03013ed79a2");
        return Assert.ThrowsAsync<EntityNotFoundException>(() => _importWriter.Import(req, NewValidVotingCardImportData(), NewValidCountOfVotersImportData(), ImportMeta));
    }

    [Fact]
    public async Task ProportionalElectionImportWithUnmodifiedBallotNoListShouldWork()
    {
        TrySetFakeAuth();
        var req = NewValidProportionalElectionImportData();
        var ballot = req.PoliticalBusinessResults.OfType<EVotingElectionResult>().First().Ballots.First();
        ballot.Unmodified = true;
        ballot.ListId = null;
        await _importWriter.Import(req, NewValidVotingCardImportData(), NewValidCountOfVotersImportData(), ImportMeta);

        var proportionalElectionImported = EventPublisherMock.GetPublishedEvents<ProportionalElectionResultImported>().Single();
        proportionalElectionImported.BlankBallotCount.Should().Be(1);
    }

    [Fact]
    public Task ProportionalElectionImportWithTripledCandidateShouldThrow()
    {
        TrySetFakeAuth();
        var candidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId2UzwilProportionalElectionInContestUzwil);
        var ballotPositions = new[]
        {
                EVotingElectionBallotPosition.ForCandidateId(candidateId),
                EVotingElectionBallotPosition.ForCandidateId(candidateId),
                EVotingElectionBallotPosition.ForCandidateId(candidateId),
        };
        var req = NewValidProportionalElectionImportData();
        var result = req.PoliticalBusinessResults.OfType<EVotingElectionResult>().First();
        result.Ballots = new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    ballotPositions),
        };
        return AssertException<ValidationException>(
            () => _importWriter.Import(req, NewValidVotingCardImportData(), NewValidCountOfVotersImportData(), ImportMeta),
            "was found more than 2 times on a single ballot");
    }

    [Fact]
    public Task VoteImportWithUnknownQuestionNumberThrow()
    {
        TrySetFakeAuth();
        var req = NewValidVoteImportData();
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

        return Assert.ThrowsAsync<EntityNotFoundException>(() => _importWriter.Import(req, NewValidVotingCardImportData(), NewValidCountOfVotersImportData(), ImportMeta));
    }

    [Fact]
    public Task VoteImportWithUnknownTieBreakQuestionNumberThrow()
    {
        TrySetFakeAuth();
        var req = NewValidVoteImportData();
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

        return Assert.ThrowsAsync<EntityNotFoundException>(() => _importWriter.Import(req, NewValidVotingCardImportData(), NewValidCountOfVotersImportData(), ImportMeta));
    }

    [Fact]
    public Task DuplicateCountOfVotersInformationShouldThrow()
    {
        TrySetFakeAuth();
        var ballotPositions = Enumerable.Repeat(EVotingElectionBallotPosition.Empty, 10).ToList();
        var req = NewValidMajorityElectionImportData(new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    ballotPositions),
        });
        var votingCardReq = NewValidVotingCardImportData();
        var countOfVotersInformation = NewValidCountOfVotersImportData(
            politicalBusinessIds: new()
            {
                MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds,
                MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds,
            });
        return AssertException<ValidationException>(
            () => _importWriter.Import(req, votingCardReq, countOfVotersInformation, ImportMeta),
            "Duplicate count of voters information provided");
    }

    [Fact]
    public Task MissingCountOfVotersInformationShouldThrow()
    {
        TrySetFakeAuth();
        var ballotPositions = Enumerable.Repeat(EVotingElectionBallotPosition.Empty, 10).ToList();
        var req = NewValidMajorityElectionImportData(new[]
        {
                new EVotingElectionBallot(
                    null,
                    false,
                    ballotPositions),
        });
        var votingCardReq = NewValidVotingCardImportData();
        var countOfVotersInformation = NewValidCountOfVotersImportData(
            basisCountingCircleId: CountingCircleMockedData.IdUzwil,
            politicalBusinessIds: new());
        return AssertException<ValidationException>(
            () => _importWriter.Import(req, votingCardReq, countOfVotersInformation, ImportMeta),
            "Import does not contain count of voters information for counting circle ca7be031-d4ce-4bb6-9538-b5e5d19e5a3e and political business 4aebd757-9f88-4a76-90b4-497dc64adb6f");
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        using var resp = await MonitoringElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk), ("ech0110File", TestFileEch0110Ok));
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

        await RunEvents<ProportionalElectionResultImported>(false);
        await RunEvents<VoteResultImported>(false);
        await RunEvents<MajorityElectionResultImported>(false);
        await RunEvents<SecondaryMajorityElectionResultImported>(false);
        await RunEvents<CountingCircleVotingCardsImported>(false);

        var vote = await GetVoteWithResults();
        vote.Results.MatchSnapshot("voteResults");

        var proportionalElection = await GetProportionalElectionWithResults();
        proportionalElection.Results.MatchSnapshot("proportionalElectionResults");

        var majorityElection = await GetMajorityElectionWithResults();
        majorityElection.Results.MatchSnapshot("majorityElectionResults");

        var countingCircleDetails = await GetCountingCircleDetails();
        countingCircleDetails.MatchSnapshot("countingCircleDetails");

        var importCompleted = EventPublisherMock.GetSinglePublishedEvent<ResultImportCompleted>();
        await TestEventPublisher.Publish(GetNextEventNumber(), importCompleted);

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

        var contestId = Guid.Parse(ContestMockedData.IdStGallenEvoting);
        await AssertHasPublishedMessage<ResultImportChanged>(x =>
            x.ContestId == contestId
            && x.CountingCircleId == CountingCircleMockedData.GuidUzwil
            && x.HasWriteIns);

        await AssertHasPublishedMessage<ResultImportChanged>(x =>
            x.ContestId == contestId
            && x.CountingCircleId == CountingCircleMockedData.GuidGossau
            && !x.HasWriteIns);
    }

    [Fact]
    public async Task ProcessorImportTwiceShouldOverwrite()
    {
        async Task<string> RunImport()
        {
            using var resp = await MonitoringElectionAdminClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk), ("ech0110File", TestFileEch0110Ok));
            resp.EnsureSuccessStatusCode();

            var importCreated = EventPublisherMock.GetSinglePublishedEvent<ResultImportCreated>();
            await TestEventPublisher.Publish(GetNextEventNumber(), importCreated);

            var importStarted = EventPublisherMock.GetSinglePublishedEvent<ResultImportStarted>();
            await TestEventPublisher.Publish(GetNextEventNumber(), importStarted);

            await RunEvents<ProportionalElectionResultImported>(false);
            await RunEvents<VoteResultImported>(false);
            await RunEvents<MajorityElectionResultImported>(false);
            await RunEvents<SecondaryMajorityElectionResultImported>(false);
            await RunEvents<CountingCircleVotingCardsImported>(false);

            var importCompleted = EventPublisherMock.GetSinglePublishedEvent<ResultImportCompleted>();
            await TestEventPublisher.Publish(GetNextEventNumber(), importCompleted);
            return importStarted.ImportId;
        }

        async Task AssertResults()
        {
            var proportionalElection = await GetProportionalElectionWithResults();
            var proportionalElectionResult = proportionalElection.Results.First();
            proportionalElectionResult.EVotingSubTotal.TotalCountOfBallots.Should().Be(4);
            proportionalElectionResult.TotalCountOfBallots.Should().Be(4);

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

    protected override HttpClient CreateHttpClient(
        bool authorize = true,
        string? tenant = "000000000000000000",
        string? userId = "default-user-id",
        params string[] roles)
        => base.CreateHttpClient(authorize, SecureConnectTestDefaults.MockedTenantStGallen.Id, userId, roles);

    protected override async Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        if (!_initializedAuthorizationTest)
        {
            _initializedAuthorizationTest = true;

            // set a state to submission finished
            await SetProportionalElectionResultState(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen, CountingCircleResultState.SubmissionDone);
            await SetProportionalElectionResultState(ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil, CountingCircleResultState.CorrectionDone);
        }

        return await httpClient.PostFiles(BuildUri(), ("ech0222File", TestFileOk), ("ech0110File", TestFileEch0110Ok));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
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
            new[] { RolesMockedData.MonitoringElectionAdmin });
    }

    private Uri BuildUri(Guid? contestId = null)
        => new($"api/result_import/{contestId?.ToString() ?? ContestMockedData.IdStGallenEvoting}", UriKind.RelativeOrAbsolute);

    private EVotingImport NewValidProportionalElectionImportData(Guid? contestId = null)
    {
        var ballots = new[]
        {
            new EVotingElectionBallot(
                Guid.Parse(ProportionalElectionMockedData.ListId2UzwilProportionalElectionInContestUzwil),
                true,
                Array.Empty<EVotingElectionBallotPosition>()),
        };

        var results = new List<EVotingPoliticalBusinessResult>()
        {
            new EVotingElectionResult(
                Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds),
                CountingCircleMockedData.IdUzwil,
                ballots),
        };

        return new EVotingImport(
            "my-mock-ech-message-id",
            contestId ?? Guid.Parse(ContestMockedData.IdUzwilEvoting),
            results);
    }

    private EVotingImport NewValidMajorityElectionImportData(EVotingElectionBallot[]? ballots = null)
    {
        ballots ??= new[]
        {
            new EVotingElectionBallot(
                null,
                false,
                Array.Empty<EVotingElectionBallotPosition>()),
        };

        var results = new List<EVotingPoliticalBusinessResult>()
        {
            new EVotingElectionResult(
                Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds),
                CountingCircleMockedData.IdUzwil,
                ballots),
        };

        return new EVotingImport(
            "my-mock-ech-message-id",
            Guid.Parse(ContestMockedData.IdUzwilEvoting),
            results);
    }

    private EVotingImport NewValidVoteImportData()
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
                Guid.Parse(VoteMockedData.BallotIdUzwilVoteInContestUzwil),
                ballots),
        };

        var results = new List<EVotingPoliticalBusinessResult>()
        {
            new EVotingVoteResult(
                Guid.Parse(VoteMockedData.IdUzwilVoteInContestUzwilWithoutChilds),
                CountingCircleMockedData.IdUzwil,
                ballotResults),
        };

        return new EVotingImport(
            "my-mock-ech-message-id",
            Guid.Parse(ContestMockedData.IdUzwilEvoting),
            results);
    }

    private EVotingVotingCardImport NewValidVotingCardImportData()
    {
        var votingCards = new List<EVotingCountingCircleVotingCards>
        {
            new(CountingCircleMockedData.IdUzwil, 123),
        };

        return new EVotingVotingCardImport(
            "my-mock-ech-message-id-0110",
            Guid.Parse(ContestMockedData.IdUzwilEvoting),
            votingCards);
    }

    private EVotingCountOfVotersInformationImport NewValidCountOfVotersImportData(
        Guid? contestId = null,
        string? basisCountingCircleId = null,
        List<string>? politicalBusinessIds = null)
    {
        politicalBusinessIds ??= new()
        {
            VoteMockedData.IdUzwilVoteInContestUzwilWithoutChilds,
            ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds,
        };

        basisCountingCircleId ??= CountingCircleMockedData.IdUzwil;

        var countOfVotersInformation = politicalBusinessIds
            .ConvertAll(x => new EVotingCountingCircleResultCountOfVotersInformation(basisCountingCircleId, GuidParser.Parse(x), 123))
;

        return new EVotingCountOfVotersInformationImport(
            contestId ?? Guid.Parse(ContestMockedData.IdUzwilEvoting),
            countOfVotersInformation);
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

    private async Task<List<ContestCountingCircleDetails>> GetCountingCircleDetails()
    {
        var ccDetails = await RunOnDb(
            db => db.ContestCountingCircleDetails
                .AsSplitQuery()
                .Include(x => x.VotingCards)
                .Include(x => x.CountOfVotersInformationSubTotals) // include these to ensure they are not modified
                .Where(x => x.ContestId == ContestMockedData.StGallenEvotingUrnengang.Id && x.VotingCards.Count > 0)
                .ToListAsync());

        foreach (var ccDetail in ccDetails)
        {
            ccDetail.OrderVotingCardsAndSubTotals();

            foreach (var votingCard in ccDetail.VotingCards)
            {
                votingCard.Id = Guid.Empty;
            }

            foreach (var subTotal in ccDetail.CountOfVotersInformationSubTotals)
            {
                subTotal.Id = Guid.Empty;
            }
        }

        return ccDetails;
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
