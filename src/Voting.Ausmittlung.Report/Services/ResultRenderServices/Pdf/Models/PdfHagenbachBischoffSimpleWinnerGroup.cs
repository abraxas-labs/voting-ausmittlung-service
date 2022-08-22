// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfHagenbachBischoffSimpleWinnerGroup : PdfHagenbachBischoffSimpleGroup
{
    public int PreviousNumberOfMandates { get; set; }

    public int NumberOfMandatesAfterWin
    {
        get => PreviousNumberOfMandates + 1;

        // only needed due to xml serializer
        set => throw new NotImplementedException();
    }
}
