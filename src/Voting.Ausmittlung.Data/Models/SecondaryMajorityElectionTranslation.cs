// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionTranslation : PoliticalBusinessTranslation
{
    public Guid SecondaryMajorityElectionId { get; set; }

    public SecondaryMajorityElection? SecondaryMajorityElection { get; set; }
}
