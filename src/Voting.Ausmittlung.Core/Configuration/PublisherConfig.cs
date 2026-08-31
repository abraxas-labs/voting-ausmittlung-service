// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Configuration;
using Voting.Ausmittlung.TemporaryData.Configuration;
using Voting.Lib.DokConnector.Configuration;
using Voting.Lib.Ech.Configuration;
using Voting.Lib.Iam.TokenHandling.ServiceToken;
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

    /// <summary>
    /// Gets or sets a value indicating whether grpc-web should be used or plain grpc.
    /// Remarks: Only inteded to be used for development and testing purposes.
    /// </summary>
    public bool EnableGrpcWeb { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether grpc reflection endpoint should be enabled or not.
    /// </summary>
    public bool EnableGrpcReflection { get; set; }

    public DmDocConfig Documatrix { get; set; } = new() { DataSerializationFormat = Lib.DmDoc.Configuration.DmDocDataSerializationFormat.Xml };

    public ResultExportJobConfig AutomaticExports { get; set; } = new();

    public ExportRateLimitConfig ExportRateLimit { get; set; } = new();

    public EchConfig Ech { get; set; } = new(typeof(AppConfig).Assembly);

    public JobConfig CleanSecondFactorTransactionsJob { get; set; } = new() { Interval = TimeSpan.FromHours(1) };

    public JobConfig CleanExportLogEntriesJob { get; set; } = new() { Interval = TimeSpan.FromHours(1) };

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
    /// Gets or sets a list of template key, for which the canton suffix will be enabled. After splitting this variable can be decomposed.
    /// </summary>
    public HashSet<string> EnableCantonSuffixTemplateKeys { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether all exports should be disabled.
    /// </summary>
    public bool DisableAllExports { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the export template key canton suffix is enabled or not. Default is 'false'. After splitting this variable can be decomposed.
    /// </summary>
    public bool ExportTemplateKeyCantonSuffixEnabled { get; set; }

    /// <summary>
    /// Gets or sets the connect service account.
    /// </summary>
    public SecureConnectServiceAccountOptions ConnectServiceAccount { get; set; } = new();

    public DokConnectorConfig DokConnectorApi { get; set; } = new();

    public SeantisConfig Seantis { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the DOK connector is mocked (mainly useful for local development).
    /// </summary>
    public bool EnableDokConnectorMock { get; set; }

    /// <summary>
    /// Gets or sets the "test counting circles" (Testurnen), which are ignored during a result import.
    /// </summary>
    public Dictionary<DomainOfInfluenceCanton, List<TestCountingCircleConfig>> TestCountingCircles { get; set; } = new();

    public TemplateConfiguration TemplateConfig { get; set; } = new();
}
