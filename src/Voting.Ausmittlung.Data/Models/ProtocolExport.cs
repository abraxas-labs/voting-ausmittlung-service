// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ProtocolExport : BaseEntity
{
    public Contest? Contest { get; set; }

    public Guid ContestId { get; set; }

    /// <summary>
    /// Gets or sets the export template ID which uniquely identifies the export template.
    /// This includes information about which political business, counting circle etc.
    /// It is not contest specific. Templates that are only identified by a key (ex. the activity protocol) would
    /// have the same ExportTemplateId across contests.
    /// </summary>
    public Guid ExportTemplateId { get; set; }

    public DateTime Started { get; set; }

    public ProtocolExportState State { get; set; }

    /// <summary>
    /// Gets or sets the callback token. This token needs to be provided when completing the protocol export.
    /// </summary>
    public string CallbackToken { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the print job ID of DmDoc, which can be used to retrieve the generated export.
    /// </summary>
    public int PrintJobId { get; set; }
}
