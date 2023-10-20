// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionCandidateEndResult : PdfElectionCandidateEndResult
{
    public PdfProportionalElectionCandidate? Candidate { get; set; }

    public ProportionalElectionCandidateEndResultState State { get; set; }

    public PdfProportionalElectionCandidateResultSubTotal? ConventionalSubTotal { get; set; }

    public PdfProportionalElectionCandidateResultSubTotal? EVotingSubTotal { get; set; }

    /// <summary>
    /// Gets or sets the count of votes gained from unmodified lists.
    /// </summary>
    public int UnmodifiedListVotesCount { get; set; }

    /// <summary>
    /// Gets or sets the count of votes gained from modified lists.
    /// </summary>
    public int ModifiedListVotesCount { get; set; }

    /// <summary>
    /// Gets or sets the count of candidate votes gained from "other" lists (panaschieren).
    /// </summary>
    public int CountOfVotesOnOtherLists { get; set; }

    /// <summary>
    /// Gets or sets the count of votes gained from accumulating this candidate (kumulieren).
    /// </summary>
    public int CountOfVotesFromAccumulations { get; set; }

    [XmlElement("VoteSourceVoteCount")]
    public List<PdfProportionalElectionCandidateVoteSourceResult>? VoteSources { get; set; }
}
