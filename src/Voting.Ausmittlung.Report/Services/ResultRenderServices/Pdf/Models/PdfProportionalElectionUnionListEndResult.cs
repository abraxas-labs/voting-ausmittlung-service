// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionUnionListEndResult
{
    public PdfProportionalElectionUnionList? UnionList { get; set; }

    public int NumberOfMandates { get; set; }

    public int ListVotesCount { get; set; }

    public int BlankRowsCount { get; set; }

    public int TotalVoteCount { get; set; }
}
