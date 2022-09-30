// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfVoteResultBallotTieBreakQuestionAnswer
{
    public PdfTieBreakQuestion? Question { get; set; }

    public TieBreakQuestionAnswer Answer { get; set; }
}
