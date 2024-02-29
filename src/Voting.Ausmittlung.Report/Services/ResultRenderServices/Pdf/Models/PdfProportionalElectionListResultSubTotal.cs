// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionListResultSubTotal
{
    public int UnmodifiedListsCount { get; set; }

    public int UnmodifiedListVotesCount { get; set; }

    public int UnmodifiedListBlankRowsCount { get; set; }

    public int ModifiedListsCount { get; set; }

    public int ModifiedListVotesCount { get; set; }

    public int ListVotesCountOnOtherLists { get; set; }

    public int ModifiedListBlankRowsCount { get; set; }

    public int UnmodifiedListVotesCountInclBlankRows { get; set; }

    public int ModifiedListVotesCountInclBlankRows { get; set; }

    public int ListVotesCount { get; set; }

    public int ListCount { get; set; }

    public int BlankRowsCount { get; set; }

    public int TotalVoteCount { get; set; }
}
