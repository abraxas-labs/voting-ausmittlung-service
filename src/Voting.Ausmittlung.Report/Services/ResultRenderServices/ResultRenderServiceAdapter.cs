// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices;

public class ResultRenderServiceAdapter
{
    private readonly ResultRenderServiceRegistry _resultRenderServiceRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ResultRenderServiceAdapter> _logger;
    private readonly IMapper _mapper;

    public ResultRenderServiceAdapter(
        ResultRenderServiceRegistry resultRenderServiceRegistry,
        IServiceProvider serviceProvider,
        ILogger<ResultRenderServiceAdapter> logger,
        IMapper mapper)
    {
        _resultRenderServiceRegistry = resultRenderServiceRegistry;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _mapper = mapper;
    }

    public Task<FileModel> Render(Guid contestId, ResultExportRequest request, CancellationToken ct = default)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ExportKey"] = request.Template.Key,
            ["ContestId"] = contestId,
        });

        var renderer = _resultRenderServiceRegistry.GetRenderService(request.Template.Key, _serviceProvider)
            ?? throw new InvalidOperationException($"result render service for template key {request.Template.Key} not found");

        ValidateRequestData(request, request.Template);

        var ctx = new ReportRenderContext(contestId, request.Template)
        {
            PoliticalBusinessIds = request.PoliticalBusinessIds,
            BasisCountingCircleId = request.CountingCircleId,
            DomainOfInfluenceType = request.DomainOfInfluenceType ?? DomainOfInfluenceType.Unspecified,
            PoliticalBusinessUnionId = request.PoliticalBusinessUnionId,
            PoliticalBusinessResultBundleId = request.PoliticalBusinessResultBundleId,
        };
        return renderer.Render(ctx, ct);
    }

    public IReadOnlyList<ReportRenderContext> BuildRenderContexts(
        Contest contest,
        string key,
        IReadOnlyCollection<SimplePoliticalBusiness> politicalBusinesses)
    {
        var template = TemplateRepository.GetByKey(key);
        var renderService = _resultRenderServiceRegistry.GetRenderService(key, _serviceProvider)
            ?? throw new InvalidOperationException($"result render service for template key {key} not found");

        var contexts = template.ResultType switch
        {
            ResultType.CountingCircleResult => BuildCountingCircleResultContexts(template, contest, politicalBusinesses),
            ResultType.PoliticalBusinessResult => BuildPoliticalBusinessResultContexts(template, contest, politicalBusinesses),
            ResultType.MultiplePoliticalBusinessesResult => BuildMultiplePoliticalBusinessResultContexts(template, contest, politicalBusinesses),
            ResultType.MultiplePoliticalBusinessesCountingCircleResult => BuildMultiplePoliticalBusinessCountingCircleResultContexts(template, contest, politicalBusinesses),
            ResultType.Contest => new List<ReportRenderContext> { BuildContestContext(template, contest, politicalBusinesses) },
            _ => throw new InvalidOperationException(),
        };

        var contextList = contexts.ToList();
        foreach (var context in contextList)
        {
            context.RendererService = renderService;
        }

        return contextList;
    }

    private IEnumerable<ReportRenderContext> BuildMultiplePoliticalBusinessCountingCircleResultContexts(
        TemplateModel template,
        Contest contest,
        IReadOnlyCollection<SimplePoliticalBusiness> politicalBusinesses)
    {
        if (!template.PerDomainOfInfluenceType)
        {
            foreach (var ctx in BuildCountingCircleResultContexts(template, contest, politicalBusinesses))
            {
                yield return ctx;
            }

            yield break;
        }

        var pbGroupedByTypeAndDoiType = politicalBusinesses
            .GroupBy(pb => (pb.BusinessType, pb.DomainOfInfluence.Type))
            .ToDictionary(x => x.Key, x => x.ToList());
        foreach (var ((businessType, doiType), politicalBusiness) in pbGroupedByTypeAndDoiType)
        {
            if (!template.CanExport(businessType, _mapper.Map<Lib.VotingExports.Models.DomainOfInfluenceType>(doiType)))
            {
                continue;
            }

            var ccBasisIds = politicalBusiness
                .SelectMany(pb => pb.SimpleResults)
                .Select(r => r.CountingCircle!.BasisCountingCircleId)
                .Distinct();
            var politicalBusinessIds = politicalBusiness.Select(x => x.Id).ToHashSet();
            foreach (var ccBasisId in ccBasisIds)
            {
                yield return new ReportRenderContext(contest.Id, template)
                {
                    PoliticalBusinessIds = politicalBusinessIds,
                    BasisCountingCircleId = ccBasisId,
                    DomainOfInfluenceType = doiType,
                };
            }
        }
    }

    private IEnumerable<ReportRenderContext> BuildMultiplePoliticalBusinessResultContexts(
        TemplateModel template,
        Contest contest,
        IReadOnlyCollection<SimplePoliticalBusiness> politicalBusinesses)
    {
        if (!template.PerDomainOfInfluenceType)
        {
            yield return new ReportRenderContext(contest.Id, template)
            {
                PoliticalBusinessIds = politicalBusinesses.Select(x => x.Id).ToList(),
            };
            yield break;
        }

        foreach (var group in politicalBusinesses.GroupBy(x => x.DomainOfInfluence.Type))
        {
            yield return new ReportRenderContext(contest.Id, template)
            {
                DomainOfInfluenceType = group.Key,
                PoliticalBusinessIds = group.Select(x => x.Id).ToList(),
            };
        }
    }

    private IEnumerable<ReportRenderContext> BuildPoliticalBusinessResultContexts(
        TemplateModel template,
        Contest contest,
        IReadOnlyCollection<SimplePoliticalBusiness> politicalBusinesses)
    {
        foreach (var politicalBusiness in politicalBusinesses)
        {
            var doiType = _mapper.Map<Lib.VotingExports.Models.DomainOfInfluenceType>(politicalBusiness.DomainOfInfluence.Type);
            if (!template.CanExport(politicalBusiness.BusinessType, doiType))
            {
                continue;
            }

            yield return new ReportRenderContext(contest.Id, template)
            {
                PoliticalBusinessIds = new[]
                {
                    politicalBusiness.Id,
                },
            };
        }
    }

    private IEnumerable<ReportRenderContext> BuildCountingCircleResultContexts(
        TemplateModel template,
        Contest contest,
        IReadOnlyCollection<SimplePoliticalBusiness> politicalBusinesses)
    {
        foreach (var politicalBusiness in politicalBusinesses)
        {
            var doiType = _mapper.Map<Lib.VotingExports.Models.DomainOfInfluenceType>(politicalBusiness.DomainOfInfluence.Type);
            if (!template.CanExport(politicalBusiness.BusinessType, doiType))
            {
                continue;
            }

            foreach (var ccResult in politicalBusiness.SimpleResults)
            {
                yield return new ReportRenderContext(contest.Id, template)
                {
                    BasisCountingCircleId = ccResult.CountingCircle!.BasisCountingCircleId,
                    PoliticalBusinessIds = new[]
                    {
                        politicalBusiness.Id,
                    },
                };
            }
        }
    }

    private ReportRenderContext BuildContestContext(
        TemplateModel template,
        Contest contest,
        IReadOnlyCollection<SimplePoliticalBusiness> politicalBusinesses)
    {
        return new ReportRenderContext(contest.Id, template)
        {
            PoliticalBusinessIds = politicalBusinesses.Select(x => x.Id).ToList(),
        };
    }

    private void ValidateRequestData(ResultExportRequest request, TemplateModel template)
    {
        if (request.PoliticalBusinessIds.Count == 0
            && template.ResultType is not ResultType.Contest and not ResultType.PoliticalBusinessUnionResult and not ResultType.PoliticalBusinessResultBundleReview)
        {
            throw new ValidationException("Cannot render without any political business ids provided");
        }

        if (request.PoliticalBusinessIds.Count != 1
            && template.ResultType is ResultType.CountingCircleResult or ResultType.PoliticalBusinessResult)
        {
            throw new ValidationException("Cannot render a counting circle result report with multiple political business ids");
        }

        if (!request.PoliticalBusinessUnionId.HasValue
            && template.ResultType is ResultType.PoliticalBusinessUnionResult)
        {
            throw new ValidationException("Cannot render a political business union result report without a union id");
        }

        if ((template.PerDomainOfInfluenceType || template.DomainOfInfluenceType.HasValue) && !request.DomainOfInfluenceType.HasValue)
        {
            throw new ValidationException("Cannot render a domain of influence type report without specifying the domain of influence type");
        }

        if (!request.PoliticalBusinessResultBundleId.HasValue
            && template.ResultType is ResultType.PoliticalBusinessResultBundleReview)
        {
            throw new ValidationException("Cannot render a political business result bundle review report without a bundle id");
        }
    }
}
