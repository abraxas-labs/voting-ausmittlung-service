// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionResult : PdfCountingCircleResult
{
    public int IndividualVoteCount { get; set; }

    public int EmptyVoteCount { get; set; }

    public int InvalidVoteCount { get; set; }

    public int TotalEmptyAndInvalidVoteCount { get; set; }

    public int TotalCandidateVoteCountExclIndividual { get; set; }

    public int TotalCandidateVoteCountInclIndividual { get; set; }

    public int TotalVoteCount { get; set; }

    [XmlElement("MajorityElectionCandidateResult")]
    public List<PdfMajorityElectionCandidateResult>? CandidateResults { get; set; }

    public PdfPoliticalBusinessCountOfVoters? CountOfVoters { get; set; }

    public PdfMajorityElectionResultSubTotal? EVotingSubTotal { get; set; }

    public PdfMajorityElectionResultSubTotal? ConventionalSubTotal { get; set; }
}
