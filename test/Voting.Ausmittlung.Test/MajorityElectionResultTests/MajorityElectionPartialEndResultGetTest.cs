// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using MajorityElectionCandidateResult = Voting.Ausmittlung.Core.Domain.MajorityElectionCandidateResult;
using PoliticalBusinessCountOfVoters = Voting.Ausmittlung.Core.Domain.PoliticalBusinessCountOfVoters;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionPartialEndResultGetTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";

    public MajorityElectionPartialEndResultGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);

        // Two results where the DOI Stadt St. Gallen is their "responsible" DOI
        await EnterResults(CountingCircleMockedData.GuidStGallenHaggen);
        await EnterResults(CountingCircleMockedData.GuidStGallenStFiden);

        await RunOnDb(db => db.DomainOfInfluences
            .Where(doi => doi.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallenStadt.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(doi => doi.ViewCountingCirclePartialResults, true)));
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        var endResult = await MonitoringElectionAdminClient.GetPartialEndResultAsync(NewValidRequest());
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminAfterSubmissionDone()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        var endResult = await MonitoringElectionAdminClient.GetPartialEndResultAsync(NewValidRequest());
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task DomainOfInfluenceWithoutFlagShouldThrow()
    {
        var client = CreateService(DomainOfInfluenceMockedData.Gossau.SecureConnectId, TestDefaults.UserId, RolesMockedData.MonitoringElectionAdmin);
        await AssertStatus(
            async () => await client.GetPartialEndResultAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetPartialEndResultAsync(
                new GetMajorityElectionPartialEndResultRequest
                {
                    MajorityElectionId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .GetPartialEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, DomainOfInfluenceMockedData.StGallenStadt.SecureConnectId, TestDefaults.UserId, roles);

    private GetMajorityElectionPartialEndResultRequest NewValidRequest()
    {
        return new GetMajorityElectionPartialEndResultRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        };
    }

    private async Task EnterResults(Guid countingCircleId)
    {
        var permissionService = GetService<Core.Services.Permission.PermissionService>();
        permissionService.SetAbraxasAuthIfNotAuthenticated();

        var aggregateFactory = GetService<IAggregateFactory>();
        var aggregate = aggregateFactory.New<MajorityElectionResultAggregate>();
        aggregate.StartSubmission(
            countingCircleId,
            MajorityElectionMockedData.StGallenMajorityElectionInContestBund.Id,
            ContestMockedData.GuidBundesurnengang,
            false);
        aggregate.DefineEntry(
            MajorityElectionResultEntry.FinalResults,
            ContestMockedData.GuidBundesurnengang);
        aggregate.EnterCountOfVoters(
            new PoliticalBusinessCountOfVoters
            {
                ConventionalReceivedBallots = 50,
                ConventionalInvalidBallots = 3,
                ConventionalAccountedBallots = 40,
                ConventionalBlankBallots = 7,
            },
            ContestMockedData.GuidBundesurnengang);
        aggregate.EnterCandidateResults(
            1,
            2,
            3,
            new List<MajorityElectionCandidateResult>
            {
                new()
                {
                    CandidateId = Guid.Parse(MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund),
                    VoteCount = 10,
                },
                new()
                {
                    CandidateId = Guid.Parse(MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund),
                    VoteCount = 5,
                },
            },
            new List<SecondaryMajorityElectionCandidateResults>
            {
                new()
                {
                    SecondaryMajorityElectionId = Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund),
                    IndividualVoteCount = 2,
                    InvalidVoteCount = 3,
                    EmptyVoteCount = 4,
                    CandidateResults = new List<MajorityElectionCandidateResult>
                    {
                        new()
                        {
                            CandidateId = Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund),
                            VoteCount = 1,
                        },
                        new()
                        {
                            CandidateId = Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund),
                            VoteCount = 2,
                        },
                    },
                },
            },
            ContestMockedData.GuidBundesurnengang);
        var aggregateRepo = GetService<IAggregateRepository>();
        await aggregateRepo.Save(aggregate);

        await RunEvents<MajorityElectionResultSubmissionStarted>(false);
        await RunEvents<MajorityElectionResultEntryDefined>(false);
        await RunEvents<MajorityElectionResultCountOfVotersEntered>(false);
        await RunEvents<MajorityElectionCandidateResultsEntered>();
    }
}
