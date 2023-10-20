// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionResultSubTotal
{
    public int TotalCountOfUnmodifiedLists { get; set; }

    public int TotalCountOfModifiedLists { get; set; }

    public int TotalCountOfListsWithoutParty { get; set; }

    public int TotalCountOfBallots { get; set; }

    public int TotalCountOfBlankRowsOnListsWithoutParty { get; set; }

    public int TotalCountOfListsWithParty { get; set; }

    public int TotalCountOfLists { get; set; }
}
