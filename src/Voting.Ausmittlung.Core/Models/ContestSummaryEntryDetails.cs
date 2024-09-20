// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models;

public class ContestSummaryEntryDetails
{
    public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

    public int ContestEntriesCount { get; set; }
}
