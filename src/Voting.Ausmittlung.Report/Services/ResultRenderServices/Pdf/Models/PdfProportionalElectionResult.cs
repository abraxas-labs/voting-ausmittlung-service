// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionResult : PdfCountingCircleResult
{
    public PdfPoliticalBusinessCountOfVoters? CountOfVoters { get; set; }

    [XmlElement("ProportionalElectionListResult")]
    public List<PdfProportionalElectionListResult> ListResults { get; set; } = new List<PdfProportionalElectionListResult>();

    [XmlElement("ProportionalElectionTotalListResult")]
    public PdfProportionalElectionListResult? TotalListResult { get; set; }

    [XmlElement("ProportionalElectionTotalListResultInclWithoutParty")]
    public PdfProportionalElectionListResult? TotalListResultInclWithoutParty { get; set; }

    public PdfProportionalElectionResultSubTotal? ConventionalSubTotal { get; set; }

    public PdfProportionalElectionResultSubTotal? EVotingSubTotal { get; set; }

    /// <summary>
    /// Gets or sets the total count of unmodified lists with a party.
    /// </summary>
    public int TotalCountOfUnmodifiedLists { get; set; }

    /// <summary>
    /// Gets or sets the total count of modified lists with a party.
    /// </summary>
    public int TotalCountOfModifiedLists { get; set; }

    /// <summary>
    /// Gets or sets the count of lists without a source list / party.
    /// </summary>
    public int TotalCountOfListsWithoutParty { get; set; }

    /// <summary>
    /// Gets or sets the count of ballots (= total count of modified lists with and without a party).
    /// </summary>
    public int TotalCountOfBallots { get; set; }

    /// <summary>
    /// Gets or sets the count of votes gained from blank rows from lists/ballots without a source list / party.
    /// </summary>
    public int TotalCountOfBlankRowsOnListsWithoutParty { get; set; }

    /// <summary>
    /// Gets or sets the total count of lists with a party (modified + unmodified).
    /// </summary>
    public int TotalCountOfListsWithParty { get; set; }

    /// <summary>
    /// Gets or sets the total count of lists (without and with a party).
    /// </summary>
    public int TotalCountOfLists { get; set; }
}
