// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionResultBallot
{
    public int Number { get; set; }

    public int EmptyVoteCount { get; set; }

    [XmlElement("ProportionalElectionCandidate")]
    public List<PdfProportionalElectionResultBallotCandidate> BallotCandidates { get; set; } = new();

    public bool AllOriginalCandidatesRemovedFromList { get; set; }
}
