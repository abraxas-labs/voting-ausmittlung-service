// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;

/// <summary>
/// Formats a percentage value according to the wabstiC spec.
/// Four digits number representing the percentage incl. 2 decimals.
/// ex: 12.343% should be represented as 1234, 12.345% as 1235.
/// </summary>
public class WabstiCPercentageConverter : TypeConverter
{
    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        => ((int)Math.Round((decimal)value * 10_000)).ToString(CultureInfo.InvariantCulture);
}
