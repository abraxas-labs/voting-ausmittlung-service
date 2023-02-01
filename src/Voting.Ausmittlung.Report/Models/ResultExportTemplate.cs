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
    public ResultExportTemplate(
        TemplateModel template,
        IReadOnlyCollection<PoliticalBusiness>? politicalBusinesses = null,
        string? description = null,
        DomainOfInfluenceType? doiType = null,
        Guid? countingCircleId = null,
        PoliticalBusinessUnion? politicalBusinessUnion = null)
    {
        Key = template.Key;
        Description = description ?? template.Description;
        EntityDescription = BuildEntityDescription(politicalBusinessUnion, politicalBusinesses) ?? string.Empty;
        Format = template.Format;
        PoliticalBusinessUnionId = politicalBusinessUnion?.Id;
        EntityType = template.EntityType;
        ResultType = template.ResultType;
        DomainOfInfluenceType = doiType ?? (DomainOfInfluenceType?)template.DomainOfInfluenceType;
        CountingCircleId = countingCircleId;
        PoliticalBusinessIds = politicalBusinesses?.Select(pb => pb.Id).ToHashSet() ?? new HashSet<Guid>();
    }

    public string Key { get; }

    public string Description { get; }

    /// <summary>
    /// Gets the description of the entity (eg. political business, political business union, ...)
    /// </summary>
    public string EntityDescription { get; }

    public Guid? PoliticalBusinessUnionId { get; }

    public EntityType EntityType { get; }

    public ResultType? ResultType { get; }

    public Guid? CountingCircleId { get; }

    public ExportFileFormat Format { get; }

    public DomainOfInfluenceType? DomainOfInfluenceType { get; }

    public IReadOnlySet<Guid> PoliticalBusinessIds { get; }

    private static string? BuildEntityDescription(PoliticalBusinessUnion? politicalBusinessUnion, IReadOnlyCollection<PoliticalBusiness>? politicalBusinesses)
    {
        if (politicalBusinessUnion != null)
        {
            return politicalBusinessUnion.Description;
        }

        if (politicalBusinesses == null)
        {
            return null;
        }

        return string.Join(", ", politicalBusinesses.Select(pb => pb.ShortDescription));
    }
}
