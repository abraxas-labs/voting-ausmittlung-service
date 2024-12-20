// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public abstract class DomainOfInfluenceResult
{
    public DomainOfInfluence? DomainOfInfluence { get; set; }

    public PoliticalBusinessCountOfVoters CountOfVoters { get; } = new PoliticalBusinessCountOfVoters();

    public ContestDomainOfInfluenceDetails ContestDomainOfInfluenceDetails { get; } = new ContestDomainOfInfluenceDetails();
}
