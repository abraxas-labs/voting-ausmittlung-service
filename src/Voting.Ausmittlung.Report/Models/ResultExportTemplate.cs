// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.VotingExports.Models;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Models;

public class ResultExportTemplate
{
    // only display a certain amount of political business numbers in frontend,
    // so that the user in the export has an approximate idea of which political businesses are included
    private const int MaxPoliticalBusinessNumbersCount = 3;

    public ResultExportTemplate(
        TemplateModel template,
        IEnumerable<PoliticalBusiness> politicalBusinesses,
        string? description = null,
        DomainOfInfluenceType? doiType = null,
        Guid? countingCircleId = null,
        Guid? politicalBusinessUnionId = null)
    {
        Key = template.Key;
        Description = description ?? template.Description;
        Format = template.Format;
        PoliticalBusinesses = politicalBusinesses;
        PoliticalBusinessUnionId = politicalBusinessUnionId;
        EntityType = template.EntityType;
        DomainOfInfluenceType = doiType ?? (DomainOfInfluenceType?)template.DomainOfInfluenceType;
        CountingCircleId = countingCircleId;
        PoliticalBusinessIds = PoliticalBusinesses.Select(pb => pb.Id).ToHashSet();
    }

    public string Key { get; }

    public string Description { get; }

    public IEnumerable<PoliticalBusiness> PoliticalBusinesses { get; }

    public Guid? PoliticalBusinessUnionId { get; }

    public EntityType EntityType { get; }

    public Guid? CountingCircleId { get; }

    public ExportFileFormat Format { get; }

    public DomainOfInfluenceType? DomainOfInfluenceType { get; }

    public IReadOnlySet<Guid> PoliticalBusinessIds { get; }

    public string PoliticalBusinessNumbers =>
        string.Join(" / ", PoliticalBusinesses
            .Take(MaxPoliticalBusinessNumbersCount)
            .Select(pb => pb.PoliticalBusinessNumber));
}
