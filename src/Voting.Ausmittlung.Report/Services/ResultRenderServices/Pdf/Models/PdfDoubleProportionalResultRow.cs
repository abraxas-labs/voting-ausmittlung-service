// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfDoubleProportionalResultRow
{
    public PdfProportionalElection? ProportionalElection { get; set; }

    public int VoteCount { get; set; }

    public int VoterNumber { get; set; }

    public int Quorum { get; set; }

    public decimal Divisor { get; set; }

    public int NumberOfMandates { get; set; }

    public int SubApportionmentNumberOfMandates { get; set; }

    [XmlElement("Cell")]
    public List<PdfDoubleProportionalResultCell>? Cells { get; set; }
}
