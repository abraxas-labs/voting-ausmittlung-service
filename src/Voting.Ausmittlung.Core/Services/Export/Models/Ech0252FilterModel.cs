// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Export.Models;

public class Ech0252FilterModel
{
    public DateTime ContestDateFrom { get; init; }

    public DateTime? ContestDateTo { get; init; }

    public List<Guid> PoliticalBusinessIds { get; init; } = new();

    public List<CountingCircleResultState> CountingCircleResultStates { get; init; } = new();

    public List<PoliticalBusinessType> PoliticalBusinessTypes { get; init; } = new();
}
