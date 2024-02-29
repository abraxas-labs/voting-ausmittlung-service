// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public abstract class PdfDomainOfInfluenceResult
{
    public PdfDomainOfInfluence? DomainOfInfluence { get; set; }

    public PdfPoliticalBusinessCountOfVoters? CountOfVoters { get; set; }

    public PdfContestDomainOfInfluenceDetails? ContestDomainOfInfluenceDetails { get; set; }
}
