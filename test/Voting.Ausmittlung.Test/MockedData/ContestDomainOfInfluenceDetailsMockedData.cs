// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ContestDomainOfInfluenceDetailsMockedData
{
    public const string IdBundUrnengangBundContestDomainOfInfluenceDetails = "fc270a18-a4ab-458a-aede-aaa15caccb54";
    public const string IdBundUrnengangStGallenContestDomainOfInfluenceDetails = "909e039a-7abc-4384-a383-234be35f764d";
    public const string IdBundUrnengangGossauContestDomainOfInfluenceDetails = "56ba1642-abbb-4e9b-9845-9fe2e29f9af3";
    public const string IdBundUrnengangUzwilContestDomainOfInfluenceDetails = "574c98c8-8610-4929-9a22-3e89672625ca";
    public const string IdStGallenUrnengangStGallenContestDomainOfInfluenceDetails = "7ac5d3af-38f2-43ec-8395-5d95d0c285b1";
    public const string IdGossauUrnengangStGallenContestDomainOfInfluenceDetails = "1a91a016-903f-485e-8847-8990c7cbc718";

    public static ContestDomainOfInfluenceDetails BundUrnengangBundContestDomainOfInfluenceDetails
        => new()
        {
            Id = Guid.Parse(IdBundUrnengangBundContestDomainOfInfluenceDetails),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            TotalCountOfVoters = 15800 + 15800 + 3210,
            VotingCards = BuildVotingCards(ContestCountingCircleDetailsMockData.StGallenUrnengangBund, ContestCountingCircleDetailsMockData.UzwilUrnengangBund, ContestCountingCircleDetailsMockData.GossauUrnengangBund),
            CountOfVotersInformationSubTotals = BuildCountOfVotersInformationSubTotals(ContestCountingCircleDetailsMockData.StGallenUrnengangBund, ContestCountingCircleDetailsMockData.UzwilUrnengangBund, ContestCountingCircleDetailsMockData.GossauUrnengangBund),
        };

    public static ContestDomainOfInfluenceDetails BundUrnengangStGallenContestDomainOfInfluenceDetails
        => new()
        {
            Id = Guid.Parse(IdBundUrnengangStGallenContestDomainOfInfluenceDetails),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            TotalCountOfVoters = 15800 + 15800 + 3210,
            VotingCards = BuildVotingCards(ContestCountingCircleDetailsMockData.StGallenUrnengangBund, ContestCountingCircleDetailsMockData.UzwilUrnengangBund, ContestCountingCircleDetailsMockData.GossauUrnengangBund),
            CountOfVotersInformationSubTotals = BuildCountOfVotersInformationSubTotals(ContestCountingCircleDetailsMockData.StGallenUrnengangBund, ContestCountingCircleDetailsMockData.UzwilUrnengangBund, ContestCountingCircleDetailsMockData.GossauUrnengangBund),
        };

    public static ContestDomainOfInfluenceDetails BundUrnengangGossauContestDomainOfInfluenceDetails
        => new()
        {
            Id = Guid.Parse(IdBundUrnengangGossauContestDomainOfInfluenceDetails),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGossau),
            TotalCountOfVoters = 15800,
            VotingCards = BuildVotingCards(ContestCountingCircleDetailsMockData.GossauUrnengangBund),
            CountOfVotersInformationSubTotals = BuildCountOfVotersInformationSubTotals(ContestCountingCircleDetailsMockData.GossauUrnengangBund),
        };

    public static ContestDomainOfInfluenceDetails BundUrnengangUzwilContestDomainOfInfluenceDetails
        => new()
        {
            Id = Guid.Parse(IdBundUrnengangUzwilContestDomainOfInfluenceDetails),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            TotalCountOfVoters = 3210,
            VotingCards = BuildVotingCards(ContestCountingCircleDetailsMockData.UzwilUrnengangBund),
            CountOfVotersInformationSubTotals = BuildCountOfVotersInformationSubTotals(ContestCountingCircleDetailsMockData.UzwilUrnengangBund),
        };

    public static ContestDomainOfInfluenceDetails StGallenUrnengangStGallenContestDomainOfInfluenceDetails
        => new()
        {
            Id = Guid.Parse(IdStGallenUrnengangStGallenContestDomainOfInfluenceDetails),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            TotalCountOfVoters = 15800,
            VotingCards = BuildVotingCards(ContestCountingCircleDetailsMockData.GossauUrnengangStGallen),
            CountOfVotersInformationSubTotals = BuildCountOfVotersInformationSubTotals(ContestCountingCircleDetailsMockData.GossauUrnengangStGallen),
        };

    public static ContestDomainOfInfluenceDetails GossauUrnengangStGallenContestDomainOfInfluenceDetails
        => new()
        {
            Id = Guid.Parse(IdGossauUrnengangStGallenContestDomainOfInfluenceDetails),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGossau),
            TotalCountOfVoters = 15800,
            VotingCards = BuildVotingCards(ContestCountingCircleDetailsMockData.GossauUrnengangStGallen),
            CountOfVotersInformationSubTotals = BuildCountOfVotersInformationSubTotals(ContestCountingCircleDetailsMockData.GossauUrnengangStGallen),
        };

    public static IEnumerable<ContestDomainOfInfluenceDetails> All
    {
        get
        {
            yield return BundUrnengangBundContestDomainOfInfluenceDetails;
            yield return BundUrnengangStGallenContestDomainOfInfluenceDetails;
            yield return BundUrnengangGossauContestDomainOfInfluenceDetails;
            yield return BundUrnengangUzwilContestDomainOfInfluenceDetails;
            yield return StGallenUrnengangStGallenContestDomainOfInfluenceDetails;
            yield return GossauUrnengangStGallenContestDomainOfInfluenceDetails;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var doiDetails = All.ToList();

            foreach (var doiDetail in doiDetails)
            {
                doiDetail.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(doiDetail.ContestId, doiDetail.DomainOfInfluenceId);
            }

            db.ContestDomainOfInfluenceDetails.AddRange(doiDetails);
            await db.SaveChangesAsync();
        });
    }

    internal static HashSet<DomainOfInfluenceVotingCardResultDetail> BuildVotingCards(params ContestCountingCircleDetails[] details)
    {
        var votingCardByChannelValidAndDoiType = new Dictionary<(VotingChannel, bool, DomainOfInfluenceType), DomainOfInfluenceVotingCardResultDetail>();

        foreach (var detail in details)
        {
            foreach (var votingCard in detail.VotingCards)
            {
                var votingCardKey = (votingCard.Channel, votingCard.Valid, votingCard.DomainOfInfluenceType);
                if (!votingCardByChannelValidAndDoiType.TryGetValue(votingCardKey, out var doiVotingCard))
                {
                    doiVotingCard = new()
                    {
                        Channel = votingCard.Channel,
                        Valid = votingCard.Valid,
                        DomainOfInfluenceType = votingCard.DomainOfInfluenceType,
                    };
                    votingCardByChannelValidAndDoiType.Add(votingCardKey, doiVotingCard);
                }

                doiVotingCard.CountOfReceivedVotingCards += votingCard.CountOfReceivedVotingCards.GetValueOrDefault();
            }
        }

        return votingCardByChannelValidAndDoiType.Values.ToHashSet();
    }

    internal static HashSet<DomainOfInfluenceCountOfVotersInformationSubTotal> BuildCountOfVotersInformationSubTotals(params ContestCountingCircleDetails[] details)
    {
        var subTotalBySexAndVoterType = new Dictionary<(SexType, VoterType), DomainOfInfluenceCountOfVotersInformationSubTotal>();

        foreach (var detail in details)
        {
            foreach (var subTotal in detail.CountOfVotersInformationSubTotals)
            {
                var subTotalKey = (subTotal.Sex, subTotal.VoterType);
                if (!subTotalBySexAndVoterType.TryGetValue(subTotalKey, out var doiSubTotal))
                {
                    doiSubTotal = new()
                    {
                        Sex = subTotal.Sex,
                        VoterType = subTotal.VoterType,
                    };
                    subTotalBySexAndVoterType.Add(subTotalKey, doiSubTotal);
                }

                doiSubTotal.CountOfVoters += subTotal.CountOfVoters.GetValueOrDefault();
            }
        }

        return subTotalBySexAndVoterType.Values.ToHashSet();
    }
}
