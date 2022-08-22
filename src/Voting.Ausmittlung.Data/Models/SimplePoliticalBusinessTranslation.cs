// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class SimplePoliticalBusinessTranslation : PoliticalBusinessTranslation
{
    public Guid SimplePoliticalBusinessId { get; set; }

    public SimplePoliticalBusiness? SimplePoliticalBusiness { get; set; }
}
