// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Lib.VotingExports.Models;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices;

public record ReportRenderContext
{
    private Guid? _singlePoliticalBusinessId;

    internal ReportRenderContext(Guid contestId, TemplateModel template)
    {
        ContestId = contestId;
        Template = template;
    }

    public Guid ContestId { get; }

    public TemplateModel Template { get; }

    public IReadOnlyCollection<Guid> PoliticalBusinessIds { get; init; } = Array.Empty<Guid>();

    public Guid? BasisCountingCircleId { get; init; }

    public Guid? PoliticalBusinessUnionId { get; set; }

    public DomainOfInfluenceType DomainOfInfluenceType { get; init; }

    public Guid PoliticalBusinessId => _singlePoliticalBusinessId ??= PoliticalBusinessIds.Single(); // count is already validated

    public Guid? PoliticalBusinessResultBundleId { get; init; }

    public IRendererService? RendererService { get; set; }
}
