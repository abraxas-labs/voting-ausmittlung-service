// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfDomainOfInfluence
{
    [XmlIgnore]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NameForProtocol { get; set; } = string.Empty;

    public string ShortName { get; set; } = string.Empty;

    public string AuthorityName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int SortNumber { get; set; }

    public string Bfs { get; set; } = string.Empty;

    public DomainOfInfluenceType Type { get; set; }

    public DomainOfInfluenceCanton Canton { get; set; }

    [XmlElement("ContestDomainOfInfluenceDetails")]
    public PdfContestDomainOfInfluenceDetails? Details { get; set; }
}
