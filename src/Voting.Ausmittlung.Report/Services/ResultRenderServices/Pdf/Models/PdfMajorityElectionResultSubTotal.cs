// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionResultSubTotal
{
    public int IndividualVoteCount { get; set; }

    public int EmptyVoteCountInclWriteIns { get; set; }

    public int InvalidVoteCount { get; set; }

    public int TotalEmptyAndInvalidVoteCount { get; set; }

    public int TotalCandidateVoteCountExclIndividual { get; set; }

    public int TotalCandidateVoteCountInclIndividual { get; set; }

    public int TotalVoteCount { get; set; }

    public int EmptyVoteCountWriteIns { get; set; }

    public int EmptyVoteCountExclWriteIns { get; set; }
}
