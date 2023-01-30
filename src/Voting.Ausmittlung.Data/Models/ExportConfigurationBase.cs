// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Diagnostics.CodeAnalysis;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class ExportConfigurationBase : BaseEntity
{
    public DomainOfInfluence DomainOfInfluence { get; set; } = null!;

    public Guid DomainOfInfluenceId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string EaiMessageType { get; set; } = string.Empty;

    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays.",
        Justification = "Simplifies the postgres mapping. Also this value is not really used by Voting.Basis.")]
    public string[] ExportKeys { get; set; } = Array.Empty<string>();

    public ExportProvider Provider { get; set; }
}
