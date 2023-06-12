// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ContestCountingCircleDetailsMockData
{
    public static readonly Guid GuidVergangenerBundesurnengangGossauContestCountingCircleDetails =
            AusmittlungUuidV5.BuildContestCountingCircleDetails(
                Guid.Parse(ContestMockedData.IdVergangenerBundesurnengang),
                CountingCircleMockedData.GuidGossau,
                true);

    public static readonly Guid GuidGossauUrnengangGossauContestCountingCircleDetails =
        AusmittlungUuidV5.BuildContestCountingCircleDetails(
            Guid.Parse(ContestMockedData.IdGossau),
            CountingCircleMockedData.GuidGossau,
            false);

    public static readonly Guid GuidGossauUrnengangStGallenContestCountingCircleDetails =
        AusmittlungUuidV5.BuildContestCountingCircleDetails(
            Guid.Parse(ContestMockedData.IdStGallenEvoting),
            CountingCircleMockedData.GuidGossau,
            false);

    public static readonly Guid GuidGossauUrnengangBundContestCountingCircleDetails =
        AusmittlungUuidV5.BuildContestCountingCircleDetails(
            Guid.Parse(ContestMockedData.IdBundesurnengang),
            CountingCircleMockedData.GuidGossau,
            false);

    public static readonly Guid GuidStGallenUrnengangBundContestCountingCircleDetails =
            AusmittlungUuidV5.BuildContestCountingCircleDetails(
                Guid.Parse(ContestMockedData.IdBundesurnengang),
                CountingCircleMockedData.GuidStGallen,
                false);

    public static readonly Guid GuidUzwilUrnengangBundContestCountingCircleDetails =
        AusmittlungUuidV5.BuildContestCountingCircleDetails(
            Guid.Parse(ContestMockedData.IdBundesurnengang),
            CountingCircleMockedData.GuidUzwil,
            false);

    public static readonly Guid GuidUzwilUrnengangUzwilContestCountingCircleDetails =
        AusmittlungUuidV5.BuildContestCountingCircleDetails(
            Guid.Parse(ContestMockedData.IdUzwilEvoting),
            CountingCircleMockedData.GuidUzwil,
            false);

    public static ContestCountingCircleDetails VergangenerBundesurnengangGossau
        => new ContestCountingCircleDetails
        {
            Id = GuidVergangenerBundesurnengangGossauContestCountingCircleDetails,
            ContestId = Guid.Parse(ContestMockedData.IdVergangenerBundesurnengang),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 12000,
            VotingCards =
            {
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 10000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 3000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 4000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 10000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 3000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 4000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
            },
            CountOfVotersInformationSubTotals =
            {
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 6000,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 4000,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 1000,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 1000,
                    },
            },
        };

    public static ContestCountingCircleDetails GossauUrnengangGossau
        => new ContestCountingCircleDetails
        {
            Id = GuidGossauUrnengangGossauContestCountingCircleDetails,
            ContestId = Guid.Parse(ContestMockedData.IdGossau),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 15000,
            VotingCards =
            {
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 1000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 3000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 1000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 3000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
            },
            CountOfVotersInformationSubTotals =
            {
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 7000,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 8000,
                    },
            },
        };

    public static ContestCountingCircleDetails GossauUrnengangStGallen
        => new ContestCountingCircleDetails
        {
            Id = GuidGossauUrnengangStGallenContestCountingCircleDetails,
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 15800,
            EVoting = true,
            VotingCards =
            {
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 1000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 3000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 1000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 3000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 3500,
                        DomainOfInfluenceType = DomainOfInfluenceType.Sk,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 2500,
                        DomainOfInfluenceType = DomainOfInfluenceType.Sk,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 500,
                        DomainOfInfluenceType = DomainOfInfluenceType.Sk,
                    },
            },
            CountOfVotersInformationSubTotals =
            {
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 7000,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 8000,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 500,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 300,
                    },
            },
        };

    public static ContestCountingCircleDetails GossauUrnengangBund
        => new ContestCountingCircleDetails
        {
            Id = GuidGossauUrnengangBundContestCountingCircleDetails,
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 15800,
            VotingCards =
            {
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 1000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 250,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.Paper,
                        Valid = true,
                        CountOfReceivedVotingCards = 100,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.EVoting,
                        Valid = true,
                        CountOfReceivedVotingCards = 120,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 1000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 250,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.Paper,
                        Valid = true,
                        CountOfReceivedVotingCards = 100,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.EVoting,
                        Valid = true,
                        CountOfReceivedVotingCards = 120,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
            },
            CountOfVotersInformationSubTotals =
            {
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 7000,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 8000,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 500,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 300,
                    },
            },
        };

    public static ContestCountingCircleDetails StGallenUrnengangBund
        => new ContestCountingCircleDetails
        {
            Id = GuidStGallenUrnengangBundContestCountingCircleDetails,
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            CountingCircleId = CountingCircleMockedData.GuidStGallen,
            TotalCountOfVoters = 7000 + 8000 + 300 + 500,
            VotingCards =
            {
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 1000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 3000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 1000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 3000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
            },
            CountOfVotersInformationSubTotals =
            {
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 7000,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 8000,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 300,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 500,
                    },
            },
        };

    public static ContestCountingCircleDetails UzwilUrnengangBund
        => new ContestCountingCircleDetails
        {
            Id = GuidUzwilUrnengangBundContestCountingCircleDetails,
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            CountingCircleId = CountingCircleMockedData.GuidUzwil,
            TotalCountOfVoters = 1400 + 1600 + 100 + 110,
            VotingCards =
            {
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 400,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.Paper,
                        Valid = true,
                        CountOfReceivedVotingCards = 200,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.EVoting,
                        Valid = true,
                        CountOfReceivedVotingCards = 150,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 150,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 400,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.Paper,
                        Valid = true,
                        CountOfReceivedVotingCards = 200,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.EVoting,
                        Valid = true,
                        CountOfReceivedVotingCards = 150,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 150,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Sk,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 400,
                        DomainOfInfluenceType = DomainOfInfluenceType.Sk,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.Paper,
                        Valid = true,
                        CountOfReceivedVotingCards = 200,
                        DomainOfInfluenceType = DomainOfInfluenceType.Sk,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.EVoting,
                        Valid = true,
                        CountOfReceivedVotingCards = 150,
                        DomainOfInfluenceType = DomainOfInfluenceType.Sk,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 150,
                        DomainOfInfluenceType = DomainOfInfluenceType.Sk,
                    },
            },
            CountOfVotersInformationSubTotals =
            {
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 1400,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 1600,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 100,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 110,
                    },
            },
        };

    public static ContestCountingCircleDetails UzwilUrnengangUzwil
        => new ContestCountingCircleDetails
        {
            Id = GuidUzwilUrnengangUzwilContestCountingCircleDetails,
            ContestId = Guid.Parse(ContestMockedData.IdUzwilEvoting),
            CountingCircleId = CountingCircleMockedData.GuidUzwil,
            EVoting = true,
            TotalCountOfVoters = 1400 + 1600 + 100 + 110,
            VotingCards =
            {
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = true,
                        CountOfReceivedVotingCards = 2000,
                        DomainOfInfluenceType = DomainOfInfluenceType.Mu,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.BallotBox,
                        Valid = true,
                        CountOfReceivedVotingCards = 400,
                        DomainOfInfluenceType = DomainOfInfluenceType.Mu,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.Paper,
                        Valid = true,
                        CountOfReceivedVotingCards = 200,
                        DomainOfInfluenceType = DomainOfInfluenceType.Mu,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.EVoting,
                        Valid = true,
                        CountOfReceivedVotingCards = 150,
                        DomainOfInfluenceType = DomainOfInfluenceType.Mu,
                    },
                    new VotingCardResultDetail
                    {
                        Channel = VotingChannel.ByMail,
                        Valid = false,
                        CountOfReceivedVotingCards = 150,
                        DomainOfInfluenceType = DomainOfInfluenceType.Mu,
                    },
            },
            CountOfVotersInformationSubTotals =
            {
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 1400,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.Swiss,
                        CountOfVoters = 1600,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Male,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 100,
                    },
                    new CountOfVotersInformationSubTotal
                    {
                        Sex = SexType.Female,
                        VoterType = VoterType.SwissAbroad,
                        CountOfVoters = 110,
                    },
            },
        };

    public static IEnumerable<ContestCountingCircleDetails> All
    {
        get
        {
            yield return VergangenerBundesurnengangGossau;
            yield return GossauUrnengangGossau;
            yield return GossauUrnengangStGallen;
            yield return GossauUrnengangBund;
            yield return StGallenUrnengangBund;
            yield return UzwilUrnengangBund;
            yield return UzwilUrnengangUzwil;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var contestCountingCircleDetails = All.ToList();

            var db = sp.GetRequiredService<DataContext>();

            foreach (var contestCountingCircle in contestCountingCircleDetails)
            {
                contestCountingCircle.CountingCircle = await db.CountingCircles.AsTracking().FirstAsync(cc =>
                    cc.BasisCountingCircleId == contestCountingCircle.CountingCircleId && cc.SnapshotContestId == contestCountingCircle.ContestId);
            }

            db.ContestCountingCircleDetails.AddRange(contestCountingCircleDetails);
            await db.SaveChangesAsync();

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", "fake", "fake", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var mapper = sp.GetRequiredService<TestMapper>();

            foreach (var details in All)
            {
                await aggregateRepository.Save(ToAggregate(details, aggregateFactory, mapper));
            }

            var ccDetailsBuilder = sp.GetRequiredService<ContestCountingCircleDetailsBuilder>();
            await ccDetailsBuilder.SyncForDomainOfInfluences(await db.DomainOfInfluences.Select(x => x.Id).ToListAsync());
        });
    }

    private static ContestCountingCircleDetailsAggregate ToAggregate(
        ContestCountingCircleDetails details,
        IAggregateFactory aggregateFactory,
        TestMapper mapper)
    {
        var aggregate = aggregateFactory.New<ContestCountingCircleDetailsAggregate>();
        var c = mapper.Map<DomainModels.ContestCountingCircleDetails>(details);

        aggregate.CreateFrom(c, c.ContestId, c.CountingCircleId, ContestMockedData.TestingPhaseEnded(details.ContestId));
        return aggregate;
    }
}
