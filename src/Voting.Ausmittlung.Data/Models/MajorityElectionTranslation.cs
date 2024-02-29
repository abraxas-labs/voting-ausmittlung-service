// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionTranslation : PoliticalBusinessTranslation
{
    public Guid MajorityElectionId { get; set; }

    public MajorityElection? MajorityElection { get; set; }
}
