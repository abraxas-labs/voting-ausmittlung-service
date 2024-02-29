// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;

/// <summary>
/// Converts a count of majority elections to a wabstiC wahltyp.
/// </summary>
public class WabstiCMajoritySecondaryElectionsCountConverter : TypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        return value switch
        {
            0 => "4",
            1 => "5",
            _ => "6",
        };
    }
}
