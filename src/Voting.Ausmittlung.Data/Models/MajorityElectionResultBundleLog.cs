// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionResultBundleLog : PoliticalBusinessResultBundleLog
{
    public MajorityElectionResultBundle Bundle { get; set; } = null!;

    public Guid BundleId { get; set; }
}
