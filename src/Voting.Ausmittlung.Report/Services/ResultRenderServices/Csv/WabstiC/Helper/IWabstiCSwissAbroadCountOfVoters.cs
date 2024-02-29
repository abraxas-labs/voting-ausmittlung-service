// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;

internal interface IWabstiCSwissAbroadCountOfVoters
{
    Guid CountingCircleId { get; }

    int CountOfVotersTotalSwissAbroad { set; }
}
