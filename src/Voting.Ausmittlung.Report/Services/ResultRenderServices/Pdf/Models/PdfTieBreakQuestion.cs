// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfTieBreakQuestion
{
    public PdfVoteQuestionLabel Label { get; set; }

    public int Number { get; set; }

    public string Question { get; set; } = string.Empty;

    public int Question1Number { get; set; }

    public int Question2Number { get; set; }
}
