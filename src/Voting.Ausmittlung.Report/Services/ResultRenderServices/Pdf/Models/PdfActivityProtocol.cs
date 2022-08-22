// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

[XmlRoot("ActivityProtocol")]
public class PdfActivityProtocol
{
    public string TemplateKey { get; set; } = string.Empty;

    public PdfContest? Contest { get; set; }

    [XmlElement("Event")]
    public List<PdfEvent> Events { get; set; } = new List<PdfEvent>();
}
