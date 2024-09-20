// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class MajorityElectionAggregateSet : AggregateSet<MajorityElectionAggregate>
{
    public MajorityElectionAggregateSet(
        IAggregateRepository aggregateRepository,
        ILogger<MajorityElectionAggregateSet> logger)
        : base(aggregateRepository, logger)
    {
    }

    public MajorityElectionAggregate? GetBySecondaryMajorityElectionId(Guid secondaryMajorityElectionId)
    {
        return Aggregates.Values.FirstOrDefault(me => me.SecondaryMajorityElections.ContainsKey(secondaryMajorityElectionId));
    }
}
