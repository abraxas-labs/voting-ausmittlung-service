// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionResultEntryParams : ElectionResultEntryParams
{
    public ProportionalElectionReviewProcedure ReviewProcedure { get; set; }
}
