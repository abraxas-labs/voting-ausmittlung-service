// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfBallotQuestion
{
    public PdfVoteQuestionLabel Label { get; set; }

    public int Number { get; set; }

    public string Question { get; set; } = string.Empty;

    public BallotQuestionType Type { get; set; }
}
