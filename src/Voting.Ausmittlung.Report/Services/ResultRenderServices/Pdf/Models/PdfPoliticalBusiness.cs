// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfPoliticalBusiness
{
    public string PoliticalBusinessNumber { get; set; } = string.Empty;

    public string OfficialDescription { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public PdfDomainOfInfluence? DomainOfInfluence { get; set; }
}
