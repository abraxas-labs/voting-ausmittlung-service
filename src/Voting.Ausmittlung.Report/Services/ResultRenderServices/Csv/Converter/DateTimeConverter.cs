// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.Converter;

public class DateTimeConverter : TypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        if (!(value is DateTime dt))
        {
            return string.Empty;
        }

        return TimeZoneUtil.UtcToLocal(dt)
            .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}
