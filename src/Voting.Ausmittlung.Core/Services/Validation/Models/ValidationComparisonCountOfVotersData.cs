// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Services.Validation.Models;

public class ValidationComparisonCountOfVotersData
{
    public decimal ThresholdPercent { get; set; }

    public decimal DeviationPercent { get; set; }

    public int Deviation { get; set; }

    public DateTime PreviousDate { get; set; }

    public int PreviousCount { get; set; }

    public int CurrentCount { get; set; }
}
