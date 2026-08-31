// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionResult : PdfCountingCircleResult
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

    [XmlIgnore]
    public int TotalEmptyAndInvalidVoteCount { get; set; }

    [XmlElement("TotalEmptyAndInvalidVoteCount")]
    public string TotalEmptyAndInvalidVoteCountFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCandidateVoteCountExclIndividual { get; set; }

    [XmlElement("TotalCandidateVoteCountExclIndividual")]
    public string TotalCandidateVoteCountExclIndividualFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalCandidateVoteCountInclIndividual { get; set; }

    [XmlElement("TotalCandidateVoteCountInclIndividual")]
    public string TotalCandidateVoteCountInclIndividualFormatted { get; set; } = string.Empty;

    [XmlIgnore]
    public int TotalVoteCount { get; set; }

    [XmlElement("TotalVoteCount")]
    public string TotalVoteCountFormatted { get; set; } = string.Empty;

    [XmlElement("MajorityElectionCandidateResult")]
    public List<PdfMajorityElectionCandidateResult>? CandidateResults { get; set; }

    public PdfPoliticalBusinessCountOfVoters? CountOfVoters { get; set; }

    public PdfMajorityElectionResultSubTotal? EVotingSubTotal { get; set; }

    public PdfMajorityElectionResultSubTotal? ConventionalSubTotal { get; set; }

    public override void FormatNumbers(bool useLegacyFormat)
    {
        base.FormatNumbers(useLegacyFormat);
        IndividualVoteCountFormatted = NumberFormatUtil.FormatInteger(IndividualVoteCount, useLegacyFormat);
        EmptyVoteCountFormatted = NumberFormatUtil.FormatInteger(EmptyVoteCount, useLegacyFormat);
        InvalidVoteCountFormatted = NumberFormatUtil.FormatInteger(InvalidVoteCount, useLegacyFormat);
        TotalEmptyAndInvalidVoteCountFormatted = NumberFormatUtil.FormatInteger(TotalEmptyAndInvalidVoteCount, useLegacyFormat);
        TotalCandidateVoteCountExclIndividualFormatted = NumberFormatUtil.FormatInteger(TotalCandidateVoteCountExclIndividual, useLegacyFormat);
        TotalCandidateVoteCountInclIndividualFormatted = NumberFormatUtil.FormatInteger(TotalCandidateVoteCountInclIndividual, useLegacyFormat);
        TotalVoteCountFormatted = NumberFormatUtil.FormatInteger(TotalVoteCount, useLegacyFormat);

        CountOfVoters?.FormatNumbers(useLegacyFormat);
        EVotingSubTotal?.FormatNumbers(useLegacyFormat);
        ConventionalSubTotal?.FormatNumbers(useLegacyFormat);

        foreach (var candidateResult in CandidateResults ?? [])
        {
            candidateResult.FormatNumbers(useLegacyFormat);
        }
    }
}
