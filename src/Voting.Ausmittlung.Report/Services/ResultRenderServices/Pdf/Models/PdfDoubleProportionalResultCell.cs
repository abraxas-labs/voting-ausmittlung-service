// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfDoubleProportionalResultCell
{
    public PdfProportionalElectionList? List { get; set; }

    public bool ProportionalElectionQuorumReached { get; set; }

    public int VoteCount { get; set; }

    public decimal VoterNumber { get; set; }

    public int SubApportionmentNumberOfMandates { get; set; }

    public decimal VoteCountPercentageInElection { get; set; }
}
