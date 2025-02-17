// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfCountOfVotersInformationSubTotal
{
    public SexType Sex { get; set; }

    public int CountOfVoters { get; set; }

    public VoterType VoterType { get; set; }
}
