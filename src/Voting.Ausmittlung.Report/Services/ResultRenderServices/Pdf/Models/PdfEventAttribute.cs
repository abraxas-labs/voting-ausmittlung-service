// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfEventAttribute
{
    [XmlElement("AttributeName")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("AttributeValue")]
    public string? Value { get; set; }

    [XmlElement("EventAttribute")]
    public List<PdfEventAttribute>? Children { get; set; }
}
