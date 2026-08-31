// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionDomainOfInfluenceResult : PdfDomainOfInfluenceResult
{
    [XmlIgnore]
    public int IndividualVoteCount { get; set; }

    [XmlElement("IndividualVoteCount")]
    public string IndividualVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int EmptyVoteCount { get; set; }

    [XmlElement("EmptyVoteCount")]
    public string EmptyVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int InvalidVoteCount { get; set; }

    [XmlElement("InvalidVoteCount")]
    public string InvalidVoteCountFormatted { get; set; } = string.Empty;

    [XmlElement("MajorityElectionCandidateResult")]
    public List<PdfMajorityElectionCandidateResult>? CandidateResults { get; set; }

    [XmlElement("MajorityElectionResult")]
    public List<PdfMajorityElectionResult>? Results { get; set; }

    public override void FormatNumbers(bool useLegacyFormat)
    {
        base.FormatNumbers(useLegacyFormat);
        IndividualVoteCountFormatted = NumberFormatUtil.FormatInteger(IndividualVoteCount, useLegacyFormat);
        EmptyVoteCountFormatted = NumberFormatUtil.FormatInteger(EmptyVoteCount, useLegacyFormat);
        InvalidVoteCountFormatted = NumberFormatUtil.FormatInteger(InvalidVoteCount, useLegacyFormat);

        foreach (var result in Results ?? [])
        {
            result.FormatNumbers(useLegacyFormat);
        }

        foreach (var candidateResult in CandidateResults ?? [])
        {
            candidateResult.FormatNumbers(useLegacyFormat);
        }
    }
}
