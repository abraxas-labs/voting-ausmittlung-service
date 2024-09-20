// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;

public class WabstiCUpperSnakeCaseConverter : DefaultTypeConverter
{
    public override string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        var s = value?.ToString();
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        // append first since no underline should be prefixed if it is upper (special start case).
        sb.Append(char.ToUpperInvariant(s[0]));

        for (var i = 1; i < s.Length; i++)
        {
            var c = s[i];
            if (!char.IsUpper(c))
            {
                sb.Append(char.ToUpperInvariant(c));
                continue;
            }

            sb.Append('_');
            sb.Append(c);
        }

        return sb.ToString();
    }
}
