// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;

// needs to be in sync with basis.
public class SecondaryMajorityElection : IPoliticalBusiness
{
    public SecondaryMajorityElection()
    {
        PoliticalBusinessNumber = string.Empty;
        ShortDescription = new List<EventLogTranslation>();
    }

    public Guid Id { get; internal set; }

    public string PoliticalBusinessNumber { get; internal set; }

    public IReadOnlyCollection<EventLogTranslation> ShortDescription { get; internal set; }
}
