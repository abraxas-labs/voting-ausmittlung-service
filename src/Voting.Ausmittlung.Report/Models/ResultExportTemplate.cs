// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.VotingExports.Models;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Models;

public class ResultExportTemplate
{
    public ResultExportTemplate(
        TemplateModel template,
        string tenantId,
        Guid? politicalBusinessId = null,
        string? description = null,
        DomainOfInfluenceType? doiType = null,
        Guid? countingCircleId = null,
        IReadOnlyCollection<PoliticalBusiness>? politicalBusinesses = null,
        PoliticalBusinessUnion? politicalBusinessUnion = null,
        Guid? politicalBusinessResultBundleId = null,
        Guid? domainOfInfluenceId = null)
    {
        Description = description ?? template.Description;
        EntityDescription = BuildEntityDescription(politicalBusinessUnion, politicalBusinesses) ?? string.Empty;
        Template = template;
        PoliticalBusinessId = politicalBusinessId;
        PoliticalBusinessUnionId = politicalBusinessUnion?.Id;
        DomainOfInfluenceType = doiType;
        CountingCircleId = countingCircleId;
        PoliticalBusinessIds = politicalBusinesses?.Select(pb => pb.Id).ToHashSet() ?? new HashSet<Guid>();
        PoliticalBusinessResultBundleId = politicalBusinessResultBundleId;
        DomainOfInfluenceId = domainOfInfluenceId;

        ExportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
            Template.Key,
            tenantId,
            CountingCircleId,
            PoliticalBusinessId,
            PoliticalBusinessUnionId,
            DomainOfInfluenceType ?? Data.Models.DomainOfInfluenceType.Unspecified,
            PoliticalBusinessResultBundleId,
            DomainOfInfluenceId);
    }

    public TemplateModel Template { get; }

    public string Description { get; }

    /// <summary>
    /// Gets the description of the entity (eg. political business, political business union, ...)
    /// </summary>
    public string EntityDescription { get; }

    public Guid? PoliticalBusinessId { get; }

    public Guid? PoliticalBusinessUnionId { get; }

    public Guid? CountingCircleId { get; }

    public Guid? PoliticalBusinessResultBundleId { get; }

    public Guid? DomainOfInfluenceId { get; }

    public DomainOfInfluenceType? DomainOfInfluenceType { get; }

    /// <summary>
    /// Gets or sets the political business IDs. This is transient information and should not be supplied by users.
    /// </summary>
    public IReadOnlyCollection<Guid> PoliticalBusinessIds { get; set; }

    /// <summary>
    /// Gets an ID which uniquely identifies this export template.
    /// </summary>
    public Guid ExportTemplateId { get; }

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

        return string.Join(" | ", politicalBusinesses.Select(pb => pb.ShortDescription));
    }
}
