// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class ProtocolExportResponse
{
    public Guid ExportTemplateId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string EntityDescription { get; set; } = string.Empty;

    public Guid? ProtocolExportId { get; set; }

    public ProtocolExportState State { get; set; }

    public string FileName { get; set; } = string.Empty;

    public DateTime? Started { get; set; }
}
