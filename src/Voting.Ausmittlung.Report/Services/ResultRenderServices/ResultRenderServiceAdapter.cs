// (c) Copyright by Abraxas Informatik AG
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

    public Task<FileModel> Render(Guid contestId, ReportRenderContext renderContext, CancellationToken ct = default)
    {
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ExportKey"] = renderContext.Template.Key,
            ["ContestId"] = contestId,
        });

        var renderer = _resultRenderServiceRegistry.GetRenderService(renderContext.Template.Key, _serviceProvider)
            ?? throw new InvalidOperationException($"result render service for template key {renderContext.Template.Key} not found");

        ValidateRequestData(renderContext);

        return renderer.Render(renderContext, ct);
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
        if (template.PerDomainOfInfluenceType)
        {
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

            yield break;
        }

        if (template.PerDomainOfInfluence)
        {
            var pbGroupedByTypeAndDoi = politicalBusinesses
                .GroupBy(pb => (pb.BusinessType, pb.DomainOfInfluence.BasisDomainOfInfluenceId))
                .ToDictionary(x => x.Key, x => x.ToList());
            foreach (var ((_, doiId), politicalBusiness) in pbGroupedByTypeAndDoi)
            {
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
                        BasisDomainOfInfluenceId = doiId,
                    };
                }
            }

            yield break;
        }

        foreach (var ctx in BuildCountingCircleResultContexts(template, contest, politicalBusinesses))
        {
            yield return ctx;
        }
    }

    private IEnumerable<ReportRenderContext> BuildMultiplePoliticalBusinessResultContexts(
        TemplateModel template,
        Contest contest,
        IReadOnlyCollection<SimplePoliticalBusiness> politicalBusinesses)
    {
        if (template.PerDomainOfInfluenceType)
        {
            foreach (var group in politicalBusinesses.GroupBy(x => x.DomainOfInfluence.Type))
            {
                yield return new ReportRenderContext(contest.Id, template)
                {
                    DomainOfInfluenceType = group.Key,
                    PoliticalBusinessIds = group.Select(x => x.Id).ToList(),
                };
            }

            yield break;
        }

        if (template.PerDomainOfInfluence)
        {
            foreach (var group in politicalBusinesses.GroupBy(x => x.DomainOfInfluence.BasisDomainOfInfluenceId))
            {
                yield return new ReportRenderContext(contest.Id, template)
                {
                    BasisDomainOfInfluenceId = group.Key,
                    PoliticalBusinessIds = group.Select(x => x.Id).ToList(),
                };
            }

            yield break;
        }

        yield return new ReportRenderContext(contest.Id, template)
        {
            PoliticalBusinessIds = politicalBusinesses.Select(x => x.Id).ToList(),
        };
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

    private void ValidateRequestData(ReportRenderContext renderContext)
    {
        if (renderContext.PoliticalBusinessIds.Count == 0
            && renderContext.Template.ResultType is not ResultType.Contest and not ResultType.PoliticalBusinessUnionResult and not ResultType.PoliticalBusinessResultBundleReview)
        {
            throw new ValidationException("Cannot render without any political business ids provided");
        }

        if (renderContext.PoliticalBusinessIds.Count != 1
            && renderContext.Template.ResultType is ResultType.CountingCircleResult or ResultType.PoliticalBusinessResult)
        {
            throw new ValidationException("Cannot render a counting circle result report with multiple political business ids");
        }

        if (!renderContext.PoliticalBusinessUnionId.HasValue
            && renderContext.Template.ResultType is ResultType.PoliticalBusinessUnionResult)
        {
            throw new ValidationException("Cannot render a political business union result report without a union id");
        }

        if (renderContext.Template.PerDomainOfInfluenceType && renderContext.DomainOfInfluenceType == DomainOfInfluenceType.Unspecified)
        {
            throw new ValidationException("Cannot render a domain of influence type report without specifying the domain of influence type");
        }

        if (renderContext.Template.PerDomainOfInfluence && !renderContext.BasisDomainOfInfluenceId.HasValue)
        {
            throw new ValidationException("Cannot render a domain of influence report without specifying the domain of influence");
        }

        if (!renderContext.PoliticalBusinessResultBundleId.HasValue
            && renderContext.Template.ResultType is ResultType.PoliticalBusinessResultBundleReview)
        {
            throw new ValidationException("Cannot render a political business result bundle review report without a bundle id");
        }
    }
}
