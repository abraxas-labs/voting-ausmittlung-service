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
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using PoliticalBusinessCountOfVoters = Voting.Ausmittlung.Core.Domain.PoliticalBusinessCountOfVoters;
using ProportionalElectionResultEntryParams = Voting.Ausmittlung.Core.Domain.ProportionalElectionResultEntryParams;
using ProportionalElectionUnmodifiedListResult = Voting.Ausmittlung.Core.Domain.ProportionalElectionUnmodifiedListResult;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionPartialEndResultGetTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";

    public ProportionalElectionPartialEndResultGetTest(TestApplicationFactory factory)
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
                new GetProportionalElectionPartialEndResultRequest
                {
                    ProportionalElectionId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .GetPartialEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, DomainOfInfluenceMockedData.StGallenStadt.SecureConnectId, TestDefaults.UserId, roles);

    private GetProportionalElectionPartialEndResultRequest NewValidRequest()
    {
        return new GetProportionalElectionPartialEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund,
        };
    }

    private async Task EnterResults(Guid countingCircleId)
    {
        var permissionService = GetService<Core.Services.Permission.PermissionService>();
        permissionService.SetAbraxasAuthIfNotAuthenticated();

        var aggregateFactory = GetService<IAggregateFactory>();
        var aggregate = aggregateFactory.New<ProportionalElectionResultAggregate>();
        aggregate.StartSubmission(
            countingCircleId,
            ProportionalElectionMockedData.StGallenProportionalElectionInContestBund.Id,
            ContestMockedData.GuidBundesurnengang,
            false);
        aggregate.DefineEntry(
            new ProportionalElectionResultEntryParams
            {
                ReviewProcedure = ProportionalElectionReviewProcedure.Physically,
                BallotBundleSize = 10,
                BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
                BallotBundleSampleSize = 2,
            },
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
        aggregate.EnterUnmodifiedListResults(
            new List<ProportionalElectionUnmodifiedListResult>
            {
                new()
                {
                    ListId = Guid.Parse(ProportionalElectionMockedData.List1IdStGallenProportionalElectionInContestBund),
                    VoteCount = 5,
                },
                new()
                {
                    ListId = Guid.Parse(ProportionalElectionMockedData.List2IdStGallenProportionalElectionInContestBund),
                    VoteCount = 8,
                },
            },
            ContestMockedData.GuidBundesurnengang);
        var aggregateRepo = GetService<IAggregateRepository>();
        await aggregateRepo.Save(aggregate);

        await RunEvents<ProportionalElectionResultSubmissionStarted>(false);
        await RunEvents<ProportionalElectionResultEntryDefined>(false);
        await RunEvents<ProportionalElectionResultCountOfVotersEntered>(false);
        await RunEvents<ProportionalElectionUnmodifiedListResultsEntered>();
    }
}
