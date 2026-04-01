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
using Voting.Ausmittlung.Ech.Utils;
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

    public Delivery ToVoteDelivery(
        Contest contest,
        Ech0252MappingContext ctx,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var ballots = GetRelevantBallots(contest);
        return ToVoteDelivery(contest, ctx, ballots, enabledResultStates);
    }

    public Delivery ToVoteDelivery(
        Contest contest,
        Ech0252MappingContext ctx,
        IEnumerable<Ballot> ballots,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var sequenceBySuperiorAuthorityId = new Dictionary<Guid, ushort>();
        var voteInfos = ballots.SelectMany(b => b.ToVoteInfoEchVote(
                ctx,
                enabledResultStates,
                sequenceBySuperiorAuthorityId,
                b.Vote.Results.All(r => r.Published)))
            .ToList();
        return ToVoteDelivery(contest, voteInfos);
    }

    public IEnumerable<Ballot> GetRelevantBallots(Contest contest)
    {
        return contest.Votes
            .Where(IsInEchDelivery)
            .Select(x => new { PbNumber = ParsePoliticalBusinessNumber(x.PoliticalBusinessNumber), Vote = x })
            .OrderBy(x => x.PbNumber)
            .ThenBy(x => x.Vote.DomainOfInfluence.Type)
            .ThenBy(x => x.Vote.Id)
            .SelectMany(x => x.Vote.Ballots.OrderBy(b => b.Position));
    }

    public Delivery ToVoteDelivery(Contest contest, List<VoteInfoType> voteInfos)
    {
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

    public Delivery ToProportionalElectionResultDelivery(
        Contest contest,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates,
        bool includeCandidateListResultsInfo)
    {
        var elections = GetRelevantProportionalElections(contest);
        return ToProportionalElectionResultDelivery(contest, elections, enabledResultStates, includeCandidateListResultsInfo);
    }

    public Delivery ToProportionalElectionResultDelivery(
        Contest contest,
        List<ProportionalElection> elections,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates,
        bool includeCandidateListResultsInfo)
    {
        var electionDelivery = new EventElectionResultDeliveryType
        {
            CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
            PollingDay = contest.Date,
            ElectionGroupResult = elections
                .ToVoteInfoEchProportionalElectionGroupResults(enabledResultStates, includeCandidateListResultsInfo)
                .ToList(),
        };

        electionDelivery.NumberOfEntries = (ushort)electionDelivery.ElectionGroupResult.Count;

        return new Delivery
        {
            DeliveryHeader = BuildDeliveryHeader(contest),
            ElectionResultDelivery = electionDelivery,
        };
    }

    public List<ProportionalElection> GetRelevantProportionalElections(Contest contest)
    {
        return contest.ProportionalElections
            .Where(IsInEchDelivery)
            .Select(x => new { PbNumber = ParsePoliticalBusinessNumber(x.PoliticalBusinessNumber), ProportionalElection = x })
            .OrderBy(x => x.PbNumber)
            .ThenBy(x => x.ProportionalElection.DomainOfInfluence.Type)
            .ThenBy(x => x.ProportionalElection.Id)
            .Select(x => x.ProportionalElection)
            .ToList();
    }

    public Delivery ToProportionalElectionInformationDelivery(Contest contest, Ech0252MappingContext ctx)
    {
        var elections = GetRelevantProportionalElections(contest);
        return ToProportionalElectionInformationDelivery(contest, elections, ctx);
    }

    public Delivery ToProportionalElectionInformationDelivery(Contest contest, List<ProportionalElection> elections, Ech0252MappingContext ctx)
    {
        var positionBySuperiorAuthorityId = new Dictionary<Guid, int>();
        var electionDelivery = new EventElectionInformationDeliveryType
        {
            CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
            PollingDay = contest.Date,
            ElectionAssociation = contest.ProportionalElectionUnions.ToVoteInfoEchElectionAssociations().ToList(),
            ElectionGroupInfo = elections.ToVoteInfoEchProportionalElectionGroups(ctx, positionBySuperiorAuthorityId).ToList(),
        };

        electionDelivery.NumberOfEntries = (ushort)electionDelivery.ElectionGroupInfo.Count;

        return new Delivery
        {
            DeliveryHeader = BuildDeliveryHeader(contest),
            ElectionInformationDelivery = electionDelivery,
        };
    }

    public Delivery ToMajorityElectionResultDelivery(
        Contest contest,
        Ech0252MappingContext ctx,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var elections = GetRelevantMajorityElections(contest);
        return ToMajorityElectionResultDelivery(contest, elections, ctx, enabledResultStates);
    }

    public Delivery ToMajorityElectionResultDelivery(
        Contest contest,
        List<MajorityElection> elections,
        Ech0252MappingContext ctx,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var electionDelivery = new EventElectionResultDeliveryType
        {
            CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
            PollingDay = contest.Date,
            ElectionGroupResult = elections
                .ToVoteInfoEchMajorityElectionGroupResults(ctx, enabledResultStates)
                .ToList(),
        };

        electionDelivery.NumberOfEntries = (ushort)electionDelivery.ElectionGroupResult.Count;

        return new Delivery
        {
            DeliveryHeader = BuildDeliveryHeader(contest),
            ElectionResultDelivery = electionDelivery,
        };
    }

    public List<MajorityElection> GetRelevantMajorityElections(Contest contest)
    {
        return contest.MajorityElections
            .Where(IsInEchDelivery)
            .Select(x => new { PbNumber = ParsePoliticalBusinessNumber(x.PoliticalBusinessNumber), MajorityElection = x })
            .OrderBy(x => x.PbNumber)
            .ThenBy(x => x.MajorityElection.DomainOfInfluence.Type)
            .ThenBy(x => x.MajorityElection.Id)
            .Select(x => x.MajorityElection)
            .ToList();
    }

    public Delivery ToMajorityElectionInformationDelivery(Contest contest, Ech0252MappingContext ctx)
    {
        var elections = GetRelevantMajorityElections(contest);
        return ToMajorityElectionInformationDelivery(contest, elections, ctx);
    }

    public Delivery ToMajorityElectionInformationDelivery(Contest contest, List<MajorityElection> elections, Ech0252MappingContext ctx)
    {
        var positionBySuperiorAuthorityId = new Dictionary<Guid, int>();
        var electionDelivery = new EventElectionInformationDeliveryType
        {
            CantonId = ToCantonId(contest.DomainOfInfluence.Canton),
            PollingDay = contest.Date,
            ElectionAssociation = contest.MajorityElectionUnions.ToVoteInfoEchElectionAssociations().ToList(),
            ElectionGroupInfo = elections.ToVoteInfoEchMajorityElectionGroups(ctx, positionBySuperiorAuthorityId).ToList(),
        };

        electionDelivery.NumberOfEntries = (ushort)electionDelivery.ElectionGroupInfo.Count;

        return new Delivery
        {
            DeliveryHeader = BuildDeliveryHeader(contest),
            ElectionInformationDelivery = electionDelivery,
        };
    }

    private int ParsePoliticalBusinessNumber(string politicalBusinessNumber)
    {
        return int.TryParse(politicalBusinessNumber, out var pbNumber)
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
            DomainOfInfluenceCanton.Ar => 15,
            _ => throw new InvalidOperationException($"Canton {canton} not supported"),
        };
    }

    private HeaderType BuildDeliveryHeader(Contest contest)
    {
        var header = _deliveryHeaderProvider.BuildHeader(!contest.TestingPhaseEnded);
        header.MessageType = MessageType;
        header.Comment = DeliveryHeaderUtils.EnrichComment(header.Comment, contest.TestingPhaseEnded);
        return header;
    }

    private bool IsInEchDelivery<TPoliticalBusiness>(TPoliticalBusiness pb)
        where TPoliticalBusiness : PoliticalBusiness
    {
        return pb.Active && !pb.DomainOfInfluence.PublishResultsDisabled;
    }
}
