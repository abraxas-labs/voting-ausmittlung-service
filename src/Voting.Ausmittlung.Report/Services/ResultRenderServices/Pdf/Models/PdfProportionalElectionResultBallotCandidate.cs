// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionResultBallotCandidate
{
    public PdfProportionalElectionCandidate? Candidate { get; set; }

    public int Position { get; set; }

    public bool RemovedFromList { get; set; }

    public bool OnList { get; set; }
}
