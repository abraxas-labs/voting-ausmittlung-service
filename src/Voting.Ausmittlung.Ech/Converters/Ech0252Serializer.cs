// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0058_5_0;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Mapping;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Ech;

namespace Voting.Ausmittlung.Ech.Converters;

public class Ech0252Serializer
{
    private const string MessageType = "1198";
    private const int UnparsablePbNumer = int.MaxValue;
    private readonly DeliveryHeaderProvider _deliveryHeaderProvider;

    public Ech0252Serializer(DeliveryHeaderProvider deliveryHeaderProvider)
    {
        _deliveryHeaderProvider = deliveryHeaderProvider;
    }

    public Delivery ToVoteDelivery(Contest contest, Ech0252MappingContext ctx, IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var sequenceBySuperiorAuthorityId = new Dictionary<Guid, ushort>();
        var voteInfos = contest.Votes
            .Select(x => new { PbNumber = ParsePoliticalBusinessNumber(x), Vote = x })
            .OrderBy(x => x.PbNumber)
            .ThenBy(x => x.Vote.DomainOfInfluence.Type)
            .ThenBy(x => x.Vote.Id)
            .SelectMany(x => x.Vote.Ballots
                .OrderBy(b => b.Position)
                .SelectMany(b => b.ToVoteInfoEchVote(ctx, enabledResultStates, sequenceBySuperiorAuthorityId)))
            .ToList();

        return new Delivery
        {
            DeliveryHeader = BuildDeliveryHeader(contest),
            VoteBaseDelivery = new EventVoteBaseDeliveryType
            {
                CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
                PollingDay = contest.Date,
                NumberOfEntries = (ushort)voteInfos.Count,
                VoteInfo = voteInfos,
            },
        };
    }

    public Delivery ToProportionalElectionResultDelivery(Contest contest)
    {
        var electionDelivery = new EventElectionResultDeliveryType
        {
            CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
            PollingDay = contest.Date,
            ElectionGroupResult = contest.ProportionalElections.ToVoteInfoEchProportionalElectionGroupResults().ToList(),
        };

        electionDelivery.NumberOfEntries = (ushort)electionDelivery.ElectionGroupResult.Count;

        return new Delivery
        {
            DeliveryHeader = BuildDeliveryHeader(contest),
            ElectionResultDelivery = electionDelivery,
        };
    }

    public Delivery ToMajorityElectionResultDelivery(Contest contest)
    {
        var electionDelivery = new EventElectionResultDeliveryType
        {
            CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
            PollingDay = contest.Date,
            ElectionGroupResult = contest.MajorityElections.ToVoteInfoEchMajorityElectionGroupResults().ToList(),
        };

        electionDelivery.NumberOfEntries = (ushort)electionDelivery.ElectionGroupResult.Count;

        return new Delivery
        {
            DeliveryHeader = BuildDeliveryHeader(contest),
            ElectionResultDelivery = electionDelivery,
        };
    }

    public Delivery ToProportionalElectionInformationDelivery(Contest contest, Ech0252MappingContext ctx)
    {
        var electionDelivery = new EventElectionInformationDeliveryType
        {
            CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
            PollingDay = contest.Date,
            ElectionAssociation = contest.ProportionalElectionUnions.ToVoteInfoEchElectionAssociations().ToList(),
            ElectionGroupInfo = contest.ProportionalElections.ToVoteInfoEchProportionalElectionGroups(ctx).ToList(),
        };

        electionDelivery.NumberOfEntries = (ushort)electionDelivery.ElectionGroupInfo.Count;

        return new Delivery
        {
            DeliveryHeader = BuildDeliveryHeader(contest),
            ElectionInformationDelivery = electionDelivery,
        };
    }

    public Delivery ToMajorityElectionInformationDelivery(Contest contest, Ech0252MappingContext ctx)
    {
        var electionDelivery = new EventElectionInformationDeliveryType
        {
            CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
            PollingDay = contest.Date,
            ElectionAssociation = contest.MajorityElectionUnions.ToVoteInfoEchElectionAssociations().ToList(),
            ElectionGroupInfo = contest.MajorityElections.ToVoteInfoEchMajorityElectionGroups(ctx).ToList(),
        };

        electionDelivery.NumberOfEntries = (ushort)electionDelivery.ElectionGroupInfo.Count;

        return new Delivery
        {
            DeliveryHeader = BuildDeliveryHeader(contest),
            ElectionInformationDelivery = electionDelivery,
        };
    }

    private int ParsePoliticalBusinessNumber(Vote vote)
    {
        return int.TryParse(vote.PoliticalBusinessNumber, out var pbNumber)
            ? pbNumber
            : UnparsablePbNumer;
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

    private HeaderType BuildDeliveryHeader(Contest contest)
    {
        var header = _deliveryHeaderProvider.BuildHeader(!contest.TestingPhaseEnded);
        header.MessageType = MessageType;
        return header;
    }
}
