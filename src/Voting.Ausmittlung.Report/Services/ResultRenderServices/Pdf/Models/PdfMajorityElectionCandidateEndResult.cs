// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionCandidateEndResult : PdfElectionCandidateEndResult
{
    public PdfMajorityElectionCandidate? Candidate { get; set; }

    public MajorityElectionCandidateEndResultState State { get; set; }

    [XmlIgnore]
    public int ConventionalVoteCount { get; set; }

    [XmlElement("ConventionalVoteCount")]
    public string ConventionalVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EVotingVoteCount { get; set; }

    [XmlElement("EVotingVoteCount")]
    public string EVotingVoteCountFormatted { get; set; } = string.Empty;

    public override void FormatNumbers(bool useLegacyFormat)
    {
        base.FormatNumbers(useLegacyFormat);
        ConventionalVoteCountFormatted = NumberFormatUtil.FormatInteger(ConventionalVoteCount, useLegacyFormat);
        EVotingVoteCountFormatted = NumberFormatUtil.FormatInteger(EVotingVoteCount, useLegacyFormat);
    }
}
