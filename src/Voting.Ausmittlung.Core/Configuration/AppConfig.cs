// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Configuration;
using Voting.Ausmittlung.TemporaryData;
using Voting.Lib.Common.Net;
using Voting.Lib.Cryptography.Configuration;
using Voting.Lib.Cryptography.HealthChecks;
using Voting.Lib.Eventing.Configuration;
using Voting.Lib.Messaging.Configuration;

namespace Voting.Ausmittlung.Core.Configuration;

public class AppConfig
{
    public ServiceMode ServiceMode { get; set; } = ServiceMode.Hybrid;

    public PortConfig Ports { get; set; } = new();

    public EventStoreConfig EventStore { get; set; } = new();

    public DataConfig Database { get; set; } = new();

    public SecureConnectConfig SecureConnect { get; set; } = new();

    public Uri? SecureConnectApi { get; set; }

    public RabbitMqConfig RabbitMq { get; set; } = new();

    public CertificatePinningConfig CertificatePinning { get; set; } = new();

    /// <summary>
    /// Gets or sets the CORS config options used within the <see cref="Voting.Lib.Common.DependencyInjection.ApplicationBuilderExtensions"/>
    /// to configure the CORS middleware from <see cref="Microsoft.AspNetCore.Builder.CorsMiddlewareExtensions"/>.
    /// </summary>
    public CorsConfig Cors { get; set; } = new();

    public PublisherConfig Publisher { get; set; } = new();

    public TimeSpan PrometheusAdapterInterval { get; set; } = TimeSpan.FromSeconds(1);

    public bool PublisherModeEnabled
        => (ServiceMode & ServiceMode.Publisher) != 0;

    public bool EventProcessorModeEnabled
        => (ServiceMode & ServiceMode.EventProcessor) != 0;

    public Pkcs11Config Pkcs11 { get; set; } = new();

    public EventSignatureConfig EventSignature { get; set; } = new();

    public bool EnablePkcs11Mock { get; set; }

    public MachineConfig Machine { get; set; } = new();

    /// <summary>
    /// Gets or sets the health check names of all health checks which are considered as non mission-critical
    /// (if any of them is unhealthy the system may still operate but in a degraded state).
    /// For example this includes the masstransit bus which is only responsible for the live updates,
    /// which is not considered mission-critical for the voting applications since only the live view updates will not work.
    /// These health checks are monitored separately.
    /// </summary>
    public HashSet<string> LowPriorityHealthCheckNames { get; set; } = new()
    {
        "masstransit-bus", // live updates are not mission critical
        Pkcs11DeviceHealthCheck.Name, // if the pkcs11 does not work, the system may still work 99% of the time, except if a public key needs to be signed / verified.
        nameof(TemporaryDataContext), // the temporary db only contains the mfa references which only affect a small number of actions.
    };
}
