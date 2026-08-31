// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionListUnionEndResultEntry
{
    public PdfProportionalElectionSimpleList? List { get; set; }

    [XmlIgnore]
    public int VoteCount { get; set; }

    [XmlElement("VoteCount")]
    public string VoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public List<int> GroupVoteCounts { get; set; }
        = new List<int>();

    [XmlElement("GroupVoteCount")]
    public List<string> GroupVoteCountsFormatted { get; set; } = [];

    public void FormatNumbers(bool useLegacyFormat)
    {
        VoteCountFormatted = NumberFormatUtil.FormatInteger(VoteCount, useLegacyFormat);

        GroupVoteCountsFormatted.Clear();
        foreach (var v in GroupVoteCounts)
        {
            GroupVoteCountsFormatted.Add(NumberFormatUtil.FormatInteger(v, useLegacyFormat));
        }
    }
}
