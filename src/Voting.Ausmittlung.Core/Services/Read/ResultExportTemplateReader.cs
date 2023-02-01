// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using DomainOfInfluenceType = Voting.Lib.VotingExports.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ResultExportTemplateReader
{
    private static readonly IReadOnlySet<string> _templateKeysOnlyWithInvalidVotes = new HashSet<string>
    {
        AusmittlungPdfMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol.Key,
    };

    private readonly ContestReader _contestReader;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepository;
    private readonly PermissionService _permissionService;
    private readonly IMapper _mapper;
    private readonly PublisherConfig _config;

    public ResultExportTemplateReader(
        ContestReader contestReader,
        PermissionService permissionService,
        IDbRepository<DataContext, CountingCircle> countingCircleRepository,
        IMapper mapper,
        PublisherConfig config)
    {
        _contestReader = contestReader;
        _permissionService = permissionService;
        _mapper = mapper;
        _config = config;
        _countingCircleRepository = countingCircleRepository;
    }

    public async Task<ResultExportTemplateContainer> ListTemplates(
        Guid contestId,
        Guid? basisCountingCircleId,
        IReadOnlySet<ExportFileFormat> formats)
    {
        var contest = await _contestReader.Get(contestId);
        CountingCircle? countingCircle = null;

        if (basisCountingCircleId.HasValue)
        {
            await _permissionService.EnsureCanReadBasisCountingCircle(basisCountingCircleId.Value, contestId);
            countingCircle = await _countingCircleRepository.Query()
                .FirstOrDefaultAsync(c => c.BasisCountingCircleId == basisCountingCircleId && c.SnapshotContestId == contestId);
        }

        var templates = await FetchResultExportTemplates(contestId, basisCountingCircleId, formats);
        return new ResultExportTemplateContainer(contest, countingCircle, templates);
    }

    private async Task<IReadOnlyCollection<ResultExportTemplate>> FetchResultExportTemplates(
        Guid contestId,
        Guid? basisCountingCircleId,
        IReadOnlySet<ExportFileFormat> formats)
    {
        if (_config.DisableAllExports)
        {
            return Array.Empty<ResultExportTemplate>();
        }

        IEnumerable<TemplateModel> templates = TemplateRepository.GetByGenerator(VotingApp.VotingAusmittlung);
        templates = FilterFormat(templates, formats);
        templates = FilterDisabledTemplates(templates);
        if (!templates.Any())
        {
            return Array.Empty<ResultExportTemplate>();
        }

        var data = await (basisCountingCircleId.HasValue
            ? LoadExportDataForErfassung(contestId, basisCountingCircleId.Value)
            : LoadExportDataForMonitoring(contestId));
        var resultTemplates = templates.SelectMany(t => BuildResultExportTemplates(t, data));

        return resultTemplates
            .OrderBy(x => x.EntityDescription)
            .ThenBy(x => x.Description)
            .ToList();
    }

    private async Task<ExportData> LoadExportDataForErfassung(Guid contestId, Guid basisCountingCircleId)
    {
        _permissionService.EnsureAnyRole();
        var politicalBusinesses = await _contestReader.GetAccessiblePoliticalBusinesses(basisCountingCircleId, contestId);
        var politicalBusinessesByType = politicalBusinesses
            .GroupBy(x => x.BusinessType)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<SimplePoliticalBusiness>)x.ToList());
        return new ExportData(
            basisCountingCircleId,
            politicalBusinessesByType,
            Array.Empty<PoliticalBusinessUnion>());
    }

    private async Task<ExportData> LoadExportDataForMonitoring(Guid contestId)
    {
        _permissionService.EnsureMonitoringElectionAdmin();
        var politicalBusinesses = await _contestReader.GetOwnedPoliticalBusinesses(contestId);
        var politicalBusinessesByType = politicalBusinesses
            .GroupBy(x => x.BusinessType)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<SimplePoliticalBusiness>)x.ToList());
        var politicalBusinessUnions = await _contestReader.ListPoliticalBusinessUnions(contestId);
        return new ExportData(
            null,
            politicalBusinessesByType,
            politicalBusinessUnions.ToList());
    }

    private IEnumerable<ResultExportTemplate> BuildResultExportTemplates(
        TemplateModel template,
        ExportData data)
    {
        switch (template.ResultType)
        {
            // monitoring exports
            case ResultType.PoliticalBusinessResult:
            case ResultType.MultiplePoliticalBusinessesResult:
                return data.BasisCountingCircleId.HasValue
                    ? Enumerable.Empty<ResultExportTemplate>()
                    : ExpandPoliticalBusinesses(
                        template,
                        data.PoliticalBusinessesByType);

            // counting circle exports
            case ResultType.CountingCircleResult:
            case ResultType.MultiplePoliticalBusinessesCountingCircleResult:
                return !data.BasisCountingCircleId.HasValue
                    ? Enumerable.Empty<ResultExportTemplate>()
                    : ExpandPoliticalBusinesses(
                        template,
                        data.PoliticalBusinessesByType,
                        data.BasisCountingCircleId);

            case ResultType.Contest when !data.BasisCountingCircleId.HasValue:
                return new[] { new ResultExportTemplate(template) };

            case ResultType.PoliticalBusinessUnionResult when !data.BasisCountingCircleId.HasValue:
                return ExpandPoliticalBusinessUnions(template, data.PoliticalBusinessUnions);

            default:
                return Enumerable.Empty<ResultExportTemplate>();
        }
    }

    private IEnumerable<ResultExportTemplate> ExpandPoliticalBusinessUnions(TemplateModel template, IEnumerable<PoliticalBusinessUnion> unions)
        => unions.Select(u => new ResultExportTemplate(template, u.PoliticalBusinesses.ToList(), politicalBusinessUnion: u));

    private IEnumerable<ResultExportTemplate> ExpandPoliticalBusinesses(
        TemplateModel template,
        IReadOnlyDictionary<PoliticalBusinessType, IReadOnlyCollection<SimplePoliticalBusiness>> politicalBusinessesByType,
        Guid? basisCountingCircleId = null)
    {
        var pbType = MapEntityTypeToPoliticalBusinessType(template.EntityType);
        if (!politicalBusinessesByType.TryGetValue(pbType, out var politicalBusinesses))
        {
            return Enumerable.Empty<ResultExportTemplate>();
        }

        var filteredPoliticalBusinesses = FilterPoliticalBusinessesForTemplate(template, politicalBusinesses);

        if (template.ResultType
            is ResultType.MultiplePoliticalBusinessesResult
            or ResultType.MultiplePoliticalBusinessesCountingCircleResult)
        {
            return ExpandMultiplePoliticalBusinesses(template, filteredPoliticalBusinesses, basisCountingCircleId);
        }

        // expand templates for each individual political business
        return filteredPoliticalBusinesses
            .Select(pb => new ResultExportTemplate(template, new[] { pb }, countingCircleId: basisCountingCircleId));
    }

    private IEnumerable<ResultExportTemplate> ExpandMultiplePoliticalBusinesses(
        TemplateModel template,
        IEnumerable<PoliticalBusiness> politicalBusinesses,
        Guid? basisCountingCircleId)
    {
        if (!template.PerDomainOfInfluenceType)
        {
            return new[]
            {
                new ResultExportTemplate(template, politicalBusinesses.ToList(), countingCircleId: basisCountingCircleId),
            };
        }

        return politicalBusinesses.GroupBy(pb => pb.DomainOfInfluence.Type)
            .Select(g => new ResultExportTemplate(
                template,
                g.ToList(),
                $"{template.Description} ({g.Key.ToString().ToUpper()})",
                g.Key,
                basisCountingCircleId));
    }

    private PoliticalBusinessType MapEntityTypeToPoliticalBusinessType(EntityType entityType)
    {
        return entityType switch
        {
            EntityType.Vote => PoliticalBusinessType.Vote,
            EntityType.MajorityElection => PoliticalBusinessType.MajorityElection,
            EntityType.ProportionalElection => PoliticalBusinessType.ProportionalElection,
            _ => PoliticalBusinessType.Unspecified,
        };
    }

    private IEnumerable<SimplePoliticalBusiness> FilterPoliticalBusinessesForTemplate(
        TemplateModel template,
        IEnumerable<SimplePoliticalBusiness> politicalBusinesses)
    {
        politicalBusinesses = FilterDomainOfInfluenceType(template, politicalBusinesses);
        politicalBusinesses = FilterFinalized(template, politicalBusinesses);
        return FilterInvalidVotes(template, politicalBusinesses);
    }

    private IEnumerable<SimplePoliticalBusiness> FilterDomainOfInfluenceType(TemplateModel template, IEnumerable<SimplePoliticalBusiness> politicalBusinesses)
    {
        return politicalBusinesses.Where(pb =>
                !template.DomainOfInfluenceType.HasValue
                || template.DomainOfInfluenceType == _mapper.Map<DomainOfInfluenceType>(pb.DomainOfInfluence.Type));
    }

    private IEnumerable<SimplePoliticalBusiness> FilterFinalized(
        TemplateModel template,
        IEnumerable<SimplePoliticalBusiness> politicalBusinesses)
    {
        // pdfs for political business results are only available for finalized results
        if (template is not { Format: ExportFileFormat.Pdf, ResultType: ResultType.PoliticalBusinessResult })
        {
            return politicalBusinesses;
        }

        return politicalBusinesses.Where(pb => pb.EndResultFinalized);
    }

    private IEnumerable<TemplateModel> FilterFormat(IEnumerable<TemplateModel> templates, IReadOnlySet<ExportFileFormat> formats)
    {
        return formats.Count == 0
            ? templates
            : templates.Where(t => formats.Contains(t.Format));
    }

    private IEnumerable<TemplateModel> FilterDisabledTemplates(IEnumerable<TemplateModel> templates)
        => templates.Where(t => !_config.DisabledExportTemplateKeys.Contains(t.Key));

    private IEnumerable<SimplePoliticalBusiness> FilterInvalidVotes(TemplateModel template, IEnumerable<SimplePoliticalBusiness> politicalBusinesses)
    {
        // some templates are only available for majority elections with enabled invalid votes
        if (!_templateKeysOnlyWithInvalidVotes.Contains(template.Key))
        {
            return politicalBusinesses;
        }

        return politicalBusinesses.Where(pb =>
            pb.DomainOfInfluence.CantonDefaults.MajorityElectionInvalidVotes);
    }

    private record ExportData(
        Guid? BasisCountingCircleId,
        IReadOnlyDictionary<PoliticalBusinessType, IReadOnlyCollection<SimplePoliticalBusiness>> PoliticalBusinessesByType,
        IReadOnlyCollection<PoliticalBusinessUnion> PoliticalBusinessUnions);
}
