// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public abstract class ProportionalElectionEndResultBaseTest : BaseTest<
    ProportionalElectionResultService.ProportionalElectionResultServiceClient>
{
    private const string DefaultBundleId = "2867c687-689f-4ffa-a078-e167bc7467c2";

    private readonly PoliticalBusinessCountOfVotersEventData _defaultCountOfVoters = new PoliticalBusinessCountOfVotersEventData
    {
        ConventionalReceivedBallots = 1000,
        ConventionalAccountedBallots = 900,
        ConventionalBlankBallots = 50,
        ConventionalInvalidBallots = 50,
    };

    private readonly (string CountingCircleId, string ElectionEndResultId)[] _countingCircleElectionResultIdPairs =
    {
            (CountingCircleMockedData.IdGossau, ProportionalElectionEndResultMockedData.GossauResultId),
            (CountingCircleMockedData.IdStGallen, ProportionalElectionEndResultMockedData.StGallenResultId),
            (CountingCircleMockedData.IdUzwil, ProportionalElectionEndResultMockedData.UzwilResultId),
            (CountingCircleMockedData.IdStGallenHaggen, ProportionalElectionEndResultMockedData.StGallenHaggenResultId),
            (CountingCircleMockedData.IdStGallenStFiden, ProportionalElectionEndResultMockedData.StGallenStFidenResultId),
            (CountingCircleMockedData.IdStGallenAuslandschweizer, ProportionalElectionEndResultMockedData.StGallenAuslandschweizerResultId),
    };

    protected ProportionalElectionEndResultBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await ContestMockedData.Seed(RunScoped);
        await SecondFactorTransactionMockedData.Seed(RunScoped);

        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    protected async Task SeedElectionAndFinishSubmissions()
    {
        await SeedElection();
        await StartResultSubmissions();
        await FinishAllResultSubmission();
    }

    protected async Task SeedElection()
    {
        await SeedElection(ProportionalElectionMandateAlgorithm.HagenbachBischoff, 5);
    }

    protected async Task SeedElection(ProportionalElectionMandateAlgorithm mandateAlgorithm, int numberOfMandates)
    {
        await ProportionalElectionEndResultMockedData.Seed(RunScoped, mandateAlgorithm, numberOfMandates);
    }

    protected async Task StartResultSubmissions()
    {
        foreach (var (countingCircleId, resultId) in _countingCircleElectionResultIdPairs)
        {
            await TestEventPublisher.Publish(
                GetNextEventNumber(),
                new ProportionalElectionResultSubmissionStarted
                {
                    ElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                    ElectionResultId = resultId,
                    CountingCircleId = countingCircleId,
                    EventInfo = new EventInfo
                    {
                        Timestamp = new Timestamp
                        {
                            Seconds = 1594980476,
                        },
                        Tenant = SecureConnectTestDefaults.MockedTenantGossau.ToEventInfoTenant(),
                        User = new() { Id = TestDefaults.UserId },
                    },
                });
        }
    }

    protected async Task FinishAllResultSubmission()
    {
        await FinishResultSubmission(
            _countingCircleElectionResultIdPairs[0].ElectionEndResultId,
            DefaultBundleId);

        foreach (var (_, endResultId) in _countingCircleElectionResultIdPairs.Skip(1))
        {
            await FinishResultSubmission(endResultId);
        }
    }

    protected async Task SetAllAuditedTentatively()
    {
        await SetOneAuditedTentatively();
        await SetOtherAuditedTentatively();
    }

    protected Task SetOneAuditedTentatively()
    {
        return SetAuditedTentatively(_countingCircleElectionResultIdPairs[0].ElectionEndResultId);
    }

    protected async Task SetOtherAuditedTentatively()
    {
        foreach (var (_, endResultId) in _countingCircleElectionResultIdPairs.Skip(1))
        {
            await SetAuditedTentatively(endResultId);
        }
    }

    private async Task SetAuditedTentatively(string resultId)
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultAuditedTentatively
            {
                ElectionResultId = resultId,
                EventInfo = GetMockedEventInfo(),
            });
    }

    private async Task FinishResultSubmission(
        string resultId,
        string? bundleIdToCreate = null)
    {
        await EnterUnmodifiedListResult(resultId);

        if (!string.IsNullOrEmpty(bundleIdToCreate))
        {
            await CreateBundle(resultId, bundleIdToCreate);
            await CreateBallot(resultId, bundleIdToCreate);
            await FinishBundleSubmissionAndSetReviewed(resultId, bundleIdToCreate);
        }

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultCountOfVotersEntered
            {
                ElectionResultId = resultId,
                CountOfVoters = _defaultCountOfVoters,
                EventInfo = GetMockedEventInfo(),
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultSubmissionFinished
            {
                ElectionResultId = resultId,
                EventInfo = new EventInfo
                {
                    Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                    User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                    Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                },
            });
    }

    private async Task EnterUnmodifiedListResult(string resultId)
    {
        var unmodifiedListResultsEnterEvent = new ProportionalElectionUnmodifiedListResultsEntered
        {
            ElectionResultId = resultId,
            Results =
                {
                    new ProportionalElectionUnmodifiedListResultEventData
                    {
                        ListId = ProportionalElectionEndResultMockedData.ListId1,
                        VoteCount = 1000,
                    },
                    new ProportionalElectionUnmodifiedListResultEventData
                    {
                        ListId = ProportionalElectionEndResultMockedData.ListId2,
                        VoteCount = 1000,
                    },
                    new ProportionalElectionUnmodifiedListResultEventData
                    {
                        ListId = ProportionalElectionEndResultMockedData.ListId3,
                        VoteCount = 1000,
                    },
                    new ProportionalElectionUnmodifiedListResultEventData
                    {
                        ListId = ProportionalElectionEndResultMockedData.ListId4,
                        VoteCount = 1000,
                    },
                },
            EventInfo = GetMockedEventInfo(),
        };

        await TestEventPublisher.Publish(GetNextEventNumber(), unmodifiedListResultsEnterEvent);
    }

    private async Task CreateBundle(string resultId, string bundleId)
    {
        var bundleCreatedEvent = new ProportionalElectionResultBundleCreated
        {
            BundleId = bundleId,
            ElectionResultId = resultId,
            ListId = ProportionalElectionEndResultMockedData.ListId1,
            BundleNumber = 1,
            ResultEntryParams = new ProportionalElectionResultEntryParamsEventData
            {
                BallotBundleSize = 2,
            },
            EventInfo = new EventInfo
            {
                Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                User = new() { Id = "creator" },
                Tenant = new() { Id = "tenant" },
            },
        };

        await TestEventPublisher.Publish(GetNextEventNumber(), bundleCreatedEvent);
    }

    private async Task CreateBallot(string resultId, string bundleId)
    {
        var ballotCreatedEvent = new ProportionalElectionResultBallotCreated
        {
            BundleId = bundleId,
            ElectionResultId = resultId,
            BallotNumber = 1,
            EmptyVoteCount = 2,
            Candidates =
                {
                    new ProportionalElectionResultBallotUpdatedCandidateEventData
                    {
                        CandidateId = ProportionalElectionEndResultMockedData.List1CandidateId1,
                        Position = 1,
                        OnList = true,
                    },
                    new ProportionalElectionResultBallotUpdatedCandidateEventData
                    {
                        CandidateId = ProportionalElectionEndResultMockedData.List3CandidateId1,
                        Position = 2,
                        OnList = false,
                    },
                },
            EventInfo = GetMockedEventInfo(),
        };

        await TestEventPublisher.Publish(GetNextEventNumber(), ballotCreatedEvent);
    }

    private async Task FinishBundleSubmissionAndSetReviewed(string resultId, string bundleId)
    {
        var bundleSubmissionFinishedEvent = new ProportionalElectionResultBundleSubmissionFinished
        {
            BundleId = bundleId,
            ElectionResultId = resultId,
            EventInfo = new EventInfo
            {
                Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
            },
        };

        var bundleReviewedEvent = new ProportionalElectionResultBundleReviewSucceeded
        {
            BundleId = bundleId,
            EventInfo = new EventInfo
            {
                Timestamp = new DateTime(2020, 01, 10, 10, 20, 0, DateTimeKind.Utc).ToTimestamp(),
                User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
            },
        };

        await TestEventPublisher.Publish(GetNextEventNumber(), bundleSubmissionFinishedEvent);
        await TestEventPublisher.Publish(GetNextEventNumber(), bundleReviewedEvent);
    }
}
