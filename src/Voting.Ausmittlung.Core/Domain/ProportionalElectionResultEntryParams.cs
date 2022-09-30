// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

public class ProportionalElectionResultEntryParams : ElectionResultEntryParams
{
    public ProportionalElectionReviewProcedure ReviewProcedure { get; set; }
}
