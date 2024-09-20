// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;

/// <summary>
/// Converts a <see cref="DomainOfInfluenceType"/> to a wabstiC numeric value.
/// </summary>
public class WabstiCDomainOfIncluenceTypeConverter : DefaultTypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        return value switch
        {
            DomainOfInfluenceType.Ch => "1",
            DomainOfInfluenceType.Ct => "2",
            _ => "3",
        };
    }
}
