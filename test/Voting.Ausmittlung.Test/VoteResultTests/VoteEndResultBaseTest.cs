// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public abstract class VoteEndResultBaseTest : BaseTest<
    VoteResultService.VoteResultServiceClient>
{
    private readonly IReadOnlyCollection<(string ResultId, string CountingCircleId)> _resultIds = new[]
    {
            (VoteEndResultMockedData.GossauResultId, CountingCircleMockedData.IdGossau),
            (VoteEndResultMockedData.StGallenResultId, CountingCircleMockedData.IdStGallen),
            (VoteEndResultMockedData.StGallenAuslandschweizerResultId, CountingCircleMockedData.IdStGallenAuslandschweizer),
            (VoteEndResultMockedData.StGallenHaggenResultId, CountingCircleMockedData.IdStGallenHaggen),
            (VoteEndResultMockedData.StGallenStFidenResultId, CountingCircleMockedData.IdStGallenStFiden),
            (VoteEndResultMockedData.UzwilResultId, CountingCircleMockedData.IdUzwil),
    };

    private readonly PoliticalBusinessCountOfVotersEventData _defaultCountOfVoters = new PoliticalBusinessCountOfVotersEventData
    {
        ConventionalReceivedBallots = 300,
        ConventionalAccountedBallots = 200,
        ConventionalBlankBallots = 50,
        ConventionalInvalidBallots = 50,
    };

    protected VoteEndResultBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await ContestMockedData.Seed(RunScoped);

        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    protected async Task SeedVote(VoteResultAlgorithm resultAlgorithm)
    {
        var vote = VoteEndResultMockedData.BuildVote(resultAlgorithm);
        vote.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(vote.ContestId, vote.DomainOfInfluenceId);

        await RunOnDb(async db =>
        {
            db.Votes.Add(vote);
            await db.SaveChangesAsync();
        });

        await RunScoped((VoteResultBuilder resultBuilder) =>
            resultBuilder.RebuildForVote(Guid.Parse(VoteEndResultMockedData.VoteId), Guid.Parse(DomainOfInfluenceMockedData.IdStGallen), false));

        await RunScoped((SimplePoliticalBusinessBuilder<Vote> builder) => builder.Create(vote));

        await RunScoped((VoteEndResultInitializer endResultInitializer) =>
            endResultInitializer.RebuildForVote(Guid.Parse(VoteEndResultMockedData.VoteId), false));
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    protected async Task StartResultSubmissions()
    {
        foreach (var (resultId, ccId) in _resultIds)
        {
            await TestEventPublisher.Publish(
                GetNextEventNumber(),
                new VoteResultSubmissionStarted
                {
                    VoteId = VoteEndResultMockedData.VoteId,
                    VoteResultId = resultId,
                    CountingCircleId = ccId,
                    EventInfo = GetMockedEventInfo(),
                });
        }
    }

    protected async Task FinishAllResultSubmission()
    {
        foreach (var (resultId, _) in _resultIds)
        {
            await FinishResultSubmission(resultId);
        }
    }

    protected async Task SetAllAuditedTentatively()
    {
        await SetOneAuditedTentatively();
        await SetOtherAuditedTentatively();
    }

    protected Task SetOneAuditedTentatively()
    {
        return SetAuditedTentatively(_resultIds.First().ResultId);
    }

    protected async Task SetOtherAuditedTentatively()
    {
        foreach (var (endResultId, _) in _resultIds.Skip(1))
        {
            await SetAuditedTentatively(endResultId);
        }
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected async Task FinishResultSubmission(
        string resultId,
        (int CountYes, int CountNo)? ballot1QuestionResult = null,
        (int CountYes, int CountNo)? ballot2Question1Result = null,
        (int CountYes, int CountNo)? ballot2Question2Result = null,
        (int CountQ1, int CountQ2, int CountUnspecified)? ballot2TieBreakQuestionResult = null)
    {
        ballot1QuestionResult ??= (15, 10);
        ballot2Question1Result ??= (15, 10);
        ballot2Question2Result ??= (15, 10);
        ballot2TieBreakQuestionResult ??= (25, 15, 10);

        var voteResultEnterEvent = new VoteResultEntered
        {
            VoteResultId = resultId,
            Results =
                {
                    new VoteBallotResultsEventData
                    {
                        BallotId = VoteEndResultMockedData.BallotId1,
                        QuestionResults =
                        {
                            new VoteBallotQuestionResultsEventData
                            {
                                QuestionNumber = 1,
                                ReceivedCountYes = ballot1QuestionResult.Value.CountYes,
                                ReceivedCountNo = ballot1QuestionResult.Value.CountNo,
                            },
                        },
                    },
                    new VoteBallotResultsEventData
                    {
                        BallotId = VoteEndResultMockedData.BallotId2,
                        QuestionResults =
                        {
                            new VoteBallotQuestionResultsEventData
                            {
                                QuestionNumber = 1,
                                ReceivedCountYes = ballot2Question1Result.Value.CountYes,
                                ReceivedCountNo = ballot2Question1Result.Value.CountNo,
                            },
                            new VoteBallotQuestionResultsEventData
                            {
                                QuestionNumber = 2,
                                ReceivedCountYes = ballot2Question2Result.Value.CountYes,
                                ReceivedCountNo = ballot2Question2Result.Value.CountNo,
                            },
                        },
                        TieBreakQuestionResults =
                        {
                            new VoteTieBreakQuestionResultsEventData
                            {
                                QuestionNumber = 1,
                                ReceivedCountQ1 = ballot2TieBreakQuestionResult.Value.CountQ1,
                                ReceivedCountQ2 = ballot2TieBreakQuestionResult.Value.CountQ2,
                                ReceivedCountUnspecified = ballot2TieBreakQuestionResult.Value.CountUnspecified,
                            },
                        },
                    },
                },
            EventInfo = GetMockedEventInfo(),
        };

        await TestEventPublisher.Publish(GetNextEventNumber(), voteResultEnterEvent);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultCountOfVotersEntered
            {
                VoteResultId = resultId,
                ResultsCountOfVoters =
                {
                        new VoteBallotResultsCountOfVotersEventData
                        {
                            BallotId = VoteEndResultMockedData.BallotId1,
                            CountOfVoters = _defaultCountOfVoters,
                        },
                        new VoteBallotResultsCountOfVotersEventData
                        {
                            BallotId = VoteEndResultMockedData.BallotId2,
                            CountOfVoters = _defaultCountOfVoters,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultSubmissionFinished
            {
                VoteResultId = resultId,
                EventInfo = GetMockedEventInfo(),
            });
    }

    private async Task SetAuditedTentatively(string resultId)
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultAuditedTentatively
            {
                VoteResultId = resultId,
                EventInfo = GetMockedEventInfo(),
            });
    }
}
