// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using FluentAssertions;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Ausmittlung.Test.ResultTests;

public abstract class MultiResultBaseTest : BaseTest<ResultService.ResultServiceClient>
{
    protected static readonly Guid ContestId = Guid.Parse(ContestMockedData.IdUzwilEvoting);
    protected static readonly Guid CountingCircleId = CountingCircleMockedData.GuidUzwil;

    protected static readonly Guid VoteId = Guid.Parse(VoteMockedData.IdUzwilVoteInContestUzwilWithoutChilds);
    protected static readonly Guid ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds);
    protected static readonly Guid MajorityElectionId = Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds);

    protected static readonly Guid VoteResultId = VoteResultMockedData.GuidUzwilVoteInContestUzwilResult;
    protected static readonly Guid ProportionalElectionResultId = ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil;
    protected static readonly Guid MajorityElectionResultId = MajorityElectionResultMockedData.GuidUzwilElectionResultInContestUzwil;

    protected MultiResultBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    protected async Task SetResultState(CountingCircleResultState state)
    {
        await RunScoped<IServiceProvider>(async sp =>
        {
            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", SecureConnectTestDefaults.MockedUserDefault.Loginid, "test", Enumerable.Empty<string>());

            var db = sp.GetRequiredService<DataContext>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var aggregateRepo = sp.GetRequiredService<AggregateRepositoryMock>();

            await SetResultState(aggregateRepo, aggregateFactory, state);

            var ccResults = await db.SimpleCountingCircleResults
                .AsTracking()
                .Where(cc => cc.CountingCircle!.SnapshotContestId == ContestId)
                .ToListAsync();

            foreach (var ccResult in ccResults)
            {
                ccResult.State = state;
            }

            await db.SaveChangesAsync();
        });
    }

    protected async Task SetResultState(IAggregateRepository aggregateRepo, IAggregateFactory aggregateFactory, CountingCircleResultState state)
    {
        switch (state)
        {
            case CountingCircleResultState.SubmissionOngoing:
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.StartSubmission(CountingCircleId, VoteId, ContestId, false));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.StartSubmission(CountingCircleId, ProportionalElectionId, ContestId, false));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.StartSubmission(CountingCircleId, MajorityElectionId, ContestId, false));
                break;
            case CountingCircleResultState.SubmissionDone:
                await SetResultState(aggregateRepo, aggregateFactory, CountingCircleResultState.SubmissionOngoing);
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.SubmissionFinished(ContestId));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.SubmissionFinished(ContestId));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.SubmissionFinished(ContestId));
                break;
            case CountingCircleResultState.ReadyForCorrection:
                await SetResultState(aggregateRepo, aggregateFactory, CountingCircleResultState.SubmissionDone);
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.FlagForCorrection(ContestId));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.FlagForCorrection(ContestId));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.FlagForCorrection(ContestId));
                break;
            case CountingCircleResultState.CorrectionDone:
                await SetResultState(aggregateRepo, aggregateFactory, CountingCircleResultState.ReadyForCorrection);
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.CorrectionFinished(string.Empty, ContestId));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.CorrectionFinished(string.Empty, ContestId));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.CorrectionFinished(string.Empty, ContestId));
                break;
            case CountingCircleResultState.AuditedTentatively:
                await SetResultState(aggregateRepo, aggregateFactory, CountingCircleResultState.SubmissionDone);
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.AuditedTentatively(ContestId));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.AuditedTentatively(ContestId));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.AuditedTentatively(ContestId));
                break;
            case CountingCircleResultState.Plausibilised:
                await SetResultState(aggregateRepo, aggregateFactory, CountingCircleResultState.AuditedTentatively);
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.Plausibilise(ContestId));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.Plausibilise(ContestId));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.Plausibilise(ContestId));
                break;
        }
    }

    protected async Task ExecuteOnAggregate<TAggregate>(
        IAggregateRepository aggregateRepo,
        IAggregateFactory aggregateFactory,
        Guid id,
        Action<TAggregate> action)
        where TAggregate : BaseEventSourcingAggregate
    {
        var aggregate = await aggregateRepo.TryGetById<TAggregate>(id)
            ?? aggregateFactory.New<TAggregate>();
        action.Invoke(aggregate);
        await aggregateRepo.Save(aggregate);
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);
}
