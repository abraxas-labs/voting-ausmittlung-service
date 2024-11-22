// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Ech0222_1_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Mapping;
using Voting.Ausmittlung.Ech.Utils;
using Voting.Lib.Common;
using Voting.Lib.Ech;

namespace Voting.Ausmittlung.Ech.Converters;

public class Ech0222Serializer
{
    private readonly IClock _clock;
    private readonly DeliveryHeaderProvider _deliveryHeaderProvider;

    public Ech0222Serializer(IClock clock, DeliveryHeaderProvider deliveryHeaderProvider)
    {
        _clock = clock;
        _deliveryHeaderProvider = deliveryHeaderProvider;
    }

    public Delivery ToDelivery(ProportionalElection proportionalElection)
    {
        return WrapInDelivery(
            new EventRawDataDelivery
            {
                ReportingBody = GetReportingBody(proportionalElection.Contest),
                RawData = new RawDataType
                {
                    ContestIdentification = proportionalElection.Contest.Id.ToString(),
                    CountingCircleRawData = proportionalElection.Results
                        .OrderBy(r => r.CountingCircle.BasisCountingCircleId).Select(r =>
                            new RawDataTypeCountingCircleRawData
                            {
                                CountingCircleId = r.CountingCircle.BasisCountingCircleId.ToString(),
                                ElectionGroupBallotRawData =
                                    new List<RawDataTypeCountingCircleRawDataElectionGroupBallotRawData>
                                    {
                                        r.ToEchElectionGroupRawData(),
                                    },
                            })
                        .Where(x => x.ElectionGroupBallotRawData.Count > 0)
                        .ToList(),
                },
            },
            proportionalElection.Contest);
    }

    public Delivery ToDelivery(MajorityElection majorityElection)
    {
        return WrapInDelivery(
            new EventRawDataDelivery
            {
                ReportingBody = GetReportingBody(majorityElection.Contest),
                RawData = new RawDataType
                {
                    ContestIdentification = majorityElection.Contest.Id.ToString(),
                    CountingCircleRawData = majorityElection.Results
                        .Where(x => x.Entry == MajorityElectionResultEntry.Detailed)
                        .OrderBy(r => r.CountingCircle.BasisCountingCircleId)
                        .Select(r => new RawDataTypeCountingCircleRawData
                        {
                            CountingCircleId = r.CountingCircle.BasisCountingCircleId.ToString(),
                            ElectionGroupBallotRawData =
                                new List<RawDataTypeCountingCircleRawDataElectionGroupBallotRawData>
                                {
                                    r.ToEchElectionGroupRawData(),
                                },
                        })
                        .Where(x => x.ElectionGroupBallotRawData.Count > 0)
                        .ToList(),
                },
            },
            majorityElection.Contest);
    }

    public Delivery ToDelivery(Vote vote)
    {
        return WrapInDelivery(
            new EventRawDataDelivery
            {
                ReportingBody = GetReportingBody(vote.Contest),
                RawData = new RawDataType
                {
                    ContestIdentification = vote.Contest.Id.ToString(),
                    CountingCircleRawData = vote.Results
                        .Where(x => x.Entry == VoteResultEntry.Detailed)
                        .OrderBy(r => r.CountingCircle.BasisCountingCircleId)
                        .Select(r => new RawDataTypeCountingCircleRawData
                        {
                            CountingCircleId = r.CountingCircle.BasisCountingCircleId.ToString(),
                            VoteRawData = new List<VoteRawDataType> { r.ToEchVoteRawData() },
                        })
                        .Where(x => x.VoteRawData.Count > 0)
                        .ToList(),
                },
            },
            vote.Contest);
    }

    private ReportingBodyType GetReportingBody(Contest contest)
    {
        return new ReportingBodyType
        {
            ReportingBodyIdentification = contest.DomainOfInfluence.SecureConnectId,
            CreationDateTime = _clock.UtcNow,
        };
    }

    private Delivery WrapInDelivery(EventRawDataDelivery data, Contest contest)
    {
        var header = _deliveryHeaderProvider.BuildHeader(!contest.TestingPhaseEnded);
        header.Comment = DeliveryHeaderUtils.EnrichComment(header.Comment, contest.TestingPhaseEnded);
        return new Delivery
        {
            DeliveryHeader = header,
            RawDataDelivery = data,
        };
    }
}
