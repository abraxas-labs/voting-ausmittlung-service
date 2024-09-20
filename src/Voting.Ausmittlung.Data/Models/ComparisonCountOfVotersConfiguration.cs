// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ComparisonCountOfVotersConfiguration : BaseEntity
{
    public ComparisonCountOfVotersCategory Category { get; set; }

    public decimal? ThresholdPercent { get; set; }

    public PlausibilisationConfiguration PlausibilisationConfiguration { get; set; } = null!;

    public Guid PlausibilisationConfigurationId { get; set; }
}
