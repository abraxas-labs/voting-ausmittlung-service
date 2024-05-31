// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class DataExportTemplate
{
    public string ExportTemplateId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string EntityDescription { get; set; } = string.Empty;
}
