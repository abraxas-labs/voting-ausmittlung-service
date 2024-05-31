// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;

/// <summary>
/// Converts a date time to a wabstiC time representation (HHmm) at the zurich time zone.
/// The input date time needs to be in utc.
/// </summary>
public class WabstiCTimeConverter : DefaultTypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var utcDateTime = (DateTime)value;
        var localDateTime = TimeZoneUtil.UtcToLocal(utcDateTime);
        return localDateTime.ToString("HHmm", CultureInfo.InvariantCulture);
    }
}
