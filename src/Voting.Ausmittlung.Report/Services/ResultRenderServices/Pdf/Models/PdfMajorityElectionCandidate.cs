// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionCandidate : PdfElectionCandidate
{
    public string Party { get; set; } = string.Empty;
}
