// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

[XmlRoot("Voting")]
public class PdfPoliticalBusinessResultBundleReview
{
    public string TemplateKey { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    public PdfCountingCircle? CountingCircle { get; set; }

    public PdfPoliticalBusiness? PoliticalBusiness { get; set; }

    public PdfMajorityElectionResultBundle? MajorityElectionResultBundle { get; set; }

    public PdfProportionalElectionResultBundle? ProportionalElectionResultBundle { get; set; }

    public PdfVoteResultBundle? VoteResultBundle { get; set; }
}
