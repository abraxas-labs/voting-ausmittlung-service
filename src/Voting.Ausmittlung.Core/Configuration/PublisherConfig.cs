// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.TemporaryData.Configuration;
using Voting.Lib.DmDoc.Configuration;
using Voting.Lib.Ech.Configuration;
using Voting.Lib.Scheduler;

namespace Voting.Ausmittlung.Core.Configuration;

public class PublisherConfig
{
    public TemporaryDataConfig TemporaryDatabase { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether detailed errors are enabled. Should not be enabled in production environments,
    /// as this could expose information about the internals of this service.
    /// </summary>
    public bool EnableDetailedErrors { get; set; }

    public bool EnableGrpcWeb { get; set; } // this should only be enabled for testing purposes

    public DmDocConfig Documatrix { get; set; } = new() { DataSerializationFormat = DmDocDataSerializationFormat.Xml };

    public ResultExportJobConfig AutomaticExports { get; set; } = new();

    public EchConfig Ech { get; set; } = new(typeof(AppConfig).Assembly)
    {
        SenderId = "ABX-VOTING",
        Product = "Voting.Ausmittlung",
    };

    public JobConfig CleanSecondFactorTransactionsJob { get; set; } = new() { Interval = TimeSpan.FromHours(1) };

    public int SecondFactorTransactionExpiredAtMinutes { get; set; } = 10;

    public HashSet<string> LanguageHeaderIgnoredPaths { get; set; } = new()
    {
        "/healthz",
        "/healthz/masstransit",
        "/metrics",
    };

    /// <summary>
    /// Gets or sets a list of template key, for which the export should be disabled.
    /// </summary>
    public HashSet<string> DisabledExportTemplateKeys { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether all exports should be disabled.
    /// </summary>
    public bool DisableAllExports { get; set; }
}