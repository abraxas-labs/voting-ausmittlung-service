// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ContestDetailsMockedData
{
    public const string IdUrnengangBundContestDetails = "0e92524a-e3e9-4b14-a0ff-cb73184bdfed";
    public const string IdUrnengangGossauContestDetails = "a5e2596c-80be-43dd-b26c-1b0666bda07e";
    public const string IdUrnengangStGallenContestDetails = "312512e9-ce57-497f-9d2d-fdc5af860d31";

    public static ContestDetails UrnengangBundContestDetails
        => new ContestDetails
        {
            Id = Guid.Parse(IdUrnengangBundContestDetails),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            VotingCards = BuildVotingCards(ContestCountingCircleDetailsMockData.GossauUrnengangBund, ContestCountingCircleDetailsMockData.UzwilUrnengangBund, ContestCountingCircleDetailsMockData.StGallenUrnengangBund),
            CountOfVotersInformationSubTotals = BuildCountOfVotersInformationSubTotals(ContestCountingCircleDetailsMockData.GossauUrnengangBund, ContestCountingCircleDetailsMockData.UzwilUrnengangBund, ContestCountingCircleDetailsMockData.StGallenUrnengangBund),
        };

    public static ContestDetails UrnengangGossauContestDetails
        => new ContestDetails
        {
            Id = Guid.Parse(IdUrnengangGossauContestDetails),
            ContestId = Guid.Parse(ContestMockedData.IdGossau),
            VotingCards = BuildVotingCards(ContestCountingCircleDetailsMockData.GossauUrnengangGossau),
            CountOfVotersInformationSubTotals = BuildCountOfVotersInformationSubTotals(ContestCountingCircleDetailsMockData.GossauUrnengangGossau),
        };

    public static ContestDetails UrnengangStGallenDetails
        => new ContestDetails
        {
            Id = Guid.Parse(IdUrnengangStGallenContestDetails),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            VotingCards = BuildVotingCards(ContestCountingCircleDetailsMockData.GossauUrnengangStGallen),
            CountOfVotersInformationSubTotals = BuildCountOfVotersInformationSubTotals(ContestCountingCircleDetailsMockData.GossauUrnengangStGallen),
        };

    public static IEnumerable<ContestDetails> All
    {
        get
        {
            yield return UrnengangBundContestDetails;
            yield return UrnengangGossauContestDetails;
            yield return UrnengangStGallenDetails;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.ContestDetails.AddRange(All);
            await db.SaveChangesAsync();
        });
    }

    private static HashSet<ContestVotingCardResultDetail> BuildVotingCards(params ContestCountingCircleDetails[] details)
    {
        var votingCardByChannelValidAndDoiType = new Dictionary<(VotingChannel, bool, DomainOfInfluenceType), ContestVotingCardResultDetail>();

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

    private static HashSet<ContestCountOfVotersInformationSubTotal> BuildCountOfVotersInformationSubTotals(params ContestCountingCircleDetails[] details)
    {
        var subTotalBySexVoterTypeAndDoiType = new Dictionary<(SexType, VoterType, DomainOfInfluenceType), ContestCountOfVotersInformationSubTotal>();

        foreach (var detail in details)
        {
            foreach (var subTotal in detail.CountOfVotersInformationSubTotals)
            {
                var subTotalKey = (subTotal.Sex, subTotal.VoterType, subTotal.DomainOfInfluenceType);
                if (!subTotalBySexVoterTypeAndDoiType.TryGetValue(subTotalKey, out var doiSubTotal))
                {
                    doiSubTotal = new()
                    {
                        Sex = subTotal.Sex,
                        VoterType = subTotal.VoterType,
                        DomainOfInfluenceType = subTotal.DomainOfInfluenceType,
                    };
                    subTotalBySexVoterTypeAndDoiType.Add(subTotalKey, doiSubTotal);
                }

                doiSubTotal.CountOfVoters += subTotal.CountOfVoters.GetValueOrDefault();
            }
        }

        return subTotalBySexVoterTypeAndDoiType.Values.ToHashSet();
    }
}
