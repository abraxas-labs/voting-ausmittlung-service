// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Voting.Ausmittlung.Core.Utils;
public class ProtocolExportMeter
{
    private const string MeterName = "Voting.Ausmittlung.Metrics";
    private const string InstrumentNamePrefix = "voting_ausmittlung_";
    private const string TemplateTypeTagName = "templateType";

    private static readonly Meter _meter = new(MeterName, GetVersion());

    private static readonly Counter<long> _protocolExportsStarted = _meter.CreateCounter<long>(
        InstrumentNamePrefix + "protocol_exports_started",
        "Export",
        "Number of started protocol exports");

    private static readonly Counter<long> _protocolExportsCompleted = _meter.CreateCounter<long>(
        InstrumentNamePrefix + "protocol_exports_completed",
        "Export",
        "Number of completed protocol exports");

    private static readonly Counter<long> _protocolExportsFailed = _meter.CreateCounter<long>(
        InstrumentNamePrefix + "protocol_exports_failed",
        "Export",
        "Number of failed protocol exports");

    private static readonly Counter<long> _protocolExportsInvalidCallbackToken = _meter.CreateCounter<long>(
        InstrumentNamePrefix + "protocol_exports_invalid_callback_token",
        "Export",
        "Number of protocol exports with invalid callback token");

    private static readonly Histogram<double> _protocolExportsDuration = _meter.CreateHistogram<double>(
        InstrumentNamePrefix + "protocol_exports_duration",
        "Export",
        "Duration of completed protocol exports");

    public static void AddExportStarted()
    {
        _protocolExportsStarted.Add(1);
    }

    public static void AddExportCompleted()
    {
        _protocolExportsCompleted.Add(1);
    }

    public static void AddExportFailed()
    {
        _protocolExportsFailed.Add(1);
    }

    public static void AddExportInvalidCallbackToken()
    {
        _protocolExportsInvalidCallbackToken.Add(1);
    }

    public static void AddExportDuration(TimeSpan duration, string templateType)
    {
        _protocolExportsDuration.Record(
            duration.TotalSeconds,
            KeyValuePair.Create<string, object?>(TemplateTypeTagName, templateType));
    }

    private static string GetVersion()
    {
        return typeof(ProtocolExportMeter)
                   .Assembly
                   .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                   ?.InformationalVersion
               ?? throw new InvalidOperationException("Could not find the assembly version.");
    }
}
