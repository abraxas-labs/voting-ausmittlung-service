// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfDoubleProportionalResultColumn
{
    public PdfProportionalElectionUnionList? UnionList { get; set; }

    public PdfProportionalElectionList? List { get; set; }

    public int VoteCount { get; set; }

    public bool CantonalQuorumReached { get; set; }

    [XmlElement("AnyQuorumReached")]
    public bool AnyRequiredQuorumReached { get; set; }

    public decimal VoterNumber { get; set; }

    [XmlElement("SuperApportionmentNumberOfMandatesUnrounded")]
    public decimal SuperApportionmentQuotient { get; set; }

    public int SuperApportionmentNumberOfMandates { get; set; }

    public int SubApportionmentNumberOfMandates { get; set; }

    public decimal Divisor { get; set; }

    [XmlElement("Cell")]
    public List<PdfDoubleProportionalResultCell>? Cells { get; set; } = new();
}
