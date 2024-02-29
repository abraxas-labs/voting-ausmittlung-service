// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionCandidateVoteSourceResult
{
    // this is not needed and should be omitted (as of the dmdoc team)
    // since these xml's are already huge and the vote sources are all in the parent container as well (for the sums)
    [XmlIgnore]
    public PdfProportionalElectionSimpleList? List { get; set; }

    [XmlText]
    public int VoteCount { get; set; }
}
