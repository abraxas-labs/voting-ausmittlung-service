// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Npgsql;

namespace Voting.Ausmittlung.Data.Configuration;

public class DataConfig
{
    public string ConnectionString => new NpgsqlConnectionStringBuilder
    {
        Host = Host,
        Port = Port,
        Username = User,
        Password = Pass,
        Database = Name,
        IncludeErrorDetail = EnableDetailedErrors,
        CommandTimeout = CommandTimeout,
    }.ToString();

    public string Host { get; set; } = string.Empty;

    public ushort Port { get; set; } = 5432;

    public string User { get; set; } = string.Empty;

    public string Pass { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool EnableSensitiveDataLogging { get; set; }

    public bool EnableDetailedErrors { get; set; }

    /// <summary>
    /// Gets or sets the command timout for database queries in seconds.
    /// Framework default is 30 sec.
    /// </summary>
    public ushort CommandTimeout { get; set; } = 30;
}
