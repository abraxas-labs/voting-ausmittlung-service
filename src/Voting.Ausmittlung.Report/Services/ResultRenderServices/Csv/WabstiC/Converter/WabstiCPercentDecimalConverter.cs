// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;

/// <summary>
/// Formats a percentage value according to the wabstiC specification
/// for percentages which are formatted in the decimal format.
/// ex: 12.343% should be represented as 12.34.
/// </summary>
public class WabstiCPercentDecimalConverter : DefaultTypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        return value == null ? string.Empty : Math.Round((decimal)value * 100, 2, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
    }
}
