// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionListVoteSourceResult
{
    public PdfProportionalElectionSimpleList? List { get; set; }

    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets the VoteCount including EmptyRows.
    /// For all values except the current list it's the same value as the VoteCount.
    /// </summary>
    public int TotalVoteCount { get; set; }
}
