// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionCandidateResult : IProportionalElectionCandidateResultTotal
{
    [XmlElement("ProportionalElectionCandidate")]
    public PdfProportionalElectionCandidate? Candidate { get; set; }

    [XmlElement("VoteSourceVoteCount")]
    public List<PdfProportionalElectionCandidateVoteSourceResult>? VoteSources { get; set; }

    public PdfProportionalElectionCandidateResultSubTotal? ConventionalSubTotal { get; set; }

    public PdfProportionalElectionCandidateResultSubTotal? EVotingSubTotal { get; set; }

    /// <inheritdoc />
    public int UnmodifiedListVotesCount { get; set; }

    /// <inheritdoc />
    public int ModifiedListVotesCount { get; set; }

    /// <inheritdoc />
    public int CountOfVotesOnOtherLists { get; set; }

    /// <inheritdoc />
    public int CountOfVotesFromAccumulations { get; set; }

    /// <inheritdoc />
    [XmlElement("TotalCountOfVotes")]
    public int VoteCount { get; set; }
}
