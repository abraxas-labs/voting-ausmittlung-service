﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
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

public class MajorityElectionEndResultUpdateLotDecisionsTest : MajorityElectionEndResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionEndResultUpdateLotDecisionsTest(TestApplicationFactory factory)
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
        var endResultId = "e51853c0-e16c-4143-b629-5ab58ec14637";

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionEndResultLotDecisionsUpdated
            {
                MajorityElectionEndResultId = endResultId,
                MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
                LotDecisions =
                {
                        new MajorityElectionEndResultLotDecisionEventData
                        {
                            CandidateId = MajorityElectionEndResultMockedData.CandidateId3,
                            Rank = 4,
                        },
                        new MajorityElectionEndResultLotDecisionEventData
                        {
                            CandidateId = MajorityElectionEndResultMockedData.CandidateId4,
                            Rank = 3,
                        },
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
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await SetResultsToAuditedTentatively();
        await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEndResultLotDecisionsUpdated>();
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
        await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEndResultLotDecisionsUpdated>();
        await RunEvents<MajorityElectionEndResultLotDecisionsUpdated>();

        var endResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, false);
        evInTestingPhase.MajorityElectionEndResultId.Should().Be(endResultInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
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
                e.LotDecisionRequired = false;
            });
        await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
            e => e.SecondaryMajorityElectionEndResult.PrimaryMajorityElectionEndResultId == endResultTestingPhaseEndedId,
            e =>
            {
                e.LotDecisionEnabled = true;
                e.LotDecisionRequired = false;
            });

        await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEndResultLotDecisionsUpdated>();
        await RunEvents<MajorityElectionEndResultLotDecisionsUpdated>();

        evTestingPhaseEnded.MajorityElectionEndResultId.Should().Be(endResultTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await SetResultsToAuditedTentatively();
            await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionEndResultLotDecisionsUpdated>();
        });
    }

    [Fact]
    public async Task TestShouldThrowIfRequiredLotDecisionIsMissing()
    {
        await SetResultsToAuditedTentatively();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(
                NewValidRequest(x =>
                {
                    x.LotDecisions.Clear();
                    x.LotDecisions.Add(new UpdateMajorityElectionEndResultLotDecisionRequest
                    {
                        CandidateId = MajorityElectionEndResultMockedData.CandidateId4,
                        Rank = 4,
                    });
                })),
            StatusCode.InvalidArgument,
            "required lot decision is missing");
    }

    [Fact]
    public async Task TestShouldThrowIfRankIsOutOfAllowedRange()
    {
        await SetResultsToAuditedTentatively();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(new UpdateMajorityElectionEndResultLotDecisionsRequest
            {
                MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
                LotDecisions =
                {
                        new UpdateMajorityElectionEndResultLotDecisionRequest
                        {
                            CandidateId = MajorityElectionEndResultMockedData.CandidateId3,
                            Rank = 3,
                        },
                        new UpdateMajorityElectionEndResultLotDecisionRequest
                        {
                            CandidateId = MajorityElectionEndResultMockedData.CandidateId4,
                            Rank = 2,
                        },
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
            }),
            StatusCode.InvalidArgument,
            "bad rank or rank already taken in existing lot decisions");
    }

    [Fact]
    public async Task TestShouldThrowIfDuplicateRankInSameElection()
    {
        await SetResultsToAuditedTentatively();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(
                NewValidRequest(x =>
                {
                    x.LotDecisions.Clear();
                    x.LotDecisions.Add(new UpdateMajorityElectionEndResultLotDecisionRequest
                    {
                        CandidateId = MajorityElectionEndResultMockedData.CandidateId3,
                        Rank = 3,
                    });
                    x.LotDecisions.Add(new UpdateMajorityElectionEndResultLotDecisionRequest
                    {
                        CandidateId = MajorityElectionEndResultMockedData.CandidateId4,
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
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowIfDuplicateCandidate()
    {
        await SetResultsToAuditedTentatively();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(
                NewValidRequest(x => x.LotDecisions.Add(new UpdateMajorityElectionEndResultLotDecisionRequest
                {
                    CandidateId = MajorityElectionEndResultMockedData.CandidateId4,
                    Rank = 9,
                }))),
            StatusCode.InvalidArgument,
            "a candidate may only appear once in the lot decisions");
    }

    [Fact]
    public async Task TestShouldThrowIfNoLotDecisions()
    {
        await SetResultsToAuditedTentatively();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(
                NewValidRequest(x => x.LotDecisions.Clear())),
            StatusCode.InvalidArgument,
            "must contain at least one lot decision");
    }

    [Fact]
    public async Task TestShouldThrowIfElectionCountingCircleNotAuditedTentatively()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "lot decisions are not allowed on this end result");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(
                NewValidRequest(x => x.MajorityElectionId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultLotDecisionsAsync(
                NewValidRequest(x => x.MajorityElectionId = IdBadFormat)),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .UpdateEndResultLotDecisionsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private UpdateMajorityElectionEndResultLotDecisionsRequest NewValidRequest(
        Action<UpdateMajorityElectionEndResultLotDecisionsRequest>? customizer = null)
    {
        var r = new UpdateMajorityElectionEndResultLotDecisionsRequest
        {
            MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
            LotDecisions =
                {
                    new UpdateMajorityElectionEndResultLotDecisionRequest
                    {
                        CandidateId = MajorityElectionEndResultMockedData.CandidateId3,
                        Rank = 4,
                    },
                    new UpdateMajorityElectionEndResultLotDecisionRequest
                    {
                        CandidateId = MajorityElectionEndResultMockedData.CandidateId4,
                        Rank = 3,
                    },
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
