// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// Basic export configuration from voting basis.
/// </summary>
public class ExportConfiguration : BaseEntity
{
    public DomainOfInfluence DomainOfInfluence { get; set; } = null!;

    public Guid DomainOfInfluenceId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string EaiMessageType { get; set; } = string.Empty;

    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays.",
        Justification = "Simplifies the postgres mapping. Also this value is not really used by Voting.Ausmittlung.")]
    public string[] ExportKeys { get; set; } = Array.Empty<string>();

    public ExportProvider Provider { get; set; }

    public ICollection<ResultExportConfiguration> ResultExportConfigurations { get; set; } = new HashSet<ResultExportConfiguration>();

    public string? SedexSenderId { get; set; }

    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays.",
        Justification = "Simplifies the postgres mapping. Also this value is not really used by Voting.Ausmittlung.")]
    public string[]? SedexRecipientIds { get; set; }
}
