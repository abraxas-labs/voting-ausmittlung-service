// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public abstract class PdfPoliticalBusinessBundle
{
    public int Number { get; set; }

    public PdfUser? CreatedBy { get; set; }
}
