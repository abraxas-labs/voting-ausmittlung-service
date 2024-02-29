// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class PoliticalBusinessResultBundleAggregate
{
    public PoliticalBusinessResultBundleAggregate(Guid id, Guid resultId, int bundleNumber)
    {
        Id = id;
        ResultId = resultId;
        BundleNumber = bundleNumber;
    }

    public Guid Id { get; }

    public Guid ResultId { get; }

    public int BundleNumber { get; set; }
}
