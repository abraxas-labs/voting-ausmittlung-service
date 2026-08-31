// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Globalization;

namespace Voting.Ausmittlung.Report.Utils;

public static class NumberFormatUtil
{
    private static readonly NumberFormatInfo SwissNumberFormatInfo = new()
    {
        NumberGroupSeparator = "'",
    };

    public static string FormatPercentage(decimal percentage, bool useLegacyFormat)
    {
        var format = useLegacyFormat
            ? "G"
            : "F2";
        var number = useLegacyFormat
            ? percentage
            : percentage * 100;
        return number.ToString(format, SwissNumberFormatInfo);
    }

    public static string FormatInteger(int number, bool useLegacyFormat)
    {
        var format = useLegacyFormat
            ? "D"
            : "N0";
        return number.ToString(format, SwissNumberFormatInfo);
    }

    public static string FormatDecimal(decimal number, bool useLegacyFormat, int decimals)
    {
        var format = useLegacyFormat
            ? "G"
            : "N" + decimals;
        return number.ToString(format, SwissNumberFormatInfo);
    }
}
