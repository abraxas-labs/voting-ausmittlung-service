// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;

public class WabstiCSexConverter : DefaultTypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        => value switch
        {
            SexType s when s == SexType.Female => "W",
            SexType s when s == SexType.Male => "M",
            _ => string.Empty,
        };
}
