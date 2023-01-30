// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using eCH_0222_1_0;
using eCH_0222_1_0.Standard;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Mapping;
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
        return WrapInDelivery(new EventRawDataDelivery
        {
            ReportingBody = GetReportingBody(proportionalElection.Contest),
            RawData = new RawDataType
            {
                ContestIdentification = proportionalElection.Contest.Id.ToString(),
                CountingCircleRawData = proportionalElection.Results
                    .OrderBy(r => r.CountingCircle.BasisCountingCircleId).Select(r =>
                        new CountingCircleRawData
                        {
                            countingCircleId = r.CountingCircle.BasisCountingCircleId.ToString(),
                            electionGroupBallotRawData = new[] { r.ToEchElectionGroupRawData() },
                        })
                    .Where(x => x.electionGroupBallotRawData.Length > 0)
                    .ToArray(),
            },
        });
    }

    public Delivery ToDelivery(MajorityElection majorityElection)
    {
        return WrapInDelivery(new EventRawDataDelivery
        {
            ReportingBody = GetReportingBody(majorityElection.Contest),
            RawData = new RawDataType
            {
                ContestIdentification = majorityElection.Contest.Id.ToString(),
                CountingCircleRawData = majorityElection.Results
                    .Where(x => x.Entry == MajorityElectionResultEntry.Detailed)
                    .OrderBy(r => r.CountingCircle.BasisCountingCircleId)
                    .Select(r => new CountingCircleRawData
                    {
                        countingCircleId = r.CountingCircle.BasisCountingCircleId.ToString(),
                        electionGroupBallotRawData = new[] { r.ToEchElectionGroupRawData() },
                    })
                    .Where(x => x.electionGroupBallotRawData.Length > 0)
                    .ToArray(),
            },
        });
    }

    public Delivery ToDelivery(Vote vote)
    {
        return WrapInDelivery(new EventRawDataDelivery
        {
            ReportingBody = GetReportingBody(vote.Contest),
            RawData = new RawDataType
            {
                ContestIdentification = vote.Contest.Id.ToString(),
                CountingCircleRawData = vote.Results
                    .Where(x => x.Entry == VoteResultEntry.Detailed)
                    .OrderBy(r => r.CountingCircle.BasisCountingCircleId)
                    .Select(r => new CountingCircleRawData
                    {
                        countingCircleId = r.CountingCircle.BasisCountingCircleId.ToString(),
                        VoteRawData = new[] { r.ToEchVoteRawData() },
                    })
                    .Where(x => x.VoteRawData.Length > 0)
                    .ToArray(),
            },
        });
    }

    private ReportingBodyType GetReportingBody(Contest contest)
    {
        return ReportingBodyType.Create(contest.DomainOfInfluence.SecureConnectId, _clock.UtcNow);
    }

    private Delivery WrapInDelivery(EventRawDataDelivery data)
    {
        return new Delivery
        {
            DeliveryHeader = _deliveryHeaderProvider.BuildHeader(),
            RawDataDelivery = data,
        };
    }
}
