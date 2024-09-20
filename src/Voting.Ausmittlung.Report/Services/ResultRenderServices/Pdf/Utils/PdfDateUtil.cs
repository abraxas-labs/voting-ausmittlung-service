// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Globalization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class PdfDateUtil
{
    public static string BuildDateForFilename(DateTime date)
    {
        return date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }
}
