// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfProportionalElectionList : PdfProportionalElectionSimpleList
{
    public string Description { get; set; } = string.Empty;
}
