// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ResultExportConfigurationPoliticalBusiness : BaseEntity
{
    public SimplePoliticalBusiness? PoliticalBusiness { get; set; }

    public Guid PoliticalBusinessId { get; set; }

    public ResultExportConfiguration? ResultExportConfiguration { get; set; }

    public Guid ResultExportConfigurationId { get; set; }
}
