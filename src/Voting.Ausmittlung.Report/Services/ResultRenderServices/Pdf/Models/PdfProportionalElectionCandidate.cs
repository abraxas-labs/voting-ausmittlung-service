// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionCandidate : PdfElectionCandidate
{
    public bool Accumulated { get; set; }

    public int AccumulatedPosition { get; set; }

    public string? NumberIncludingList { get; set; }
}
