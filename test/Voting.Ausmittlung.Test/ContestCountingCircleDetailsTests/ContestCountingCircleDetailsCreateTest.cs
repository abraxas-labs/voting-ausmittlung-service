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

public class ContestCountingCircleDetailsCreateTest : ContestCountingCircleDetailsBaseTest
{
    public ContestCountingCircleDetailsCreateTest(TestApplicationFactory factory)
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
        EventPublisherMock.Clear();
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(NewValidEvent());

        var details = await RunOnDb(db => db
            .ContestCountingCircleDetails
            .AsSplitQuery()
            .Where(cd => cd.ContestId == Guid.Parse(ContestMockedData.IdBundesurnengang)
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
        }

        details.MatchSnapshot(x => x.CountingCircleId);
    }

    [Fact]
    public async Task CreateDetailsShouldBeOk()
    {
        await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsCreated>();
        eventData.MatchSnapshot("create");

        await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest());
        var eventDataUpdate = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsUpdated>();
        eventDataUpdate.MatchSnapshot("update");
    }

    [Fact]
    public async Task CreateDetailsWithAllVoterTypesShouldBeOk()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            x =>
            {
                x.HasForeignerVoters = true;
                x.HasMinorVoters = true;
                x.SwissAbroadVotingRight = SwissAbroadVotingRight.OnEveryCountingCircle;
            });

        await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
        {
            x.CountOfVoters.Add(new UpdateCountOfVotersInformationSubTotalRequest
            {
                VoterType = SharedProto.VoterType.SwissAbroad,
                Sex = SharedProto.SexType.Male,
                CountOfVoters = 40,
            });
            x.CountOfVoters.Add(new UpdateCountOfVotersInformationSubTotalRequest
            {
                VoterType = SharedProto.VoterType.SwissAbroad,
                Sex = SharedProto.SexType.Female,
                CountOfVoters = 60,
            });
            x.CountOfVoters.Add(new UpdateCountOfVotersInformationSubTotalRequest
            {
                VoterType = SharedProto.VoterType.Foreigner,
                Sex = SharedProto.SexType.Male,
                CountOfVoters = 20,
            });
            x.CountOfVoters.Add(new UpdateCountOfVotersInformationSubTotalRequest
            {
                VoterType = SharedProto.VoterType.Foreigner,
                Sex = SharedProto.SexType.Female,
                CountOfVoters = 80,
            });
            x.CountOfVoters.Add(new UpdateCountOfVotersInformationSubTotalRequest
            {
                VoterType = SharedProto.VoterType.Minor,
                Sex = SharedProto.SexType.Male,
                CountOfVoters = 80,
            });
            x.CountOfVoters.Add(new UpdateCountOfVotersInformationSubTotalRequest
            {
                VoterType = SharedProto.VoterType.Minor,
                Sex = SharedProto.SexType.Female,
                CountOfVoters = 20,
            });
        }));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsCreated>();
        eventData.MatchSnapshot("create");
    }

    [Fact]
    public async Task TestShouldWorkAfterTestingPhaseEnded()
    {
        var request = NewValidRequest();
        var ccId = Guid.Parse(request.CountingCircleId);
        var contestId = Guid.Parse(request.ContestId);

        // testing phase
        await ErfassungElectionAdminClient.UpdateDetailsAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsCreated>();

        var detailsInTestingPhaseId = AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, ccId, false);
        evInTestingPhase.Id.Should().Be(detailsInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        await ErfassungElectionAdminClient.UpdateDetailsAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsCreated>();
        await RunEvents<ContestTestingPhaseEnded>();

        var detailsTestingPhaseEndedId = AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, ccId, true);
        evTestingPhaseEnded.Id.Should().Be(detailsTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            var response = await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ContestCountingCircleDetailsCreated>();
        });
    }

    [Fact]
    public async Task CreateDetailsShouldBeOkAsContestManagerDuringTestingPhase()
    {
        await BundErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsCreated>();
        eventData.MatchSnapshot("create");

        await BundErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest());
        var eventDataUpdate = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsUpdated>();
        eventDataUpdate.MatchSnapshot("update");
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task CreateDetailsWithCountingMachineUnspecifiedWithEnabledOnCantonSettingsShouldThrow()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidBundesurnengang,
            x => x.CountingMachineEnabled = true,
            true);

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(
            NewValidRequest(x => x.CountingMachine = SharedProto.CountingMachine.Unspecified)),
            StatusCode.InvalidArgument,
            "Counting machine is required");
    }

    [Fact]
    public async Task CreateDetailsWithCountingMachineWithDisabledOnCantonSettingsShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(
            NewValidRequest(x => x.CountingMachine = SharedProto.CountingMachine.BanknoteCountingMachine)),
            StatusCode.InvalidArgument,
            "Cannot set counting machine if it is not enabled on canton settings");
    }

    [Fact]
    public async Task CreateDetailsWithCountingMachineWithEnabledOnCantonSettingsShouldBeOk()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidBundesurnengang,
            x => x.CountingMachineEnabled = true,
            true);

        await ErfassungElectionAdminClient.UpdateDetailsAsync(
            NewValidRequest(x => x.CountingMachine = SharedProto.CountingMachine.BanknoteCountingMachine));
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ContestCountingCircleDetailsCreated>();
        eventData.CountingMachine.Should().Be(SharedProto.CountingMachine.BanknoteCountingMachine);
    }

    [Fact]
    public async Task CreateDetailsNegativeVotingCardCountValueShouldThrow()
    {
        // tests whether the integration of the proto validators work.
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(
                NewValidRequest(x => x.VotingCards[0].CountOfReceivedVotingCards = -1)),
            StatusCode.InvalidArgument,
            "CountOfReceivedVotingCards' is smaller than the MinValue 0");
    }

    [Fact]
    public async Task CreateDetailsShouldThrowIfDisallowedSwissAbroadVoterType()
    {
        await AssertStatus(
        async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
        {
            x.ContestId = ContestMockedData.IdGossau;
            x.CountOfVoters.Add(new UpdateCountOfVotersInformationSubTotalRequest
            {
                Sex = SharedProto.SexType.Female,
                VoterType = SharedProto.VoterType.SwissAbroad,
                CountOfVoters = 1,
            });
        })),
        StatusCode.InvalidArgument,
        "swiss abroads not allowed");
    }

    [Fact]
    public async Task CreateDetailsShouldThrowIfDisallowedForeignerVoterType()
    {
        await AssertStatus(
        async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
        {
            x.ContestId = ContestMockedData.IdGossau;
            x.CountOfVoters.Add(new UpdateCountOfVotersInformationSubTotalRequest
            {
                Sex = SharedProto.SexType.Female,
                VoterType = SharedProto.VoterType.Foreigner,
                CountOfVoters = 1,
            });
        })),
        StatusCode.InvalidArgument,
        "foreigners not allowed");
    }

    [Fact]
    public async Task CreateDetailsShouldThrowIfDisallowedMinorVoterType()
    {
        await AssertStatus(
        async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
        {
            x.ContestId = ContestMockedData.IdGossau;
            x.CountOfVoters.Add(new UpdateCountOfVotersInformationSubTotalRequest
            {
                Sex = SharedProto.SexType.Female,
                VoterType = SharedProto.VoterType.Minor,
                CountOfVoters = 1,
            });
        })),
        StatusCode.InvalidArgument,
        "minors not allowed");
    }

    [Fact]
    public async Task CreateDetailsShouldThrowDuplicateVotingCard()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
                x.VotingCards.Add(new UpdateVotingCardResultDetailRequest
                {
                    Channel = SharedProto.VotingChannel.ByMail,
                    Valid = true,
                    CountOfReceivedVotingCards = 10000,
                    DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ct,
                }))),
            StatusCode.InvalidArgument,
            "duplicated voting card details found");
    }

    [Fact]
    public async Task CreateDetailsShouldThrowDuplicateCountOfVoters()
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
    public async Task CreateDetailsShouldThrowIfContestIsLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task CreateDetailsShouldThrowInvalidDomainOfInfluenceType()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
                x.VotingCards.Add(new UpdateVotingCardResultDetailRequest
                {
                    Channel = SharedProto.VotingChannel.ByMail,
                    Valid = true,
                    CountOfReceivedVotingCards = 1,
                    DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Sk,
                }))),
            StatusCode.InvalidArgument,
            "Voting cards with domain of influence type which don't exist are provided.");
    }

    [Fact]
    public async Task CreateDetailsShouldThrowIfVotingChannelNotEnabled()
    {
        await RunOnDb(async db =>
        {
            var cantonDefaults = await db.ContestCantonDefaults
                .AsSplitQuery()
                .AsTracking()
                .SingleAsync(x => x.ContestId == GuidParser.Parse(ContestMockedData.IdBundesurnengang));
            var vcChannel = cantonDefaults.EnabledVotingCardChannels.Single(x => !x.Valid && x.Channel == VotingChannel.ByMail);
            cantonDefaults.EnabledVotingCardChannels.Remove(vcChannel);
            await db.SaveChangesAsync();
        });
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Voting card channel ByMail/False is not enabled");
    }

    [Fact]
    public async Task CreateDetailsShouldThrowWithElectorateAndNonUniqueCount()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.UpdateDetailsAsync(NewValidRequest(x =>
                x.VotingCards.Add(new UpdateVotingCardResultDetailRequest
                {
                    Channel = SharedProto.VotingChannel.ByMail,
                    Valid = true,
                    CountOfReceivedVotingCards = 9999,
                    DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ct,
                }))),
            StatusCode.InvalidArgument,
            "Voting card counts per electorate, channel and valid state must be unique");
    }

    [Fact]
    public async Task TestShouldUpdateAggregatedContestCountingCircleDetails()
    {
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        await RunOnDb(async db =>
        {
            db.ContestDetails.RemoveRange(await db.ContestDetails.Where(x => x.ContestId == contestId).ToListAsync());
            db.ContestDomainOfInfluenceDetails.RemoveRange(await db.ContestDomainOfInfluenceDetails.Where(x => x.ContestId == contestId).ToListAsync());

            var contestCcDetails = await db.ContestCountingCircleDetails
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.VotingCards)
                .Include(x => x.CountOfVotersInformationSubTotals)
                .Where(x => x.ContestId == contestId)
                .ToListAsync();

            foreach (var detail in contestCcDetails)
            {
                detail.VotingCards.Clear();
                detail.CountOfVotersInformationSubTotals.Clear();
                detail.TotalCountOfVoters = 0;
            }

            await db.SaveChangesAsync();
        });

        var contestDetailsBefore = await LoadContestDetails(contestId);
        contestDetailsBefore.MatchSnapshot("contestDetailsBefore");
        var doiDetailsBefore = await LoadContestDomainOfInfluenceDetails(contestId);
        doiDetailsBefore.MatchSnapshot("doiDetailsBefore");

        await TestEventPublisher.Publish(NewValidEvent());
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

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    private UpdateContestCountingCircleDetailsRequest NewValidRequest(
        Action<UpdateContestCountingCircleDetailsRequest>? customizer = null)
    {
        var request = new UpdateContestCountingCircleDetailsRequest
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            CountingCircleId = CountingCircleMockedData.IdStGallenStFiden,
            VotingCards =
                {
                    new UpdateVotingCardResultDetailRequest
                    {
                        Channel = SharedProto.VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 10000,
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

    private ContestCountingCircleDetailsCreated NewValidEvent()
    {
        return new ContestCountingCircleDetailsCreated
        {
            Id = AusmittlungUuidV5.BuildContestCountingCircleDetails(
                Guid.Parse(ContestMockedData.IdBundesurnengang),
                CountingCircleMockedData.GuidGossau,
                false).ToString(),
            ContestId = ContestMockedData.IdBundesurnengang,
            CountingCircleId = CountingCircleMockedData.IdGossau,
            VotingCards =
                {
                    new VotingCardResultDetailEventData
                    {
                        Channel = SharedProto.VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 10000,
                        DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetailEventData
                    {
                        Channel = SharedProto.VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 3000,
                        DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetailEventData
                    {
                        Channel = SharedProto.VotingChannel.BallotBox,
                        Valid = false,
                        CountOfReceivedVotingCards = 4000,
                        DomainOfInfluenceType = SharedProto.DomainOfInfluenceType.Ct,
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
                        new CountOfVotersInformationSubTotalEventData
                        {
                            Sex = SharedProto.SexType.Male,
                            VoterType = SharedProto.VoterType.Foreigner,
                            CountOfVoters = 30,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            Sex = SharedProto.SexType.Female,
                            VoterType = SharedProto.VoterType.Foreigner,
                            CountOfVoters = 70,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            Sex = SharedProto.SexType.Male,
                            VoterType = SharedProto.VoterType.Minor,
                            CountOfVoters = 70,
                        },
                        new CountOfVotersInformationSubTotalEventData
                        {
                            Sex = SharedProto.SexType.Female,
                            VoterType = SharedProto.VoterType.Minor,
                            CountOfVoters = 30,
                        },
                    },
                TotalCountOfVoters = 12200,
            },
            CountingMachine = SharedProto.CountingMachine.CalibratedScales,
            EventInfo = GetMockedEventInfo(),
        };
    }
}
