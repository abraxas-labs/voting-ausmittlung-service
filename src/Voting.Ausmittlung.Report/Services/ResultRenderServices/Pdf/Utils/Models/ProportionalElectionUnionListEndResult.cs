// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public class ProportionalElectionUnionListEndResult
{
    public ProportionalElectionUnionList? UnionList { get; set; }

    public int NumberOfMandates { get; set; }

    /// <summary>
    /// Gets or sets the count of candidate votes gained from from unmodified and modified lists.
    /// </summary>
    public int ListVotesCount { get; set; }

    /// <summary>
    /// Gets or sets the count of votes gained from blank rows from unmodified and modified lists.
    /// </summary>
    public int BlankRowsCount { get; set; }

    /// <summary>
    /// Gets the sum list votes count and blank rows count.
    /// </summary>
    public int TotalVoteCount => ListVotesCount + BlankRowsCount;
}
