// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class ProtocolExportStateResponse
{
    public Guid ExportTemplateId { get; set; }

    public Guid ProtocolExportId { get; set; }

    public ProtocolExportState State { get; set; }

    public string FileName { get; set; } = string.Empty;

    public DateTime Started { get; set; }
}
