// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ContestCountingCircleDetailsTests;

public class ContestCountingCircleDetailsValidateUpdateTest
    : ContestCountingCircleDetailsBaseTest
{
    public ContestCountingCircleDetailsValidateUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task ShouldReturnIsValid()
    {
        var result = await ErfassungElectionAdminClient.ValidateUpdateDetailsAsync(NewValidRequest());
        result.MatchSnapshot();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnComparisonCountOfVoters()
    {
        var result = await ErfassungElectionAdminClient.ValidateUpdateDetailsAsync(NewValidBundRequest());

        var validationResult = result.ValidationResults
            .FirstOrDefault(x => x.Validation == SharedProto.Validation.ComparisonCountOfVoters);

        validationResult.Should().NotBeNull();
        validationResult!.ComparisonCountOfVotersData.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldReturnComparisonVotingChannels()
    {
        var result = await ErfassungElectionAdminClient.ValidateUpdateDetailsAsync(NewValidBundRequest());

        var validationResults = result.ValidationResults
            .Where(x => x.Validation == SharedProto.Validation.ComparisonVotingChannels)
            .ToList();

        validationResults.Any().Should().BeTrue();
        validationResults.Select(x => x.ComparisonVotingChannelsData).MatchSnapshot();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenCountOfVotersNotNull()
    {
        var result = await ErfassungElectionAdminClient.ValidateUpdateDetailsAsync(NewValidRequest(x =>
            x.Request.CountOfVoters[0].CountOfVoters = null));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.ContestCountingCircleDetailsCountOfVotersNotNull)
            .IsValid.Should().BeFalse();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenVotingCardsReceivedNotNull()
    {
        var result = await ErfassungElectionAdminClient.ValidateUpdateDetailsAsync(NewValidRequest(x =>
            x.Request.VotingCards[0].CountOfReceivedVotingCards = null));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.ContestCountingCircleDetailsVotingCardsReceivedNotNull)
            .IsValid.Should().BeFalse();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsNotValidWhenNotVotingCardsLessOrEqualCountOfVoters()
    {
        var result = await ErfassungElectionAdminClient.ValidateUpdateDetailsAsync(NewValidRequest(x =>
            x.Request.VotingCards[0].CountOfReceivedVotingCards++));
        result.ValidationResults.Single(r => r.Validation == SharedProto.Validation.ContestCountingCircleDetailsVotingCardsLessOrEqualCountOfVoters)
            .IsValid.Should().BeFalse();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReturnIsValidAsContestManagerDuringTestingPhase()
    {
        var result = await StGallenErfassungElectionAdminClient.ValidateUpdateDetailsAsync(NewValidRequest(x => x.Request.ContestId = ContestMockedData.IdStGallenEvoting));
        result.MatchSnapshot();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdVergangenerBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundErfassungElectionAdminClient.ValidateUpdateDetailsAsync(NewValidRequest(x => x.Request.ContestId = ContestMockedData.IdVergangenerBundesurnengang)),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestCountingCircleDetailsService.ContestCountingCircleDetailsServiceClient(channel)
            .ValidateUpdateDetailsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private ValidateUpdateContestCountingCircleDetailsRequest NewValidRequest(
        Action<ValidateUpdateContestCountingCircleDetailsRequest>? customizer = null)
    {
        var request = new ValidateUpdateContestCountingCircleDetailsRequest
        {
            Request = new UpdateContestCountingCircleDetailsRequest
            {
                ContestId = ContestMockedData.IdGossau,
                CountingCircleId = CountingCircleMockedData.IdGossau,
                VotingCards =
                    {
                        new UpdateVotingCardResultDetailRequest
                        {
                            Channel = SharedProto.VotingChannel.ByMail,
                            Valid = true,
                            CountOfReceivedVotingCards = 5000,
                            DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ch,
                        },
                        new UpdateVotingCardResultDetailRequest
                        {
                            Channel = SharedProto.VotingChannel.BallotBox,
                            Valid = true,
                            CountOfReceivedVotingCards = 3000,
                            DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ch,
                        },
                        new UpdateVotingCardResultDetailRequest
                        {
                            Channel = SharedProto.VotingChannel.BallotBox,
                            Valid = false,
                            CountOfReceivedVotingCards = 2000,
                            DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ch,
                        },
                    },
                CountOfVoters =
                    {
                        new UpdateCountOfVotersInformationSubTotalRequest
                        {
                            Sex = SharedProto.SexType.Female,
                            VoterType = SharedProto.VoterType.Swiss,
                            CountOfVoters = 6000,
                        },
                        new UpdateCountOfVotersInformationSubTotalRequest
                        {
                            Sex = SharedProto.SexType.Male,
                            VoterType = SharedProto.VoterType.Swiss,
                            CountOfVoters = 4000,
                        },
                    },
            },
        };

        customizer?.Invoke(request);
        return request;
    }

    private ValidateUpdateContestCountingCircleDetailsRequest NewValidBundRequest(
        Action<ValidateUpdateContestCountingCircleDetailsRequest>? customizer = null)
    {
        return NewValidRequest(x =>
        {
            x.Request.ContestId = ContestMockedData.IdBundesurnengang;
            x.Request.CountingCircleId = CountingCircleMockedData.IdGossau;
            customizer?.Invoke(x);
        });
    }
}
