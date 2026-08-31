// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionUnionListEndResult
{
    public PdfProportionalElectionUnionList? UnionList { get; set; }

    public int NumberOfMandates { get; set; }

    [XmlIgnore]
    public int ListVotesCount { get; set; }

    [XmlElement("ListVotesCount")]
    public string ListVotesCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int BlankRowsCount { get; set; }

    [XmlElement("BlankRowsCount")]
    public string BlankRowsCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalVoteCount { get; set; }

    [XmlElement("TotalVoteCount")]
    public string TotalVoteCountFormatted { get; set; } = string.Empty;

    public void FormatNumbers(bool useLegacyFormat)
    {
        ListVotesCountFormatted = NumberFormatUtil.FormatInteger(ListVotesCount, useLegacyFormat);
        BlankRowsCountFormatted = NumberFormatUtil.FormatInteger(BlankRowsCount, useLegacyFormat);
        TotalVoteCountFormatted = NumberFormatUtil.FormatInteger(TotalVoteCount, useLegacyFormat);
    }
}
