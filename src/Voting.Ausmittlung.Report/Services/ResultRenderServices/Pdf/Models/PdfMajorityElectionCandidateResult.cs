// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionCandidateResult
{
    public PdfMajorityElectionCandidate? Candidate { get; set; }

    [XmlIgnore]
    public int ConventionalVoteCount { get; set; }

    [XmlElement("ConventionalVoteCount")]
    public string ConventionalVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EVotingExclWriteInsVoteCount { get; set; }

    [XmlElement("EVotingExclWriteInsVoteCount")]
    public string EVotingExclWriteInsVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EVotingWriteInsVoteCount { get; set; }

    [XmlElement("EVotingWriteInsVoteCount")]
    public string EVotingWriteInsVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EVotingInclWriteInsVoteCount { get; set; }

    [XmlElement("EVotingInclWriteInsVoteCount")]
    public string EVotingInclWriteInsVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int VoteCount { get; set; }

    [XmlElement("VoteCount")]
    public string VoteCountFormatted { get; set; } = string.Empty;

    public void FormatNumbers(bool useLegacyFormat)
    {
        ConventionalVoteCountFormatted = NumberFormatUtil.FormatInteger(ConventionalVoteCount, useLegacyFormat);
        EVotingExclWriteInsVoteCountFormatted = NumberFormatUtil.FormatInteger(EVotingExclWriteInsVoteCount, useLegacyFormat);
        EVotingWriteInsVoteCountFormatted = NumberFormatUtil.FormatInteger(EVotingWriteInsVoteCount, useLegacyFormat);
        EVotingInclWriteInsVoteCountFormatted = NumberFormatUtil.FormatInteger(EVotingInclWriteInsVoteCount, useLegacyFormat);
        VoteCountFormatted = NumberFormatUtil.FormatInteger(VoteCount, useLegacyFormat);
    }
}
