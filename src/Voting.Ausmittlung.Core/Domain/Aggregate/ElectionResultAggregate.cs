// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public abstract class ElectionResultAggregate : CountingCircleResultAggregate
{
    public PoliticalBusinessCountOfVoters CountOfVoters { get; protected set; } = new();

    /// <summary>
    /// Gets a set of all BundleNumbers which are currently in use or were in use by a deleted bundle.
    /// </summary>
    protected List<int> BundleNumbers { get; } = new();

    protected int GetNextBundleNumber()
    {
        return BundleNumbers.Count == 0 ? 1 : BundleNumbers.Max() + 1;
    }

    protected void ResetBundleNumbers()
    {
        BundleNumbers.Clear();
    }
}
