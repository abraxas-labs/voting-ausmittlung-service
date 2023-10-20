// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

// this needs to be in sync with the aggregate names in basis and ausmittlung.
internal static class AggregateNames
{
    public const string CountingCircle = "voting-countingCircles";
    public const string Contest = "voting-contests";

    public const string Vote = "voting-votes";
    public const string ProportionalElection = "voting-proportionalElections";
    public const string MajorityElection = "voting-majorityElections";

    public const string MajorityElectionUnion = "voting-majorityElectionUnions";
    public const string ProportionalElectionUnion = "voting-proportionalElectionUnions";

    public const string ContestEventSignatureBasis = "voting-contestEventSignatureBasis";
    public const string ContestEventSignatureAusmittlung = "voting-contestEventSignatureAusmittlung";

    public static string Build(string aggregateName, Guid id) => $"{aggregateName}-{id}";
}
