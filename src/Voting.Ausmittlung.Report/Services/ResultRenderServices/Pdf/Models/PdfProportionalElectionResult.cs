// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

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
    [XmlIgnore]
    public int TotalCountOfUnmodifiedLists { get; set; }

    [XmlElement("TotalCountOfUnmodifiedLists")]
    public string TotalCountOfUnmodifiedListsFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total count of modified lists with a party.
    /// </summary>
    [XmlIgnore]
    public int TotalCountOfModifiedLists { get; set; }

    [XmlElement("TotalCountOfModifiedLists")]
    public string TotalCountOfModifiedListsFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of lists without a source list / party.
    /// </summary>
    [XmlIgnore]
    public int TotalCountOfListsWithoutParty { get; set; }

    [XmlElement("TotalCountOfListsWithoutParty")]
    public string TotalCountOfListsWithoutPartyFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of ballots (= total count of modified lists with and without a party).
    /// </summary>
    [XmlIgnore]
    public int TotalCountOfBallots { get; set; }

    [XmlElement("TotalCountOfBallots")]
    public string TotalCountOfBallotsFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of votes gained from blank rows from lists/ballots without a source list / party.
    /// </summary>
    [XmlIgnore]
    public int TotalCountOfBlankRowsOnListsWithoutParty { get; set; }

    [XmlElement("TotalCountOfBlankRowsOnListsWithoutParty")]
    public string TotalCountOfBlankRowsOnListsWithoutPartyFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total count of lists with a party (modified + unmodified).
    /// </summary>
    [XmlIgnore]
    public int TotalCountOfListsWithParty { get; set; }

    [XmlElement("TotalCountOfListsWithParty")]
    public string TotalCountOfListsWithPartyFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total count of lists (without and with a party).
    /// </summary>
    [XmlIgnore]
    public int TotalCountOfLists { get; set; }

    [XmlElement("TotalCountOfLists")]
    public string TotalCountOfListsFormatted { get; set; } = string.Empty;

    public override void FormatNumbers(bool useLegacyFormat)
    {
        base.FormatNumbers(useLegacyFormat);

        TotalCountOfUnmodifiedListsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfUnmodifiedLists, useLegacyFormat);
        TotalCountOfModifiedListsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfModifiedLists, useLegacyFormat);
        TotalCountOfListsWithoutPartyFormatted = NumberFormatUtil.FormatInteger(TotalCountOfListsWithoutParty, useLegacyFormat);
        TotalCountOfBallotsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfBallots, useLegacyFormat);
        TotalCountOfBlankRowsOnListsWithoutPartyFormatted = NumberFormatUtil.FormatInteger(TotalCountOfBlankRowsOnListsWithoutParty, useLegacyFormat);
        TotalCountOfListsWithPartyFormatted = NumberFormatUtil.FormatInteger(TotalCountOfListsWithParty, useLegacyFormat);
        TotalCountOfListsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfLists, useLegacyFormat);

        foreach (var listResult in ListResults)
        {
            listResult.FormatNumbers(useLegacyFormat);
        }

        CountOfVoters?.FormatNumbers(useLegacyFormat);
        TotalListResult?.FormatNumbers(useLegacyFormat);
        TotalListResultInclWithoutParty?.FormatNumbers(useLegacyFormat);
        EVotingSubTotal?.FormatNumbers(useLegacyFormat);
        ConventionalSubTotal?.FormatNumbers(useLegacyFormat);
    }
}
