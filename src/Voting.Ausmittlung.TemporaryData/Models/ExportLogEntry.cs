// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.TemporaryData.Models;

public class ExportLogEntry : BaseEntity
{
    public string ExportKey { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
