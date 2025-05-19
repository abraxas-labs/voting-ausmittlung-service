// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfHagenbachBischoffSimpleWinnerGroup : PdfHagenbachBischoffSimpleGroup
{
    public int PreviousNumberOfMandates { get; set; }

    public int NumberOfMandatesAfterWin
    {
        get => PreviousNumberOfMandates + 1;

        set
        {
            // only needed due to xml serializer
        }
    }
}
