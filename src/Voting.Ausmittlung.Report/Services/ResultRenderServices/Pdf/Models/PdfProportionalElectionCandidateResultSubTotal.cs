// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionCandidateResultSubTotal
{
    public int UnmodifiedListVotesCount { get; set; }

    public int ModifiedListVotesCount { get; set; }

    public int CountOfVotesOnOtherLists { get; set; }

    public int CountOfVotesFromAccumulations { get; set; }

    [XmlElement("TotalCountOfVotes")]
    public int VoteCount { get; set; }
}
