// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using DomainOfInfluenceType = Voting.Lib.VotingExports.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ResultExportTemplateReader
{
    private readonly PoliticalBusinessQueries _pbQueries;
    private readonly ContestReader _contestReader;
    private readonly PermissionService _permissionService;
    private readonly IMapper _mapper;
    private readonly PublisherConfig _config;

    public ResultExportTemplateReader(
        PoliticalBusinessQueries pbQueries,
        ContestReader contestReader,
        PermissionService permissionService,
        IMapper mapper,
        PublisherConfig config)
    {
        _pbQueries = pbQueries;
        _contestReader = contestReader;
        _permissionService = permissionService;
        _mapper = mapper;
        _config = config;
    }

    public async Task<List<ResultExportTemplate>> GetForCountingCircleResult(
        Guid basisCountingCircleId,
        Guid politicalBusinessId,
        PoliticalBusinessType politicalBusinessType)
    {
        _permissionService.EnsureCanExport(ResultType.CountingCircleResult);
        if (_config.DisableAllExports)
        {
            return new List<ResultExportTemplate>();
        }

        var entityType = MapPoliticalBusinessTypeToEntityType(politicalBusinessType);

        var ccResult = await _pbQueries
            .CountingCircleResultQueryIncludingPoliticalBusiness(politicalBusinessType, politicalBusinessId, basisCountingCircleId)
            .FirstOrDefaultAsync()
        ?? throw new EntityNotFoundException(new { politicalBusinessId, basisCountingCircleId });

        await _permissionService.EnsureCanReadBasisCountingCircle(basisCountingCircleId, ccResult.PoliticalBusiness.ContestId);

        var templates = TemplateRepository.GetCountingCircleResultTemplates(
            VotingApp.VotingAusmittlung,
            entityType,
            _mapper.Map<DomainOfInfluenceType>(ccResult.PoliticalBusiness.DomainOfInfluence.Type));
        var enabledTemplates = FilterDisabledTemplates(templates);

        var politicalBusinesses = new List<PoliticalBusiness> { ccResult.PoliticalBusiness };

        return enabledTemplates
            .Select(t => new ResultExportTemplate(
                t,
                politicalBusinesses,
                countingCircleId: basisCountingCircleId))
            .ToList();
    }

    public async Task<List<ResultExportTemplate>> GetForPoliticalBusinessResult(
        Guid politicalBusinessId,
        PoliticalBusinessType politicalBusinessType)
    {
        _permissionService.EnsureCanExport(ResultType.PoliticalBusinessResult);
        if (_config.DisableAllExports)
        {
            return new List<ResultExportTemplate>();
        }

        var entityType = MapPoliticalBusinessTypeToEntityType(politicalBusinessType);

        var politicalBusiness = await _pbQueries
            .PoliticalBusinessQuery(politicalBusinessType)
            .Include(pb => pb.DomainOfInfluence)
            .FirstOrDefaultAsync(pb => pb.Id == politicalBusinessId)
            ?? throw new EntityNotFoundException(politicalBusinessId);

        await _permissionService.EnsureCanReadContest(politicalBusiness.ContestId);

        var templates = TemplateRepository.GetPoliticalBusinessResultTemplates(
            VotingApp.VotingAusmittlung,
            entityType,
            _mapper.Map<DomainOfInfluenceType>(politicalBusiness.DomainOfInfluence.Type));
        var enabledTemplates = FilterDisabledTemplates(templates);

        var isFinalized = await _pbQueries
            .PoliticalBusinessEndResultQuery(politicalBusinessType, politicalBusinessId)
            .AnyAsync(x => x.Finalized);

        if (!isFinalized)
        {
            enabledTemplates = FilterFinalizedTemplates(enabledTemplates);
        }

        var hasInvalidVotes = politicalBusiness.DomainOfInfluence.CantonDefaults.MajorityElectionInvalidVotes;
        if (!hasInvalidVotes)
        {
            enabledTemplates = FilterInvalidVotesTemplates(enabledTemplates);
        }

        var politicalBusinesses = new List<PoliticalBusiness> { politicalBusiness };

        return enabledTemplates
            .Select(t => new ResultExportTemplate(t, politicalBusinesses))
            .ToList();
    }

    public async Task<IReadOnlyCollection<ResultExportTemplate>> GetForContest(Guid contestId)
    {
        _permissionService.EnsureCanExport(ResultType.Contest);
        if (_config.DisableAllExports)
        {
            return new List<ResultExportTemplate>();
        }

        await _permissionService.EnsureCanReadContest(contestId);

        var templates = TemplateRepository.GetContestTemplates(VotingApp.VotingAusmittlung);
        var enabledTemplates = FilterDisabledTemplates(templates);
        return enabledTemplates.Select(x => new ResultExportTemplate(x, Array.Empty<PoliticalBusiness>())).ToList();
    }

    public async Task<List<ResultExportTemplate>> GetForMultiplePoliticalBusinessesResult(Guid contestId)
    {
        _permissionService.EnsureCanExport(ResultType.MultiplePoliticalBusinessesResult);
        if (_config.DisableAllExports)
        {
            return new List<ResultExportTemplate>();
        }

        var politicalBusinesses = await _contestReader.GetOwnedPoliticalBusinesses(contestId);
        if (politicalBusinesses.Count == 0)
        {
            throw new EntityNotFoundException("no owned political businesses found");
        }

        var templates = TemplateRepository.GetMultiplePoliticalBusinessesResultTemplates(VotingApp.VotingAusmittlung);
        var enabledTemplates = FilterDisabledTemplates(templates);
        return enabledTemplates
            .SelectMany(x => CreatePoliticalBusinessesTemplateModels(x, politicalBusinesses))
            .ToList();
    }

    public async Task<List<ResultExportTemplate>> GetForMultiplePoliticalBusinessesCountingCircleResult(Guid contestId, Guid basisCountingCircleId)
    {
        _permissionService.EnsureCanExport(ResultType.MultiplePoliticalBusinessesCountingCircleResult);
        if (_config.DisableAllExports)
        {
            return new List<ResultExportTemplate>();
        }

        var politicalBusinesses = await _contestReader.GetAccessiblePoliticalBusinesses(basisCountingCircleId, contestId);
        if (politicalBusinesses.Count == 0)
        {
            throw new EntityNotFoundException("no accessible political businesses found");
        }

        var templates = TemplateRepository.GetMultiplePoliticalBusinessesCountingCircleResultTemplates(VotingApp.VotingAusmittlung);
        var enabledTemplates = FilterDisabledTemplates(templates);
        return enabledTemplates
            .SelectMany(x => CreatePoliticalBusinessesTemplateModels(x, politicalBusinesses, basisCountingCircleId))
            .ToList();
    }

    public async Task<List<ResultExportTemplate>> GetForPoliticalBusinessUnionResult(
        Guid politicalBusinessUnionId,
        PoliticalBusinessType politicalBusinessType)
    {
        _permissionService.EnsureCanExport(ResultType.PoliticalBusinessUnionResult);
        if (_config.DisableAllExports)
        {
            return new List<ResultExportTemplate>();
        }

        var entityType = MapPoliticalBusinessTypeToEntityType(politicalBusinessType);

        var politicalBusinessUnion = await _pbQueries
            .PoliticalBusinessUnionQueryIncludingPoliticalBusinesses(politicalBusinessType)
            .FirstOrDefaultAsync(x => x.Id == politicalBusinessUnionId)
            ?? throw new EntityNotFoundException(politicalBusinessUnionId);

        await _permissionService.EnsureCanReadContest(politicalBusinessUnion.ContestId);

        var templates = TemplateRepository.GetPoliticalBusinessUnionResultTemplates(VotingApp.VotingAusmittlung, entityType);
        var enabledTemplates = FilterDisabledTemplates(templates);
        return enabledTemplates
            .Select(t => new ResultExportTemplate(
                t,
                politicalBusinessUnion.PoliticalBusinesses.ToList(),
                politicalBusinessUnionId: politicalBusinessUnionId))
            .ToList();
    }

    private IEnumerable<ResultExportTemplate> CreatePoliticalBusinessesTemplateModels(
        TemplateModel template,
        IEnumerable<PoliticalBusiness> politicalBusinesses,
        Guid? basisCountingCircleId = null)
    {
        var filteredPoliticalBusinesses = politicalBusinesses
            .Where(pb => pb.BusinessType != PoliticalBusinessType.SecondaryMajorityElection
                && MapPoliticalBusinessTypeToEntityType(pb.BusinessType) == template.EntityType
                && (!template.DomainOfInfluenceType.HasValue || template.DomainOfInfluenceType == _mapper.Map<DomainOfInfluenceType>(pb.DomainOfInfluence.Type)))
            .ToList();

        if (filteredPoliticalBusinesses.Count == 0)
        {
            return Enumerable.Empty<ResultExportTemplate>();
        }

        if (!template.PerDomainOfInfluenceType)
        {
            return new[]
            {
                new ResultExportTemplate(
                    template,
                    filteredPoliticalBusinesses,
                    countingCircleId: basisCountingCircleId),
            };
        }

        return filteredPoliticalBusinesses.GroupBy(pb => pb.DomainOfInfluence.Type)
            .Select(g => new ResultExportTemplate(
                template,
                g,
                $"{template.Description} ({g.Key.ToString().ToUpper()})",
                g.Key,
                basisCountingCircleId));
    }

    private EntityType MapPoliticalBusinessTypeToEntityType(PoliticalBusinessType politicalBusinessType)
    {
        return politicalBusinessType switch
        {
            PoliticalBusinessType.Vote => EntityType.Vote,
            PoliticalBusinessType.MajorityElection => EntityType.MajorityElection,
            PoliticalBusinessType.ProportionalElection => EntityType.ProportionalElection,
            _ => throw new InvalidOperationException($"Cannot map {politicalBusinessType} to an EntityType"),
        };
    }

    private IEnumerable<TemplateModel> FilterDisabledTemplates(IEnumerable<TemplateModel> templates)
        => templates.Where(t => !_config.DisabledExportTemplateKeys.Contains(t.Key));

    private IEnumerable<TemplateModel> FilterFinalizedTemplates(IEnumerable<TemplateModel> templates)
        => templates.Where(t => t.Format != ExportFileFormat.Pdf);

    private IEnumerable<TemplateModel> FilterInvalidVotesTemplates(IEnumerable<TemplateModel> templates)
        => templates.Where(t => t.Key != AusmittlungPdfMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol.Key);
}
