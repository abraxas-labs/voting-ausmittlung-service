// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Report.Utils;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionEndResult : PdfPoliticalBusinessEndResult
{
    public bool IsComplete { get; set; }

    public PdfPoliticalBusinessCountOfVoters CountOfVoters { get; set; }
        = new PdfPoliticalBusinessCountOfVoters();

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

    public PdfMajorityElectionResultSubTotal? EVotingSubTotal { get; set; }

    public PdfMajorityElectionResultSubTotal? ConventionalSubTotal { get; set; }

    [XmlElement("MajorityElectionCandidateEndResult")]
    public List<PdfMajorityElectionCandidateEndResult>? CandidateEndResults { get; set; }

    public PdfMajorityElectionEndResultCalculation? Calculation { get; set; }

    [XmlElement("MajorityElectionCandidateEndResultPending")]
    public List<PdfMajorityElectionCandidateEndResult>? CandidateEndResultsPending { get; set; }

    [XmlElement("MajorityElectionCandidateEndResultAbsoluteMajorityAndElected")]
    public List<PdfMajorityElectionCandidateEndResult>? CandidateEndResultsAbsoluteMajorityAndElected { get; set; }

    [XmlElement("MajorityElectionCandidateEndResultAbsoluteMajorityAndNotElected")]
    public List<PdfMajorityElectionCandidateEndResult>? CandidateEndResultsAbsoluteMajorityAndNotElected { get; set; }

    [XmlElement("MajorityElectionCandidateEndResultNoAbsoluteMajorityAndNotElectedButRankOk")]
    public List<PdfMajorityElectionCandidateEndResult>? CandidateEndResultsNoAbsoluteMajorityAndNotElectedButRankOk { get; set; }

    [XmlElement("MajorityElectionCandidateEndResultElected")]
    public List<PdfMajorityElectionCandidateEndResult>? CandidateEndResultsElected { get; set; }

    [XmlElement("MajorityElectionCandidateEndResultNotElected")]
    public List<PdfMajorityElectionCandidateEndResult>? CandidateEndResultsNotElected { get; set; }

    [XmlElement("MajorityElectionCandidateEndResultNotElectedInPrimaryElectionNotEligible")]
    public List<PdfMajorityElectionCandidateEndResult>? CandidateEndResultsNotElectedInPrimaryElectionNotEligible { get; set; }

    [XmlElement("MajorityElectionCandidateEndResultAbsoluteMajorityAndNotElectedInPrimaryElectionNotEligible")]
    public List<PdfMajorityElectionCandidateEndResult>? CandidateEndResultsAbsoluteMajorityAndNotElectedInPrimaryElectionNotEligible { get; set; }

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

        EVotingSubTotal?.FormatNumbers(useLegacyFormat);
        ConventionalSubTotal?.FormatNumbers(useLegacyFormat);
        CountOfVoters.FormatNumbers(useLegacyFormat);
        Calculation?.FormatNumbers(useLegacyFormat);

        FormatCandidateNumbers(CandidateEndResults, useLegacyFormat);
        FormatCandidateNumbers(CandidateEndResultsPending, useLegacyFormat);
        FormatCandidateNumbers(CandidateEndResultsAbsoluteMajorityAndElected, useLegacyFormat);
        FormatCandidateNumbers(CandidateEndResultsAbsoluteMajorityAndNotElected, useLegacyFormat);
        FormatCandidateNumbers(CandidateEndResultsNoAbsoluteMajorityAndNotElectedButRankOk, useLegacyFormat);
        FormatCandidateNumbers(CandidateEndResultsElected, useLegacyFormat);
        FormatCandidateNumbers(CandidateEndResultsNotElected, useLegacyFormat);
        FormatCandidateNumbers(CandidateEndResultsNotElectedInPrimaryElectionNotEligible, useLegacyFormat);
        FormatCandidateNumbers(CandidateEndResultsAbsoluteMajorityAndNotElectedInPrimaryElectionNotEligible, useLegacyFormat);
    }

    private void FormatCandidateNumbers(List<PdfMajorityElectionCandidateEndResult>? candidateEndResults, bool useLegacyFormat)
    {
        foreach (var candidateEndResult in candidateEndResults ?? [])
        {
            candidateEndResult.FormatNumbers(useLegacyFormat);
        }
    }
}
