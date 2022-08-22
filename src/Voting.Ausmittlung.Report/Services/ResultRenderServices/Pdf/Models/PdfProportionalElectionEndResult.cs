// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionEndResult : PdfPoliticalBusinessEndResult
{
    public PdfPoliticalBusinessCountOfVoters? CountOfVoters { get; set; }

    [XmlElement("ProportionalElectionListEndResult")]
    public List<PdfProportionalElectionListEndResult> ListEndResults { get; set; } = new List<PdfProportionalElectionListEndResult>();

    [XmlElement("ProportionalElectionTotalListEndResult")]
    public PdfProportionalElectionListEndResult? TotalListEndResult { get; set; }

    [XmlElement("ProportionalElectionTotalListResultInclWithoutParty")]
    public PdfProportionalElectionListEndResult? TotalListResultInclWithoutParty { get; set; }

    [XmlElement("ProportionalElectionListUnionEndResult")]
    public PdfProportionalElectionListUnionEndResult? ListUnionEndResult { get; set; }

    [XmlElement("ProportionalElectionSubListUnionEndResult")]
    public PdfProportionalElectionListUnionEndResult? SubListUnionEndResult { get; set; }

    public PdfProportionalElectionEndResultCalculation? Calculation { get; set; }

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
