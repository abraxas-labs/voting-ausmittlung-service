// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class DataExportTemplate
{
    public string ExportTemplateId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string EntityDescription { get; set; } = string.Empty;

    public IReadOnlyCollection<Guid> PoliticalBusinessIds { get; set; } = Array.Empty<Guid>();

    public string TemplateKey { get; set; } = string.Empty;
}
