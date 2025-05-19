// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionEndResult : PdfPoliticalBusinessEndResult
{
    public PdfPoliticalBusinessCountOfVoters CountOfVoters { get; set; }
        = new PdfPoliticalBusinessCountOfVoters();

    public int IndividualVoteCount { get; set; }

    public int EmptyVoteCount { get; set; }

    public int InvalidVoteCount { get; set; }

    public int TotalEmptyAndInvalidVoteCount { get; set; }

    public int TotalCandidateVoteCountExclIndividual { get; set; }

    public int TotalCandidateVoteCountInclIndividual { get; set; }

    public int TotalVoteCount { get; set; }

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
}
