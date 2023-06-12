// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Globalization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;

public static class WabstiCDateUtil
{
    private const string DateFormatForFilename = "yyyyMMdd";

    public static string BuildDateForFilename(DateTime date)
    {
        return date.ToString(DateFormatForFilename, CultureInfo.InvariantCulture);
    }
}
