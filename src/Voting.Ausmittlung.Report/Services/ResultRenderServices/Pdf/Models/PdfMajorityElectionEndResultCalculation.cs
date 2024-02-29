// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionEndResultCalculation
{
    public int? DecisiveVoteCount { get; set; }

    public decimal? AbsoluteMajorityThreshold { get; set; }

    public int? AbsoluteMajority { get; set; }

    public bool ShouldSerializeDecisiveVoteCount() => DecisiveVoteCount.HasValue;

    public bool ShouldSerializeAbsoluteMajorityThreshold() => AbsoluteMajorityThreshold.HasValue;

    public bool ShouldSerializeAbsoluteMajority() => AbsoluteMajority.HasValue;
}
