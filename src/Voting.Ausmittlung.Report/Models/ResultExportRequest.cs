// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.VotingExports.Models;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Models;

public class ResultExportRequest
{
    public List<Guid> PoliticalBusinessIds { get; set; } = new();

    public Guid? CountingCircleId { get; set; }

    public Guid? PoliticalBusinessUnionId { get; set; }

    public DomainOfInfluenceType? DomainOfInfluenceType { get; set; }

    public TemplateModel Template { get; set; } = null!;
}
