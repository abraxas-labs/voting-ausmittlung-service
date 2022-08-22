// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Controllers.Models;

public class GenerateResultExportRequest
{
    public string Key { get; set; } = string.Empty;

    public List<Guid> PoliticalBusinessIds { get; set; } = new();

    public Guid? CountingCircleId { get; set; }

    public Guid? PoliticalBusinessUnionId { get; set; }

    public DomainOfInfluenceType? DomainOfInfluenceType { get; set; }
}
