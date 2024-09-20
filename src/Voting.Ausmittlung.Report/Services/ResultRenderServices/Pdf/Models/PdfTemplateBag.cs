// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Xml.Serialization;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

[XmlRoot("Voting")]
public class PdfTemplateBag
{
    public string TemplateKey { get; set; } = string.Empty;

    public PdfContest? Contest { get; set; }

    [XmlElement("Vote")]
    public List<PdfVote>? Votes { get; set; }

    [XmlElement("MajorityElection")]
    public List<PdfMajorityElection>? MajorityElections { get; set; }

    [XmlElement("ProportionalElection")]
    public List<PdfProportionalElection>? ProportionalElections { get; set; }

    [XmlElement("ProportionalElectionUnion")]
    public List<PdfProportionalElectionUnion>? ProportionalElectionUnions { get; set; }

    public PdfCountingCircle? CountingCircle { get; set; }

    public PdfDomainOfInfluence? DomainOfInfluence { get; set; }

    /// <summary>
    /// Gets or sets the domain of influence type of this report.
    /// This should only be set if the <see cref="ReportRenderContext.DomainOfInfluenceType"/> was set during the render service call.
    /// This indicates this report targets a specific domain of influence type.
    /// </summary>
    public DomainOfInfluenceType? DomainOfInfluenceType { get; set; }

    public bool ShouldSerializeDomainOfInfluenceType()
        => DomainOfInfluenceType.HasValue;
}
