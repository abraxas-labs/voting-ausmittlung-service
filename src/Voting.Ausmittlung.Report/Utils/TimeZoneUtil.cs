// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.Utils;

public static class TimeZoneUtil
{
    private const string TzZurichIanaName = "Europe/Zurich";

    private static readonly TimeZoneInfo ReportsTimeZone = TimeZoneInfo.FindSystemTimeZoneById(TzZurichIanaName);

    internal static DateTime UtcToLocal(DateTime utcDateTime)
        => TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, ReportsTimeZone);
}
