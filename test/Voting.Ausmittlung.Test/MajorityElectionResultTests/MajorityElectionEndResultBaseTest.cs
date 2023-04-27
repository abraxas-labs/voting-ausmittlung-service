// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public abstract class MajorityElectionEndResultBaseTest : BaseTest<
    MajorityElectionResultService.MajorityElectionResultServiceClient>
{
    private readonly IReadOnlyCollection<string> _resultIds = new List<string>
    {
        MajorityElectionEndResultMockedData.GossauResultId,
        MajorityElectionEndResultMockedData.StGallenResultId,
        MajorityElectionEndResultMockedData.StGallenHaggenResultId,
        MajorityElectionEndResultMockedData.StGallenStFidenResultId,
        MajorityElectionEndResultMockedData.StGallenAuslandschweizerResultId,
        MajorityElectionEndResultMockedData.UzwilResultId,
    };

    private readonly IEnumerable<(string CandidateId, int Count)> _defaultCountByCandidateIds = new List<(string CandidateId, int Count)>
    {
        (MajorityElectionEndResultMockedData.CandidateId1, 200),
        (MajorityElectionEndResultMockedData.CandidateId2, 150),
        (MajorityElectionEndResultMockedData.CandidateId3, 100),
        (MajorityElectionEndResultMockedData.CandidateId4, 100),
        (MajorityElectionEndResultMockedData.CandidateId5, 80),
        (MajorityElectionEndResultMockedData.CandidateId6, 70),
        (MajorityElectionEndResultMockedData.CandidateId7, 60),
        (MajorityElectionEndResultMockedData.CandidateId8, 60),
        (MajorityElectionEndResultMockedData.CandidateId9InBallotGroup, 50),
    };

    private readonly IEnumerable<(string ElectionId, string CandidateId, int Count)> _defaultCountBySecondaryCandidateIds = new List<(string ElectionId, string CandidateId, int Count)>
    {
        (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId1, 200),
        (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId2, 100),
        (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId3, 100),
        (MajorityElectionEndResultMockedData.SecondaryElectionId, MajorityElectionEndResultMockedData.SecondaryCandidateId4InBallotGroup, 80),
        (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId1, 80),
        (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId2, 70),
        (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId3, 60),
        (MajorityElectionEndResultMockedData.SecondaryElectionId2, MajorityElectionEndResultMockedData.Secondary2CandidateId4, 55),
    };

    private readonly PoliticalBusinessCountOfVotersEventData _defaultCountOfVoters = new PoliticalBusinessCountOfVotersEventData
    {
        ConventionalReceivedBallots = 300,
        ConventionalAccountedBallots = 200,
        ConventionalBlankBallots = 50,
        ConventionalInvalidBallots = 50,
    };

    protected MajorityElectionEndResultBaseTest(TestApplicationFactory factory)
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

    protected async Task SeedElectionAndFinishResultSubmissions()
    {
        await SeedElection();
        await StartResultSubmissions();
        await FinishResultSubmissions();
    }

    protected async Task SeedElection()
    {
        await SeedElection(
            MajorityElectionResultEntry.Detailed,
            MajorityElectionMandateAlgorithm.RelativeMajority,
            3,
            2);
    }

    protected async Task SeedElection(
        MajorityElectionResultEntry resultEntry,
        MajorityElectionMandateAlgorithm mandateAlgorithm,
        int primaryElectionNumberOfMandates,
        int secondaryElectionNumberOfMandates)
    {
        var election = MajorityElectionEndResultMockedData.BuildElection(
            resultEntry,
            mandateAlgorithm,
            primaryElectionNumberOfMandates,
            secondaryElectionNumberOfMandates);
        election.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(election.ContestId, election.DomainOfInfluenceId);

        await RunOnDb(async db =>
        {
            db.MajorityElections.Add(election);
            await db.SaveChangesAsync();
        });

        await RunScoped((SimplePoliticalBusinessBuilder<MajorityElection> builder) => builder.Create(election));

        await RunScoped((MajorityElectionResultBuilder resultBuilder) =>
            resultBuilder.RebuildForElection(election.Id, election.DomainOfInfluenceId, false));

        await RunScoped((MajorityElectionEndResultInitializer endResultBuilder) =>
            endResultBuilder.RebuildForElection(election.Id, false));
    }

    protected async Task StartResultSubmissions()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultSubmissionStarted
            {
                ElectionId = MajorityElectionEndResultMockedData.ElectionId,
                ElectionResultId = MajorityElectionEndResultMockedData.GossauResultId,
                CountingCircleId = CountingCircleMockedData.IdGossau,
                EventInfo = GetMockedEventInfo(),
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultSubmissionStarted
            {
                ElectionId = MajorityElectionEndResultMockedData.ElectionId,
                ElectionResultId = MajorityElectionEndResultMockedData.StGallenResultId,
                CountingCircleId = CountingCircleMockedData.IdStGallen,
                EventInfo = GetMockedEventInfo(),
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultSubmissionStarted
            {
                ElectionId = MajorityElectionEndResultMockedData.ElectionId,
                ElectionResultId = MajorityElectionEndResultMockedData.StGallenStFidenResultId,
                CountingCircleId = CountingCircleMockedData.IdStGallenStFiden,
                EventInfo = GetMockedEventInfo(),
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultSubmissionStarted
            {
                ElectionId = MajorityElectionEndResultMockedData.ElectionId,
                ElectionResultId = MajorityElectionEndResultMockedData.StGallenHaggenResultId,
                CountingCircleId = CountingCircleMockedData.IdStGallenHaggen,
                EventInfo = GetMockedEventInfo(),
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultSubmissionStarted
            {
                ElectionId = MajorityElectionEndResultMockedData.ElectionId,
                ElectionResultId = MajorityElectionEndResultMockedData.StGallenAuslandschweizerResultId,
                CountingCircleId = CountingCircleMockedData.IdStGallenAuslandschweizer,
                EventInfo = GetMockedEventInfo(),
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultSubmissionStarted
            {
                ElectionId = MajorityElectionEndResultMockedData.ElectionId,
                ElectionResultId = MajorityElectionEndResultMockedData.UzwilResultId,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
                EventInfo = GetMockedEventInfo(),
            });
    }

    protected async Task FinishResultSubmissions(
        PoliticalBusinessCountOfVotersEventData? countOfVoters = null,
        IEnumerable<(string CandidateId, int Count)>? countByCandidateIds = null,
        IEnumerable<(string ElectionId, string CandidateId, int Count)>? countBySecondaryCandidateIds = null)
    {
        foreach (var resultId in _resultIds)
        {
            await FinishResultSubmission(
                resultId,
                countOfVoters,
                countByCandidateIds,
                countBySecondaryCandidateIds);
        }
    }

    protected async Task SetResultsToAuditedTentatively()
    {
        await SetOneResultToAuditedTentatively();
        await SetOtherResultToAuditedTentatively();
    }

    protected async Task SetOneResultToAuditedTentatively(string? id = null)
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultAuditedTentatively
            {
                ElectionResultId = id ?? _resultIds.First(),
                EventInfo = GetMockedEventInfo(),
            });
    }

    protected async Task SetOtherResultToAuditedTentatively()
    {
        foreach (var resultId in _resultIds.Skip(1))
        {
            await TestEventPublisher.Publish(
                GetNextEventNumber(),
                new MajorityElectionResultAuditedTentatively
                {
                    ElectionResultId = resultId,
                    EventInfo = GetMockedEventInfo(),
                });
        }
    }

    protected async Task ResetOneResultToSubmissionFinished(string electionResultId)
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultResettedToSubmissionFinished
            {
                ElectionResultId = electionResultId,
                EventInfo = GetMockedEventInfo(),
            });
    }

    private async Task FinishResultSubmission(
        string resultId,
        PoliticalBusinessCountOfVotersEventData? countOfVoters = null,
        IEnumerable<(string CandidateId, int Count)>? countByCandidateIds = null,
        IEnumerable<(string ElectionId, string CandidateId, int Count)>? countBySecondaryCandidateIds = null)
    {
        countByCandidateIds ??= _defaultCountByCandidateIds;
        countBySecondaryCandidateIds ??= _defaultCountBySecondaryCandidateIds;
        countOfVoters ??= _defaultCountOfVoters;

        var candidateResultCounts = countByCandidateIds.Select(countByCandidateId =>
            new MajorityElectionCandidateResultCountEventData { CandidateId = countByCandidateId.CandidateId, VoteCount = countByCandidateId.Count });

        var secondaryCandidateResultCounts = countBySecondaryCandidateIds
            .GroupBy(x => x.ElectionId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(countByCandidateId =>
            new MajorityElectionCandidateResultCountEventData { CandidateId = countByCandidateId.CandidateId, VoteCount = countByCandidateId.Count }));

        var candidateResultsEnterEvent = new MajorityElectionCandidateResultsEntered
        {
            IndividualVoteCount = 3,
            InvalidVoteCount = 5,
            EmptyVoteCount = 13,
            ElectionResultId = resultId,
            CandidateResults = { candidateResultCounts },
            SecondaryElectionCandidateResults =
                {
                    new SecondaryMajorityElectionCandidateResultsEventData
                    {
                        IndividualVoteCount = 1,
                        SecondaryMajorityElectionId = MajorityElectionEndResultMockedData.SecondaryElectionId,
                        CandidateResults = { secondaryCandidateResultCounts[MajorityElectionEndResultMockedData.SecondaryElectionId] },
                    },
                    new SecondaryMajorityElectionCandidateResultsEventData
                    {
                        IndividualVoteCount = 2,
                        SecondaryMajorityElectionId = MajorityElectionEndResultMockedData.SecondaryElectionId2,
                        CandidateResults = { secondaryCandidateResultCounts[MajorityElectionEndResultMockedData.SecondaryElectionId2] },
                    },
                },
            EventInfo = GetMockedEventInfo(),
        };
        await TestEventPublisher.Publish(GetNextEventNumber(), candidateResultsEnterEvent);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultCountOfVotersEntered
            {
                ElectionResultId = resultId,
                CountOfVoters = countOfVoters,
                EventInfo = GetMockedEventInfo(),
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultSubmissionFinished
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
}
