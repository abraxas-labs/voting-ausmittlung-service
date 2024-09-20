// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class IgnoredImportCountingCircle : BaseEntity
{
    public string CountingCircleId { get; set; } = string.Empty;

    public string CountingCircleDescription { get; set; } = string.Empty;

    public bool IsTestCountingCircle { get; set; }

    public Guid ResultImportId { get; set; }

    public ResultImport? ResultImport { get; set; }
}
