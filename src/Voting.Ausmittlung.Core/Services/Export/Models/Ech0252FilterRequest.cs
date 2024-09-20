// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Export.Models;

public class Ech0252FilterRequest
{
    public DateTime? PollingDateFrom { get; set; }

    public DateTime? PollingDateTo { get; set; }

    public DateTime? PollingDate { get; set; }

    public int? PollingSinceDays { get; set; }

    public List<Guid> VotingIdentifications { get; set; } = new();

    public List<CountingCircleResultState>? CountingStates { get; set; } = new();

    public List<PoliticalBusinessType>? PoliticalBusinessTypes { get; set; } = new();

    public bool InformationOnly { get; set; }
}
