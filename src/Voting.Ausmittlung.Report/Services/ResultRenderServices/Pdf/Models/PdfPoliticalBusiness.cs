// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfPoliticalBusiness
{
    [XmlIgnore]
    public Guid Id { get; set; }

    public string PoliticalBusinessNumber { get; set; } = string.Empty;

    public string OfficialDescription { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public PdfDomainOfInfluence? DomainOfInfluence { get; set; }
}
