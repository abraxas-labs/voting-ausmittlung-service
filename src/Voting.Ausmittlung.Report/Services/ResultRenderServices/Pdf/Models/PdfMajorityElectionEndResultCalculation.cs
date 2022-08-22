// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionEndResultCalculation
{
    public int? AbsoluteMajority { get; set; }

    public bool ShouldSerializeAbsoluteMajority() => AbsoluteMajority.HasValue;
}
