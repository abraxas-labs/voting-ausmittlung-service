// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfDoubleProportionalResult
{
    public int NumberOfMandates { get; set; }

    public int VoteCount { get; set; }

    public int CantonalQuorum { get; set; }

    public decimal VoterNumber { get; set; }

    public decimal ElectionKey { get; set; }

    public int SuperApportionmentNumberOfMandates { get; set; }

    public int SubApportionmentNumberOfMandates { get; set; }

    [XmlElement("Row")]
    public List<PdfDoubleProportionalResultRow> Rows { get; set; } = new();

    [XmlElement("Column")]
    public List<PdfDoubleProportionalResultColumn> Columns { get; set; } = new();
}
