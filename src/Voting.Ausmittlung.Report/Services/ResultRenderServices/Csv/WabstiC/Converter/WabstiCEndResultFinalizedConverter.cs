// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;

public class WabstiCEndResultFinalizedConverter : TypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        => value is true ? "2" : "1";
}
