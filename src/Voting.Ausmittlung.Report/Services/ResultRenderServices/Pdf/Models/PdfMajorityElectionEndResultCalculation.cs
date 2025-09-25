// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionEndResultCalculation
{
    private decimal? _absoluteMajorityThreshold;

    public int? DecisiveVoteCount { get; set; }

    public decimal? AbsoluteMajorityThreshold
    {
        get
        {
            return _absoluteMajorityThreshold;
        }

        set
        {
            _absoluteMajorityThreshold = value.HasValue
                ? decimal.Round(value.Value, 1, MidpointRounding.AwayFromZero)
                : null;
        }
    }

    public int? AbsoluteMajority { get; set; }

    public bool ShouldSerializeDecisiveVoteCount() => DecisiveVoteCount.HasValue;

    public bool ShouldSerializeAbsoluteMajorityThreshold() => AbsoluteMajorityThreshold.HasValue;

    public bool ShouldSerializeAbsoluteMajority() => AbsoluteMajority.HasValue;
}
