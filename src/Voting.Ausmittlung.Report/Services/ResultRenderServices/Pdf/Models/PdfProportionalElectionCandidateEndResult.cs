// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Utils;

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
    [XmlIgnore]
    public int UnmodifiedListVotesCount { get; set; }

    [XmlElement("UnmodifiedListVotesCount")]
    public string UnmodifiedListVotesCountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of votes gained from modified lists.
    /// </summary>
    [XmlIgnore]
    public int ModifiedListVotesCount { get; set; }

    [XmlElement("ModifiedListVotesCount")]
    public string ModifiedListVotesCountFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of candidate votes gained from "other" lists (panaschieren).
    /// </summary>
    [XmlIgnore]
    public int CountOfVotesOnOtherLists { get; set; }

    [XmlElement("CountOfVotesOnOtherLists")]
    public string CountOfVotesOnOtherListsFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of votes gained from accumulating this candidate (kumulieren).
    /// </summary>
    [XmlIgnore]
    public int CountOfVotesFromAccumulations { get; set; }

    [XmlElement("CountOfVotesFromAccumulations")]
    public string CountOfVotesFromAccumulationsFormatted { get; set; } = string.Empty;

    [XmlElement("VoteSourceVoteCount")]
    public List<PdfProportionalElectionCandidateVoteSourceResult>? VoteSources { get; set; }

    public override void FormatNumbers(bool useLegacyFormat)
    {
        base.FormatNumbers(useLegacyFormat);

        UnmodifiedListVotesCountFormatted = NumberFormatUtil.FormatInteger(UnmodifiedListVotesCount, useLegacyFormat);
        ModifiedListVotesCountFormatted = NumberFormatUtil.FormatInteger(ModifiedListVotesCount, useLegacyFormat);
        CountOfVotesOnOtherListsFormatted = NumberFormatUtil.FormatInteger(CountOfVotesOnOtherLists, useLegacyFormat);
        CountOfVotesFromAccumulationsFormatted = NumberFormatUtil.FormatInteger(CountOfVotesFromAccumulations, useLegacyFormat);

        ConventionalSubTotal?.FormatNumbers(useLegacyFormat);
        EVotingSubTotal?.FormatNumbers(useLegacyFormat);

        foreach (var voteSource in VoteSources ?? [])
        {
            voteSource.FormatNumbers(useLegacyFormat);
        }
    }
}
