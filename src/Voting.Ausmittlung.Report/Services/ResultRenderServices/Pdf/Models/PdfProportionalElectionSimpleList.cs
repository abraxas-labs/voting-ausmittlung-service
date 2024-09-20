// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionSimpleList
{
    [XmlIgnore]
    public Guid Id { get; set; }

    [XmlIgnore]
    public int Position { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;
}
