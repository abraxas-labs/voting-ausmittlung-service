// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionResultBundle : PdfPoliticalBusinessBundle
{
    public int CountOfBallots { get; set; }

    [XmlElement("MajorityElectionResultBallot")]
    public List<PdfMajorityElectionResultBallot> Ballots { get; set; } = new();

    public PdfUser? CreatedBy { get; set; }
}
