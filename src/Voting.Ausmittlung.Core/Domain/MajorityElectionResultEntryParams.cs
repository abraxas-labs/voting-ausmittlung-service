// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

public class MajorityElectionResultEntryParams : ElectionResultEntryParams
{
    public MajorityElectionReviewProcedure ReviewProcedure { get; set; }
}
