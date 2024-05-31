// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Mapping;
using Voting.Lib.Ech;

namespace Voting.Ausmittlung.Ech.Converters;

public class Ech0252Serializer
{
    private readonly DeliveryHeaderProvider _deliveryHeaderProvider;

    public Ech0252Serializer(DeliveryHeaderProvider deliveryHeaderProvider)
    {
        _deliveryHeaderProvider = deliveryHeaderProvider;
    }

    public Delivery ToVoteDelivery(Contest contest)
    {
        return new Delivery
        {
            DeliveryHeader = _deliveryHeaderProvider.BuildHeader(),
            VoteBaseDelivery = new EventVoteBaseDeliveryType
            {
                CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
                PollingDay = contest.Date,
                NumberOfEntries = (ushort)contest.Votes.Count,
                VoteInfo = contest.Votes
                    .OrderBy(v => v.DomainOfInfluence.Type)
                    .ThenBy(v => v.PoliticalBusinessNumber)
                    .SelectMany(x => x.Ballots.OrderBy(b => b.Position).SelectMany(b => b.ToVoteInfoEchVote())).ToList(),
            },
        };
    }

    public Delivery ToProportionalElectionDelivery(Contest contest)
    {
        var electionDelivery = new EventElectionResultDeliveryType
        {
            CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
            PollingDay = contest.Date,
            ElectionGroupResult = contest.ProportionalElections.ToVoteInfoEchProportionalElectionGroups().ToList(),
        };

        electionDelivery.NumberOfEntries = (ushort)electionDelivery.ElectionGroupResult.Count;

        return new Delivery
        {
            DeliveryHeader = _deliveryHeaderProvider.BuildHeader(),
            ElectionResultDelivery = electionDelivery,
        };
    }

    public Delivery ToMajorityElectionDelivery(Contest contest)
    {
        var electionDelivery = new EventElectionResultDeliveryType
        {
            CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
            PollingDay = contest.Date,
            ElectionGroupResult = contest.MajorityElections.ToVoteInfoEchMajorityElectionGroups().ToList(),
        };

        electionDelivery.NumberOfEntries = (ushort)electionDelivery.ElectionGroupResult.Count;

        return new Delivery
        {
            DeliveryHeader = _deliveryHeaderProvider.BuildHeader(),
            ElectionResultDelivery = electionDelivery,
        };
    }

    private byte ToCantonId(DomainOfInfluenceCanton canton)
    {
        return canton switch
        {
            DomainOfInfluenceCanton.Zh => 1,
            DomainOfInfluenceCanton.Sg => 17,
            DomainOfInfluenceCanton.Tg => 20,
            DomainOfInfluenceCanton.Gr => 18,
            _ => throw new InvalidOperationException($"Canton {canton} not supported"),
        };
    }
}
