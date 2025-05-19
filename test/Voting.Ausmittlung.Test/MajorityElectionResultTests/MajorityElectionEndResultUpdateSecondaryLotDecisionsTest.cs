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

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionEndResultUpdateSecondaryLotDecisionsTest : MajorityElectionEndResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionEndResultUpdateSecondaryLotDecisionsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedElectionAndFinishResultSubmissions();
    }

    [Fact]
    public async Task TestProcessor()
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();
        var endResultId = "e51853c0-e16c-4143-b629-5ab58ec14637";

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionEndResultSecondaryLotDecisionsUpdated
            {
                MajorityElectionEndResultId = endResultId,
                MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
                LotDecisions =
                {
                        new MajorityElectionEndResultLotDecisionEventData
                        {
                            CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId2,
                            Rank = 2,
                        },
                        new MajorityElectionEndResultLotDecisionEventData
                        {
                            CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId3,
                            Rank = 3,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetMajorityElectionEndResultRequest
        {
            MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
        });
        endResult.MatchSnapshot("response");

        var availableLotDecisions = await MonitoringElectionAdminClient.GetEndResultAvailableLotDecisionsAsync(
            new GetMajorityElectionEndResultAvailableLotDecisionsRequest
            {
                MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
            });
        availableLotDecisions.MatchSnapshot("availableLotDecisions");

        await AssertHasPublishedEventProcessedMessage(MajorityElectionEndResultSecondaryLotDecisionsUpdated.Descriptor, Guid.Parse(endResultId));
    }

    [Fact]
    public async Task TestProcessorWithNullRanks()
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();
        var endResultId = "e51853c0-e16c-4143-b629-5ab58ec14637";

        await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
            x => x.Candidate.SecondaryMajorityElection.PrimaryMajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x =>
            {
                if (x.CandidateId == Guid.Parse(MajorityElectionEndResultMockedData.SecondaryCandidateId2))
                {
                    x.Rank = 2;
                    x.LotDecision = true;
                }

                if (x.CandidateId == Guid.Parse(MajorityElectionEndResultMockedData.SecondaryCandidateId3))
                {
                    x.Rank = 3;
                    x.LotDecision = true;
                }
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionEndResultSecondaryLotDecisionsUpdated
            {
                MajorityElectionEndResultId = endResultId,
                MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
                LotDecisions =
                {
                        new MajorityElectionEndResultLotDecisionEventData
                        {
                            CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId2,
                            Rank = null,
                        },
                        new MajorityElectionEndResultLotDecisionEventData
                        {
                            CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId3,
                            Rank = null,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetMajorityElectionEndResultRequest
        {
            MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
        });

        var candidate2 = endResult.SecondaryMajorityElectionEndResults
            .SelectMany(x => x.CandidateEndResults)
            .Single(x => x.Candidate.Id == MajorityElectionEndResultMockedData.SecondaryCandidateId2);

        candidate2.Rank.Should().Be(2);
        candidate2.LotDecision.Should().BeFalse();

        var candidate3 = endResult.SecondaryMajorityElectionEndResults
            .SelectMany(x => x.CandidateEndResults)
            .Single(x => x.Candidate.Id == MajorityElectionEndResultMockedData.SecondaryCandidateId3);

        candidate3.Rank.Should().Be(2);
        candidate3.LotDecision.Should().BeFalse();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();
        await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEndResultSecondaryLotDecisionsUpdated>();
        eventData.MatchSnapshot("event", x => x.MajorityElectionEndResultId);
    }

    [Fact]
    public async Task TestShouldReturnAfterTestingPhaseEnded()
    {
        var request = NewValidRequest();
        var electionId = Guid.Parse(request.MajorityElectionId);
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        // testing phase
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();
        await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEndResultSecondaryLotDecisionsUpdated>();
        await RunEvents<MajorityElectionEndResultSecondaryLotDecisionsUpdated>();

        var endResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, false);
        evInTestingPhase.MajorityElectionEndResultId.Should().Be(endResultInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(GetNextEventNumber(), new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        var endResultTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, true);

        await ModifyDbEntities<MajorityElectionEndResult>(
            e => e.MajorityElectionId == electionId,
            e => e.CountOfDoneCountingCircles = e.TotalCountOfCountingCircles);
        await ModifyDbEntities<MajorityElectionCandidateEndResult>(
            e => e.MajorityElectionEndResultId == endResultTestingPhaseEndedId,
            e =>
            {
                e.LotDecisionEnabled = true;

                if (e.CandidateId == Guid.Parse(MajorityElectionEndResultMockedData.CandidateId3) ||
                    e.CandidateId == Guid.Parse(MajorityElectionEndResultMockedData.CandidateId4))
                {
                    e.ConventionalVoteCount = 100;
                }
            });
        await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
            e => e.SecondaryMajorityElectionEndResult.PrimaryMajorityElectionEndResultId == endResultTestingPhaseEndedId,
            e =>
            {
                e.LotDecisionEnabled = true;

                if (e.CandidateId == Guid.Parse(MajorityElectionEndResultMockedData.SecondaryCandidateId2) ||
                    e.CandidateId == Guid.Parse(MajorityElectionEndResultMockedData.SecondaryCandidateId3))
                {
                    e.ConventionalVoteCount = 100;
                }
            });

        foreach (var lotDecision in request.LotDecisions)
        {
            lotDecision.Rank = null;
        }

        await SetPrimaryRequiredLotDecisions();

        await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEndResultSecondaryLotDecisionsUpdated>();
        await RunEvents<MajorityElectionEndResultSecondaryLotDecisionsUpdated>();

        evTestingPhaseEnded.MajorityElectionEndResultId.Should().Be(endResultTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await SetResultsToAuditedTentatively();
            await SetPrimaryRequiredLotDecisions();
            await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionEndResultSecondaryLotDecisionsUpdated>();
        });
    }

    [Fact]
    public async Task TestShouldThrowIfLotDecisionWithSameVoteCountIsMissing()
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();

        var request = NewValidRequest(x =>
        {
            x.LotDecisions.Clear();
            x.LotDecisions.Add(new UpdateMajorityElectionEndResultLotDecisionRequest
            {
                CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId2,
                Rank = null,
            });
        });

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(request),
            StatusCode.InvalidArgument,
            "A related lot decision of the group with vote count");
    }

    [Fact]
    public async Task TestShouldThrowIfLotDecisionWithSameVoteCountHasMixedNullAndNonNullRanks()
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();

        var request = NewValidRequest(x =>
        {
            x.LotDecisions.Clear();
            x.LotDecisions.Add(new UpdateMajorityElectionEndResultLotDecisionRequest
            {
                CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId2,
                Rank = 2,
            });
            x.LotDecisions.Add(new UpdateMajorityElectionEndResultLotDecisionRequest
            {
                CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId3,
                Rank = null,
            });
        });

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(request),
            StatusCode.InvalidArgument,
            "Either all related lot decisions of the group with vote count");
    }

    [Fact]
    public async Task TestShouldThrowIfRankIsOutOfAllowedRange()
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(new UpdateMajorityElectionEndResultSecondaryLotDecisionsRequest
            {
                MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
                LotDecisions =
                {
                    new UpdateMajorityElectionEndResultLotDecisionRequest
                    {
                        CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId2,
                        Rank = 3,
                    },
                    new UpdateMajorityElectionEndResultLotDecisionRequest
                    {
                        CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId3,
                        Rank = 4,
                    },
                },
            }),
            StatusCode.InvalidArgument,
            "bad rank or rank already taken in existing lot decisions");
    }

    [Fact]
    public async Task TestShouldThrowWithPrimaryCandidates()
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(NewValidRequest(x =>
            {
                x.LotDecisions.Add(new UpdateMajorityElectionEndResultLotDecisionRequest
                {
                    CandidateId = MajorityElectionEndResultMockedData.CandidateId3,
                    Rank = 3,
                });
            })),
            StatusCode.InvalidArgument,
            "candidate id found which not exists in available lot decisions");
    }

    [Fact]
    public async Task TestShouldThrowIfRequiredPrimaryLotDecisionsNotSet()
    {
        await SetResultsToAuditedTentatively();

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Cannot set secondary lot decisions if the primary required lot decisions are not set yet");
    }

    [Fact]
    public async Task TestShouldThrowIfDuplicateRankInSameElection()
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(
                NewValidRequest(x =>
                {
                    x.LotDecisions.Clear();
                    x.LotDecisions.Add(new UpdateMajorityElectionEndResultLotDecisionRequest
                    {
                        CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId2,
                        Rank = 3,
                    });
                    x.LotDecisions.Add(new UpdateMajorityElectionEndResultLotDecisionRequest
                    {
                        CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId3,
                        Rank = 3,
                    });
                })),
            StatusCode.InvalidArgument,
            "bad rank or rank already taken in existing lot decisions");
    }

    [Fact]
    public async Task TestShouldThrowIfContestLocked()
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();

        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowIfDuplicateCandidate()
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(
                NewValidRequest(x => x.LotDecisions.Add(new UpdateMajorityElectionEndResultLotDecisionRequest
                {
                    CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId2,
                    Rank = 9,
                }))),
            StatusCode.InvalidArgument,
            "a candidate may only appear once in the lot decisions");
    }

    [Fact]
    public async Task TestShouldThrowIfNoLotDecisions()
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(
                NewValidRequest(x => x.LotDecisions.Clear())),
            StatusCode.InvalidArgument,
            "must contain at least one lot decision");
    }

    [Fact]
    public async Task TestShouldThrowIfElectionCountingCircleNotAuditedTentatively()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "lot decisions are not allowed on this end result");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(
                NewValidRequest(x => x.MajorityElectionId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultSecondaryLotDecisionsAsync(
                NewValidRequest(x => x.MajorityElectionId = IdBadFormat)),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await SetResultsToAuditedTentatively();
        await SetPrimaryRequiredLotDecisions();
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .UpdateEndResultSecondaryLotDecisionsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private async Task SetPrimaryRequiredLotDecisions()
    {
        await ModifyDbEntities<MajorityElectionCandidateEndResult>(
            x => x.CandidateId == Guid.Parse(MajorityElectionEndResultMockedData.CandidateId3),
            x =>
            {
                x.Rank = 4;
                x.LotDecision = true;
            });

        await ModifyDbEntities<MajorityElectionCandidateEndResult>(
           x => x.CandidateId == Guid.Parse(MajorityElectionEndResultMockedData.CandidateId4),
           x =>
           {
               x.Rank = 3;
               x.LotDecision = true;
           });
    }

    private UpdateMajorityElectionEndResultSecondaryLotDecisionsRequest NewValidRequest(
        Action<UpdateMajorityElectionEndResultSecondaryLotDecisionsRequest>? customizer = null)
    {
        var r = new UpdateMajorityElectionEndResultSecondaryLotDecisionsRequest
        {
            MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
            LotDecisions =
                {
                    new UpdateMajorityElectionEndResultLotDecisionRequest
                    {
                        CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId2,
                        Rank = 2,
                    },
                    new UpdateMajorityElectionEndResultLotDecisionRequest
                    {
                        CandidateId = MajorityElectionEndResultMockedData.SecondaryCandidateId3,
                        Rank = 3,
                    },
                },
        };
        customizer?.Invoke(r);
        return r;
    }
}
