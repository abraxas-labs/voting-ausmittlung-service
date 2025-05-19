// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionResultBundleLog : PoliticalBusinessResultBundleLog
{
    public ProportionalElectionResultBundle Bundle { get; set; } = null!;

    public Guid BundleId { get; set; }
}
