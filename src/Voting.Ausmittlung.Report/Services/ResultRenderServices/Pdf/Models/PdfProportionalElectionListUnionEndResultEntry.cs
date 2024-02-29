// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionListUnionEndResultEntry
{
    public PdfProportionalElectionSimpleList? List { get; set; }

    public int VoteCount { get; set; }

    [XmlElement("GroupVoteCount")]
    public List<int> GroupVoteCounts { get; set; }
        = new List<int>();
}
