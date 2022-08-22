// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public abstract class PdfPoliticalBusinessEndResult
{
    public int TotalCountOfVoters { get; set; }

    public int CountOfDoneCountingCircles { get; set; }

    public int TotalCountOfCountingCircles { get; set; }

    public bool AllCountingCirclesDone { get; set; }
}
