// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Controllers.Models.Export;

public class DownloadEch0252ExportRequest
{
    public DateOnly? PollingDateFrom { get; set; }

    public DateOnly? PollingDateTo { get; set; }

    public DateOnly? PollingDate { get; set; }

    public int? PollingSinceDays { get; set; }

    public List<Guid>? VotingIdentifications { get; set; }

    public List<CountingCircleResultState>? CountingStates { get; set; }

    public List<PoliticalBusinessType>? PoliticalBusinessTypes { get; set; }

    public bool InformationOnly { get; set; }
}
