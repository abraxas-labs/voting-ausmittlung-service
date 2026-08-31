// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionUnionEndResult
{
    public PdfPoliticalBusinessCountOfVoters? CountOfVoters { get; set; }

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

    [XmlIgnore]
    public int TotalCountOfBlankRowsOnListsWithoutParty { get; set; }

    [XmlElement("TotalCountOfBlankRowsOnListsWithoutParty")]
    public string TotalCountOfBlankRowsOnListsWithoutPartyFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalVoteCountInclWithoutParty { get; set; }

    [XmlElement("TotalVoteCountInclWithoutParty")]
    public string TotalVoteCountInclWithoutPartyFormatted { get; set; } = string.Empty;

    [XmlElement("ProportionalElectionUnionListEndResult")]
    public List<PdfProportionalElectionUnionListEndResult>? UnionListEndResults { get; set; }

    public void FormatNumbers(bool useLegacyFormat)
    {
        ListVotesCountFormatted = NumberFormatUtil.FormatInteger(ListVotesCount, useLegacyFormat);
        BlankRowsCountFormatted = NumberFormatUtil.FormatInteger(BlankRowsCount, useLegacyFormat);
        TotalVoteCountFormatted = NumberFormatUtil.FormatInteger(TotalVoteCount, useLegacyFormat);
        TotalCountOfBlankRowsOnListsWithoutPartyFormatted = NumberFormatUtil.FormatInteger(TotalCountOfBlankRowsOnListsWithoutParty, useLegacyFormat);
        TotalVoteCountInclWithoutPartyFormatted = NumberFormatUtil.FormatInteger(TotalVoteCountInclWithoutParty, useLegacyFormat);

        CountOfVoters?.FormatNumbers(useLegacyFormat);

        foreach (var result in UnionListEndResults ?? [])
        {
            result.FormatNumbers(useLegacyFormat);
        }
    }
}
