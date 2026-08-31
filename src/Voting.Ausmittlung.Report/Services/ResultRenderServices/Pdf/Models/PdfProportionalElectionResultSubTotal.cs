// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionResultSubTotal
{
    [XmlIgnore]
    public int TotalCountOfUnmodifiedLists { get; set; }

    [XmlElement("TotalCountOfUnmodifiedLists")]
    public string TotalCountOfUnmodifiedListsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfModifiedLists { get; set; }

    [XmlElement("TotalCountOfModifiedLists")]
    public string TotalCountOfModifiedListsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfListsWithoutParty { get; set; }

    [XmlElement("TotalCountOfListsWithoutParty")]
    public string TotalCountOfListsWithoutPartyFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfBallots { get; set; }

    [XmlElement("TotalCountOfBallots")]
    public string TotalCountOfBallotsFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfBlankRowsOnListsWithoutParty { get; set; }

    [XmlElement("TotalCountOfBlankRowsOnListsWithoutParty")]
    public string TotalCountOfBlankRowsOnListsWithoutPartyFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfListsWithParty { get; set; }

    [XmlElement("TotalCountOfListsWithParty")]
    public string TotalCountOfListsWithPartyFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCountOfLists { get; set; }

    [XmlElement("TotalCountOfLists")]
    public string TotalCountOfListsFormatted { get; set; } = string.Empty;

    public void FormatNumbers(bool useLegacyFormat)
    {
        TotalCountOfUnmodifiedListsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfUnmodifiedLists, useLegacyFormat);
        TotalCountOfModifiedListsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfModifiedLists, useLegacyFormat);
        TotalCountOfListsWithoutPartyFormatted = NumberFormatUtil.FormatInteger(TotalCountOfListsWithoutParty, useLegacyFormat);
        TotalCountOfBallotsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfBallots, useLegacyFormat);
        TotalCountOfBlankRowsOnListsWithoutPartyFormatted = NumberFormatUtil.FormatInteger(TotalCountOfBlankRowsOnListsWithoutParty, useLegacyFormat);
        TotalCountOfListsWithPartyFormatted = NumberFormatUtil.FormatInteger(TotalCountOfListsWithParty, useLegacyFormat);
        TotalCountOfListsFormatted = NumberFormatUtil.FormatInteger(TotalCountOfLists, useLegacyFormat);
    }
}
