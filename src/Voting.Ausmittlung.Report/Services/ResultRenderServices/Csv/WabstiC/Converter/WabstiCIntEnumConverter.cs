// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;

/// <summary>
/// enum's would be converter to their string/name representation by default.
/// this converter can be used to convert to the int representation instead.
/// </summary>
public class WabstiCIntEnumConverter : DefaultTypeConverter
{
    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        => ((int)value).ToString(CultureInfo.InvariantCulture);
}
