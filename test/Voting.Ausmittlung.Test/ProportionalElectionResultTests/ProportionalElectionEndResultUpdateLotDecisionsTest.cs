// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionEndResultUpdateLotDecisionsTest : ProportionalElectionEndResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionEndResultUpdateLotDecisionsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedElectionAndFinishSubmissions();
    }

    [Fact]
    public async Task TestProcessor()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        var endResultId = "e51853c0-e16c-4143-b629-5ab58ec14637";

        await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            x => x.Candidate.ProportionalElectionListId == Guid.Parse(ProportionalElectionEndResultMockedData.ListId1),
            x => x.State = ProportionalElectionCandidateEndResultState.NotElected);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionListEndResultLotDecisionsUpdated
            {
                ProportionalElectionEndResultId = endResultId,
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                ProportionalElectionListId = ProportionalElectionEndResultMockedData.ListId1,
                LotDecisions =
                {
                        new ProportionalElectionEndResultLotDecisionEventData
                        {
                            CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId2,
                            Rank = 2,
                        },
                        new ProportionalElectionEndResultLotDecisionEventData
                        {
                            CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId3,
                            Rank = 3,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
        });
        var listEndResult = endResult.ListEndResults.First();
        listEndResult.MatchSnapshot("response");
        listEndResult.CandidateEndResults.Any(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.Elected).Should().BeTrue();

        var availableLotDecisions = await MonitoringElectionAdminClient.GetListEndResultAvailableLotDecisionsAsync(
            new GetProportionalElectionListEndResultAvailableLotDecisionsRequest
            {
                ProportionalElectionListId = ProportionalElectionEndResultMockedData.ListId1,
            });
        availableLotDecisions.MatchSnapshot("availableLotDecisions");
    }

    [Fact]
    public async Task TestProcessorWithNullRanks()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        var endResultId = "e51853c0-e16c-4143-b629-5ab58ec14637";

        await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            x => x.Candidate.ProportionalElectionListId == Guid.Parse(ProportionalElectionEndResultMockedData.ListId1),
            x =>
            {
                x.State = ProportionalElectionCandidateEndResultState.NotElected;
                if (x.CandidateId == Guid.Parse(ProportionalElectionEndResultMockedData.List1CandidateId2))
                {
                    x.Rank = 3;
                    x.LotDecision = true;
                }

                if (x.CandidateId == Guid.Parse(ProportionalElectionEndResultMockedData.List1CandidateId3))
                {
                    x.Rank = 2;
                    x.LotDecision = true;
                }
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionListEndResultLotDecisionsUpdated
            {
                ProportionalElectionEndResultId = endResultId,
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                ProportionalElectionListId = ProportionalElectionEndResultMockedData.ListId1,
                LotDecisions =
                {
                        new ProportionalElectionEndResultLotDecisionEventData
                        {
                            CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId2,
                            Rank = null,
                        },
                        new ProportionalElectionEndResultLotDecisionEventData
                        {
                            CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId3,
                            Rank = null,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
        });
        var listEndResult = endResult.ListEndResults.First(x => x.List.Id == ProportionalElectionEndResultMockedData.ListId1);

        var candidate2 = listEndResult.CandidateEndResults.Single(x => x.Candidate.Id == ProportionalElectionEndResultMockedData.List1CandidateId2);
        candidate2.LotDecision.Should().BeFalse();
        candidate2.Rank.Should().Be(2);
        candidate2.State.Should().Be(SharedProto.ProportionalElectionCandidateEndResultState.NotElected);

        var candidate3 = listEndResult.CandidateEndResults.Single(x => x.Candidate.Id == ProportionalElectionEndResultMockedData.List1CandidateId3);
        candidate3.LotDecision.Should().BeFalse();
        candidate3.Rank.Should().Be(2);
        candidate3.State.Should().Be(SharedProto.ProportionalElectionCandidateEndResultState.NotElected);
    }

    [Fact]
    public async Task TestProcessorWithManualEndResult()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();

        var endResultId = "e51853c0-e16c-4143-b629-5ab58ec14637";

        await ModifyDbEntities<ProportionalElectionEndResult>(
            x => x.ProportionalElectionId == ProportionalElectionEndResultMockedData.ElectionGuid,
            x => x.ManualEndResultRequired = true);

        await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            x => x.Candidate.ProportionalElectionListId == Guid.Parse(ProportionalElectionEndResultMockedData.ListId1),
            x => x.State = ProportionalElectionCandidateEndResultState.NotElected);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionListEndResultLotDecisionsUpdated
            {
                ProportionalElectionEndResultId = endResultId,
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                ProportionalElectionListId = ProportionalElectionEndResultMockedData.ListId1,
                LotDecisions =
                {
                        new ProportionalElectionEndResultLotDecisionEventData
                        {
                            CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId2,
                            Rank = 2,
                        },
                        new ProportionalElectionEndResultLotDecisionEventData
                        {
                            CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId3,
                            Rank = 3,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
        });
        endResult.ManualEndResultRequired.Should().BeTrue();
        var listEndResult = endResult.ListEndResults.First();
        listEndResult.MatchSnapshot("response");

        // the candidate end result should not change if it is a manual end result
        listEndResult.CandidateEndResults.Any(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.Elected).Should().BeFalse();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListEndResultLotDecisionsUpdated>();
        eventData.MatchSnapshot("event", x => x.ProportionalElectionEndResultId);
    }

    [Fact]
    public async Task TestShouldReturnAfterTestingPhaseEnded()
    {
        var request = NewValidRequest();
        var electionId = Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId);
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        // testing phase
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListEndResultLotDecisionsUpdated>();
        await RunEvents<ProportionalElectionListEndResultLotDecisionsUpdated>();

        var endResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, false);
        evInTestingPhase.ProportionalElectionEndResultId.Should().Be(endResultInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(GetNextEventNumber(), new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        var endResultTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, true);

        await ModifyDbEntities<ProportionalElectionEndResult>(
            e => e.ProportionalElectionId == electionId,
            e =>
            {
                e.CountOfDoneCountingCircles = e.TotalCountOfCountingCircles;
                e.MandateDistributionTriggered = true;
            });
        await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            e => e.ListEndResult.ElectionEndResultId == endResultTestingPhaseEndedId,
            e =>
            {
                e.LotDecisionEnabled = true;
                e.LotDecisionRequired = false;
            });

        request.LotDecisions.Add(new UpdateProportionalElectionEndResultLotDecisionRequest
        {
            CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId1,
            Rank = 1,
        });

        await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListEndResultLotDecisionsUpdated>();
        await RunEvents<ProportionalElectionListEndResultLotDecisionsUpdated>();

        evTestingPhaseEnded.ProportionalElectionEndResultId.Should().Be(endResultTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await SetAllAuditedTentatively();
            await TriggerMandateDistribution();
            await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListEndResultLotDecisionsUpdated>();
        });
    }

    [Fact]
    public async Task TestShouldThrowIfLotDecisionWithSameVoteCountIsMissing()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();

        var request = NewValidRequest(x =>
        {
            x.LotDecisions.Clear();
            x.LotDecisions.Add(new UpdateProportionalElectionEndResultLotDecisionRequest
            {
                CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId2,
                Rank = null,
            });
        });

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(request),
            StatusCode.InvalidArgument,
            "A related lot decision of the group with vote count");
    }

    [Fact]
    public async Task TestShouldThrowIfLotDecisionWithSameVoteCountHasMixedNullAndNonNullRanks()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();

        var request = NewValidRequest(x =>
        {
            x.LotDecisions.Clear();
            x.LotDecisions.Add(new UpdateProportionalElectionEndResultLotDecisionRequest
            {
                CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId2,
                Rank = null,
            });
            x.LotDecisions.Add(new UpdateProportionalElectionEndResultLotDecisionRequest
            {
                CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId3,
                Rank = 1,
            });
        });

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(request),
            StatusCode.InvalidArgument,
            "Either all related lot decisions of the group with vote count");
    }

    [Fact]
    public async Task TestShouldThrowIfRankIsOutOfAllowedRange()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(new UpdateProportionalElectionListEndResultLotDecisionsRequest
            {
                ProportionalElectionListId = ProportionalElectionEndResultMockedData.ListId1,
                LotDecisions =
                {
                        new UpdateProportionalElectionEndResultLotDecisionRequest
                        {
                            CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId2,
                            Rank = 2,
                        },
                        new UpdateProportionalElectionEndResultLotDecisionRequest
                        {
                            CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId3,
                            Rank = 4,
                        },
                },
            }),
            StatusCode.InvalidArgument,
            "bad rank or rank already taken in existing lot decisions");
    }

    [Fact]
    public async Task TestShouldThrowIfDuplicateRankInSameElection()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(
                NewValidRequest(x =>
                {
                    x.LotDecisions.Clear();
                    x.LotDecisions.Add(new UpdateProportionalElectionEndResultLotDecisionRequest
                    {
                        CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId2,
                        Rank = 3,
                    });
                    x.LotDecisions.Add(new UpdateProportionalElectionEndResultLotDecisionRequest
                    {
                        CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId3,
                        Rank = 3,
                    });
                })),
            StatusCode.InvalidArgument,
            "bad rank or rank already taken in existing lot decisions");
    }

    [Fact]
    public async Task TestShouldThrowIfDuplicateCandidate()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(
                NewValidRequest(x => x.LotDecisions.Add(new UpdateProportionalElectionEndResultLotDecisionRequest
                {
                    CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId3,
                    Rank = 2,
                }))),
            StatusCode.InvalidArgument,
            "a candidate may only appear once in the lot decisions");
    }

    [Fact]
    public async Task TestShouldThrowIfNoLotDecisions()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(
                NewValidRequest(x => x.LotDecisions.Clear())),
            StatusCode.InvalidArgument,
            "must contain at least one lot decision");
    }

    [Fact]
    public async Task TestShouldThrowIfElectionMandateDistributionNotStarted()
    {
        await SetAllAuditedTentatively();

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "lot decisions are not allowed on this end result");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await SetAllAuditedTentatively();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(
                NewValidRequest(x => x.ProportionalElectionListId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateListEndResultLotDecisionsAsync(
                NewValidRequest(x => x.ProportionalElectionListId = IdBadFormat)),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        ResetDb();
        await InitializeAsync();
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .UpdateListEndResultLotDecisionsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private UpdateProportionalElectionListEndResultLotDecisionsRequest NewValidRequest(
        Action<UpdateProportionalElectionListEndResultLotDecisionsRequest>? customizer = null)
    {
        var r = new UpdateProportionalElectionListEndResultLotDecisionsRequest
        {
            ProportionalElectionListId = ProportionalElectionEndResultMockedData.ListId1,
            LotDecisions =
                {
                    new UpdateProportionalElectionEndResultLotDecisionRequest
                    {
                        CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId2,
                        Rank = 2,
                    },
                    new UpdateProportionalElectionEndResultLotDecisionRequest
                    {
                        CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId3,
                        Rank = 3,
                    },
                },
        };
        customizer?.Invoke(r);
        return r;
    }
}
