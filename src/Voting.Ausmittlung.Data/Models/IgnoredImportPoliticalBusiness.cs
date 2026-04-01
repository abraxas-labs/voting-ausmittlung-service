// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class IgnoredImportPoliticalBusiness : BaseEntity
{
    public Guid PoliticalBusinessId { get; set; }

    public SimplePoliticalBusiness? PoliticalBusiness { get; set; }

    public Guid ResultImportId { get; set; }

    public ResultImport? ResultImport { get; set; }
}
