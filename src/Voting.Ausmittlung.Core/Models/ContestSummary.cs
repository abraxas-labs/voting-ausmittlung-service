// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models;

public class ContestSummary
{
    public Contest Contest { get; set; } = null!; // loaded by ef

    public List<ContestSummaryEntryDetails>? ContestEntriesDetails { get; set; }
}
