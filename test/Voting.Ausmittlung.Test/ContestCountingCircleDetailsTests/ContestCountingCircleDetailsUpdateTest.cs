// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ContestCountingCircleDetailsTests;

public class ContestCountingCircleDetailsUpdateTest : ContestCountingCircleDetailsBaseTest
{
    public ContestCountingCircleDetailsUpdateTest(TestApplicationFactory factory)
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
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(NewValidUpdatedEvent());

        var details = await RunOnDb(db => db
            .ContestCountingCircleDetails
            .AsSplitQuery()
            .Where(cd => cd.ContestId == Guid.Parse(ContestMockedData.IdGossau)
                         && cd.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidGossau)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.VotingCards)
            .ToListAsync());

        // ensure consistent json snapshot
        foreach (var detail in details)
        {
            foreach (var votingCardResultDetail in detail.VotingCards)
            {
                votingCardResultDetail.ContestCountingCircleDetails = null!;
                votingCardResultDetail.Id = Guid.Empty;
            }

            foreach (var subtotal in detail.CountOfVotersInformationSubTotals)
            {
                subtotal.ContestCountingCircleDetails = null!;
                subtotal.Id = Guid.Empty;
            }

            detail.OrderVotingCardsAndSubTotals();

            detail.Id = Guid.Empty;
        }

        details.MatchSnapshot(x => x.CountingCircleId);
    }

    [Fact]
    public async Task UpdateDetailsShouldBeOk()
    {
        await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsUpdated>();

        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task UpdateDetailsWithEVotingShouldBeOk()
    {
        var client = CreateService(SecureConnectTestDefaults.MockedTenantGossau.Id, roles: new[] { RolesMockedData.ErfassungElectionAdmin });
        await client.UpdateDetailsAsync(new UpdateContestCountingCircleDetailsRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdGossau,
            VotingCards =
            {
                new UpdateVotingCardResultDetailRequest
                {
                    Channel = SharedProto.VotingChannel.ByMail,
                    Valid = true,
                    CountOfReceivedVotingCards = 8000,
                    DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ct,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Channel = SharedProto.VotingChannel.BallotBox,
                    Valid = true,
                    CountOfReceivedVotingCards = 3000,
                    DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ct,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Channel = SharedProto.VotingChannel.ByMail,
                    Valid = false,
                    CountOfReceivedVotingCards = 4000,
                    DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ct,
                },
                new UpdateVotingCardResultDetailRequest
                {
                    Channel = SharedProto.VotingChannel.EVoting,
                    Valid = true,
                    CountOfReceivedVotingCards = 2000,
                    DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ct,
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
        });
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsUpdated>();
        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdGossau, async () =>
        {
            await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestCountingCircleDetailsUpdated>();
        });
    }

    [Fact]
    public async Task UpdateDetailsShouldBeOkAsContestManagerDuringTestingPhase()
    {
        await StGallenErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x => x.ContestId = ContestMockedData.IdStGallenEvoting));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsUpdated>();

        eventData.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x => x.ContestId = ContestMockedData.IdStGallenEvoting)),
            StatusCode.PermissionDenied);
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task UpdateDetailsShouldThrowIfVoteSubmissionDone(CountingCircleResultState disallowedState)
    {
        await RunOnDb(async db =>
        {
            var voteResult = await db.VoteResults.FirstAsync(vr => vr.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestGossauResult));
            voteResult.State = disallowedState;
            db.VoteResults.Update(voteResult);
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "A political business is already finished");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task UpdateDetailsShouldThrowIfMajorityElectionSubmissionDone(CountingCircleResultState disallowedState)
    {
        await MajorityElectionMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var electionResult = await db.MajorityElectionResults.FirstAsync(mr =>
                mr.Id == MajorityElectionResultMockedData.GuidGossauElectionResultInContestGossau);
            electionResult.State = disallowedState;
            db.MajorityElectionResults.Update(electionResult);
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "A political business is already finished");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task UpdateDetailsShouldThrowIfProportionalElectionSubmissionDone(CountingCircleResultState disallowedState)
    {
        await ProportionalElectionMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var electionResult = await db.ProportionalElectionResults.FirstAsync(mr =>
                mr.Id == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestGossau);
            electionResult.State = disallowedState;
            db.ProportionalElectionResults.Update(electionResult);
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "A political business is already finished");
    }

    [Theory]
    [InlineData(CountingCircleResultState.Initial)]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    public async Task UpdateDetailsWithVoteInProgressShouldWork(CountingCircleResultState allowedState)
    {
        await RunOnDb(async db =>
        {
            var voteResult = await db.VoteResults.FirstAsync(vr => vr.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestGossauResult));
            voteResult.State = allowedState;
            db.VoteResults.Update(voteResult);
            await db.SaveChangesAsync();
        });

        var request = NewValidRequest();
        await ErfassungElectionAdminClient.UpdateDetailsAsync(request);

        await RunEvents<ContestCountingCircleDetailsUpdated>();

        await RunOnDb(async db =>
        {
            var voteResult = await db.VoteResults
                .Include(vr => vr.Results)
                .ThenInclude(r => r.CountOfVoters)
                .FirstAsync(vr => vr.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestGossauResult));

            var totalCountOfVoters = request.CountOfVoters.Sum(c => c.CountOfVoters.GetValueOrDefault());
            foreach (var ballotResult in voteResult.Results)
            {
                ballotResult.CountOfVoters.VoterParticipation.Should()
                    .Be(ballotResult.CountOfVoters.TotalReceivedBallots / (decimal)totalCountOfVoters);
            }
        });
    }

    [Theory]
    [InlineData(CountingCircleResultState.Initial)]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    public async Task UpdateDetailsWithMajorityElectionInProgressShouldWork(CountingCircleResultState allowedState)
    {
        await MajorityElectionMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var electionResult = await db.MajorityElectionResults.FirstAsync(mr =>
                mr.Id == MajorityElectionResultMockedData.GuidGossauElectionResultInContestGossau);
            electionResult.State = allowedState;
            db.MajorityElectionResults.Update(electionResult);
            await db.SaveChangesAsync();
        });

        var request = NewValidRequest();
        await ErfassungElectionAdminClient.UpdateDetailsAsync(request);

        await RunEvents<ContestCountingCircleDetailsUpdated>();

        await RunOnDb(async db =>
        {
            var electionResult = await db.MajorityElectionResults
                .Include(mr => mr.CountOfVoters)
                .FirstAsync(vr => vr.Id == MajorityElectionResultMockedData.GuidGossauElectionResultInContestGossau);

            var totalCountOfVoters = request.CountOfVoters.Sum(c => c.CountOfVoters.GetValueOrDefault());
            electionResult.CountOfVoters.VoterParticipation.Should()
                .Be(electionResult.CountOfVoters.TotalReceivedBallots / (decimal)totalCountOfVoters);
        });
    }

    [Theory]
    [InlineData(CountingCircleResultState.Initial)]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    public async Task UpdateDetailsWithProportionalElectionInProgressShouldWork(CountingCircleResultState allowedState)
    {
        await ProportionalElectionMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var electionResult = await db.ProportionalElectionResults.FirstAsync(pr =>
                pr.Id == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestGossau);
            electionResult.State = allowedState;
            db.ProportionalElectionResults.Update(electionResult);
            await db.SaveChangesAsync();
        });

        var request = NewValidRequest();
        await ErfassungElectionAdminClient.UpdateDetailsAsync(request);

        await RunEvents<ContestCountingCircleDetailsUpdated>();

        await RunOnDb(async db =>
        {
            var electionResult = await db.ProportionalElectionResults
                .Include(pr => pr.CountOfVoters)
                .FirstAsync(vr => vr.Id == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestGossau);

            var totalCountOfVoters = request.CountOfVoters.Sum(c => c.CountOfVoters.GetValueOrDefault());
            electionResult.CountOfVoters.VoterParticipation.Should()
                .Be(electionResult.CountOfVoters.TotalReceivedBallots / (decimal)totalCountOfVoters);
        });
    }

    [Fact]
    public async Task UpdateDetailsShouldThrowIfVotingChannelNotEnabled()
    {
        await RunOnDb(async db =>
        {
            var doi = await db.DomainOfInfluences
                .AsTracking()
                .SingleAsync(x => x.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdGossau) && x.SnapshotContestId == GuidParser.Parse(ContestMockedData.IdGossau));
            var vcChannel = doi.CantonDefaults.EnabledVotingCardChannels.Single(x => !x.Valid && x.Channel == VotingChannel.ByMail);
            doi.CantonDefaults.EnabledVotingCardChannels.Remove(vcChannel);
            await db.SaveChangesAsync();
        });
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Voting card channel ByMail/False is not enabled");
    }

    [Fact]
    public async Task UpdateDetailsShouldThrowIfSwissAbroadVoterType()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
                x.CountOfVoters.Add(new UpdateCountOfVotersInformationSubTotalRequest
                {
                    Sex = SharedProto.SexType.Female,
                    VoterType = SharedProto.VoterType.SwissAbroad,
                    CountOfVoters = 1,
                }))),
            StatusCode.InvalidArgument,
            "swiss abroads not allowed");
    }

    [Fact]
    public async Task UpdateDetailsShouldThrowDuplicateVotingCard()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
                x.VotingCards.Add(new UpdateVotingCardResultDetailRequest
                {
                    Channel = SharedProto.VotingChannel.ByMail,
                    Valid = true,
                    CountOfReceivedVotingCards = 10000,
                    DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Sk,
                }))),
            StatusCode.InvalidArgument,
            "duplicated voting card details found");
    }

    [Fact]
    public async Task UpdateDetailsShouldThrowWithElectorateAndNonUniqueCount()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
                x.VotingCards.Add(new UpdateVotingCardResultDetailRequest
                {
                    Channel = SharedProto.VotingChannel.ByMail,
                    Valid = true,
                    CountOfReceivedVotingCards = 9999,
                    DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Sk,
                }))),
            StatusCode.InvalidArgument,
            "Voting card counts per electorate, channel and valid state must be unique");
    }

    [Fact]
    public async Task UpdateDetailsShouldThrowDuplicateCountOfVoters()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
                x.CountOfVoters.Add(new UpdateCountOfVotersInformationSubTotalRequest
                {
                    Sex = SharedProto.SexType.Female,
                    VoterType = SharedProto.VoterType.Swiss,
                    CountOfVoters = 1,
                }))),
            StatusCode.InvalidArgument,
            "duplicated count of voters subtotal found");
    }

    [Fact]
    public async Task UpdateDetailsShouldThrowIfContestIsLocked()
    {
        await SetContestState(ContestMockedData.IdGossau, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task UpdateDetailsShouldThrowInvalidDomainOfInfluenceType()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
                x.VotingCards.Add(new UpdateVotingCardResultDetailRequest
                {
                    Channel = SharedProto.VotingChannel.ByMail,
                    Valid = true,
                    CountOfReceivedVotingCards = 1,
                    DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ch,
                }))),
            StatusCode.InvalidArgument,
            "Voting cards with domain of influence type which don't exist are provided.");
    }

    [Fact]
    public async Task UpdateDetailsWithCountingMachineUnspecifiedWithEnabledOnCantonSettingsShouldThrow()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => doi.SnapshotContestId == Guid.Parse(ContestMockedData.IdGossau),
            doi => doi.CantonDefaults.CountingMachineEnabled = true);

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(
            NewValidRequest(x => x.CountingMachine = SharedProto.CountingMachine.Unspecified)),
            StatusCode.InvalidArgument,
            "Counting machine is required");
    }

    [Fact]
    public async Task UpdateDetailsWithCountingMachineWithDisabledOnCantonSettingsShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(
            NewValidRequest(x => x.CountingMachine = SharedProto.CountingMachine.None)),
            StatusCode.InvalidArgument,
            "Cannot set counting machine if it is not enabled on canton settings");
    }

    [Fact]
    public async Task UpdateDetailsWithCountingMachineWithEnabledOnCantonSettingsShouldBeOk()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => doi.SnapshotContestId == Guid.Parse(ContestMockedData.IdGossau),
            doi => doi.CantonDefaults.CountingMachineEnabled = true);

        await ErfassungElectionAdminClient.UpdateDetailsAsync(
            NewValidRequest(x => x.CountingMachine = SharedProto.CountingMachine.CalibratedScales));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsUpdated>();
        eventData.CountingMachine.Should().Be(SharedProto.CountingMachine.CalibratedScales);
    }

    [Fact]
    public async Task TestShouldUpdateAggregatedContestCountingCircleDetails()
    {
        var contestId = Guid.Parse(ContestMockedData.IdGossau);
        await SeedContestDomainOfInfluenceDetails();

        var contestDetailsBefore = await LoadContestDetails(contestId);
        contestDetailsBefore.MatchSnapshot("contestDetailsBefore");
        var doiDetailsBefore = await LoadContestDomainOfInfluenceDetails(contestId);
        doiDetailsBefore.MatchSnapshot("doiDetailsBefore");

        await TestEventPublisher.Publish(NewValidUpdatedEvent());
        var contestDetailsAfter = await LoadContestDetails(contestId);
        contestDetailsAfter.MatchSnapshot("contestDetailsAfter");
        var doiDetailsAfter = await LoadContestDomainOfInfluenceDetails(contestId);
        doiDetailsAfter.MatchSnapshot("doiDetailsAfter");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestCountingCircleDetailsService.ContestCountingCircleDetailsServiceClient(channel)
            .UpdateDetailsAsync(NewValidRequest());
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private UpdateContestCountingCircleDetailsRequest NewValidRequest(
        Action<UpdateContestCountingCircleDetailsRequest>? customizer = null)
    {
        var request = new UpdateContestCountingCircleDetailsRequest
        {
            ContestId = ContestMockedData.IdGossau,
            CountingCircleId = CountingCircleMockedData.IdGossau,
            VotingCards =
                {
                    new UpdateVotingCardResultDetailRequest
                    {
                        Channel = SharedProto.VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 10000,
                        DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Sk,
                    },
                    new UpdateVotingCardResultDetailRequest
                    {
                        Channel = SharedProto.VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 3000,
                        DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Sk,
                    },
                    new UpdateVotingCardResultDetailRequest
                    {
                        Channel = SharedProto.VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 4000,
                        DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Sk,
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
        };

        customizer?.Invoke(request);
        return request;
    }

    private ContestCountingCircleDetailsUpdated NewValidUpdatedEvent()
    {
        return new ContestCountingCircleDetailsUpdated
        {
            Id = ContestCountingCircleDetailsMockData.GuidGossauUrnengangGossauContestCountingCircleDetails.ToString(),
            ContestId = ContestMockedData.IdGossau,
            CountingCircleId = CountingCircleMockedData.IdGossau,
            VotingCards =
                {
                    new VotingCardResultDetailEventData
                    {
                        Channel = SharedProto.VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 10000,
                        DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Sk,
                    },
                    new VotingCardResultDetailEventData
                    {
                        Channel = SharedProto.VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 3000,
                        DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Sk,
                    },
                    new VotingCardResultDetailEventData
                    {
                        Channel = SharedProto.VotingChannel.BallotBox,
                        Valid = false,
                        CountOfReceivedVotingCards = 4000,
                        DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Sk,
                    },
                },
            CountOfVotersInformation = new CountOfVotersInformationEventData
            {
                SubTotalInfo =
                    {
                        new CountOfVotersInformationSubTotalEventData
                        {
                            Sex = SharedProto.SexType.Female,
                            VoterType = SharedProto.VoterType.Swiss,
                            CountOfVoters = 6000,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            Sex = SharedProto.SexType.Male,
                            VoterType = SharedProto.VoterType.Swiss,
                            CountOfVoters = 4000,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            Sex = SharedProto.SexType.Male,
                            VoterType = SharedProto.VoterType.SwissAbroad,
                            CountOfVoters = 1000,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            Sex = SharedProto.SexType.Female,
                            VoterType = SharedProto.VoterType.SwissAbroad,
                            CountOfVoters = 1000,
                        },
                    },
                TotalCountOfVoters = 12000,
            },
            CountingMachine = SharedProto.CountingMachine.None,
            EventInfo = GetMockedEventInfo(),
        };
    }

    private async Task SeedContestDomainOfInfluenceDetails()
    {
        var contestId = Guid.Parse(ContestMockedData.IdGossau);
        var gossauCcDetails = ContestCountingCircleDetailsMockData.GossauUrnengangGossau;

        await RunOnDb(async db =>
        {
            await db.ContestDomainOfInfluenceDetails.AddRangeAsync(
                new()
                {
                    ContestId = contestId,
                    DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(contestId, Guid.Parse(DomainOfInfluenceMockedData.IdBund)),
                    VotingCards = ContestDomainOfInfluenceDetailsMockedData.BuildVotingCards(gossauCcDetails),
                    CountOfVotersInformationSubTotals = ContestDomainOfInfluenceDetailsMockedData.BuildCountOfVotersInformationSubTotals(gossauCcDetails),
                    TotalCountOfVoters = 15000,
                },
                new()
                {
                    ContestId = contestId,
                    DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(contestId, Guid.Parse(DomainOfInfluenceMockedData.IdStGallen)),
                    VotingCards = ContestDomainOfInfluenceDetailsMockedData.BuildVotingCards(gossauCcDetails),
                    CountOfVotersInformationSubTotals = ContestDomainOfInfluenceDetailsMockedData.BuildCountOfVotersInformationSubTotals(gossauCcDetails),
                    TotalCountOfVoters = 15000,
                },
                new()
                {
                    ContestId = contestId,
                    DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(contestId, Guid.Parse(DomainOfInfluenceMockedData.IdGossau)),
                    VotingCards = ContestDomainOfInfluenceDetailsMockedData.BuildVotingCards(gossauCcDetails),
                    CountOfVotersInformationSubTotals = ContestDomainOfInfluenceDetailsMockedData.BuildCountOfVotersInformationSubTotals(gossauCcDetails),
                    TotalCountOfVoters = 15000,
                });
            await db.SaveChangesAsync();
        });
    }
}
