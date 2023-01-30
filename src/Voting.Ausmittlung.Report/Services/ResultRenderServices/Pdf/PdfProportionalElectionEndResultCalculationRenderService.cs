// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfProportionalElectionEndResultCalculationRenderService : IRendererService
{
    private static readonly int NrOfHagenbachBischoffLevels = Enum.GetValues(typeof(HagenbachBischoffGroupType)).Length;

    private readonly IDbRepository<DataContext, ProportionalElectionEndResult> _repo;
    private readonly TemplateService _templateService;
    private readonly IClock _clock;

    public PdfProportionalElectionEndResultCalculationRenderService(
        IDbRepository<DataContext, ProportionalElectionEndResult> repo,
        IMapper mapper,
        TemplateService templateService,
        IClock clock)
    {
        _repo = repo;
        _templateService = templateService;
        _clock = clock;
        Mapper = mapper;
    }

    protected IMapper Mapper { get; }

    protected bool IncludeCalculationRounds { get; set; } = true;

    public async Task<FileModel> Render(
        ReportRenderContext ctx,
        CancellationToken ct = default)
    {
        var data = await BuildQuery()
            .FirstOrDefaultAsync(x => x.ProportionalElectionId == ctx.PoliticalBusinessId, ct)
            ?? throw new ValidationException($"invalid data requested: {nameof(ctx.PoliticalBusinessId)}: {ctx.PoliticalBusinessId}");

        // can be inlined with ef5
        SortData(data);

        // reset the domain of influence on the result, since this is a single domain of influence report
        var proportionalElection = Mapper.Map<PdfProportionalElection>(data.ProportionalElection);
        var domainOfInfluence = proportionalElection.DomainOfInfluence;
        domainOfInfluence!.Details ??= new PdfContestDomainOfInfluenceDetails();
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(domainOfInfluence.Details, domainOfInfluence.Type);

        // we don't need this data in the xml
        domainOfInfluence.Details!.VotingCards = new();
        proportionalElection.DomainOfInfluence = null!;

        MapAdditionalElectionData(data, proportionalElection);

        var contest = Mapper.Map<PdfContest>(data.ProportionalElection.Contest);
        if (contest.Details != null)
        {
            PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(contest.Details!, domainOfInfluence!.Type);
            contest.Details!.VotingCards = new();
        }

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            Contest = contest,
            ProportionalElections = new List<PdfProportionalElection>
                {
                    proportionalElection,
                },
            DomainOfInfluence = domainOfInfluence,
        };

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            data.ProportionalElection.ShortDescription,
            PdfDateUtil.BuildDateForFilename(_clock.UtcNow));
    }

    protected virtual void MapAdditionalElectionData(ProportionalElectionEndResult endResult, PdfProportionalElection pdfElection)
    {
    }

    protected virtual IQueryable<ProportionalElectionEndResult> BuildQuery()
    {
        var query = _repo.Query()
            .AsSplitQuery()
            .Include(x => x.ProportionalElection.Translations)
            .Include(x => x.ProportionalElection.DomainOfInfluence.Details!.VotingCards)
            .Include(x => x.ProportionalElection.Contest.Translations)
            .Include(x => x.ProportionalElection.Contest.DomainOfInfluence);
        return BuildCalculationIncludes(query);
    }

    protected virtual void SortData(ProportionalElectionEndResult data)
    {
        data.HagenbachBischoffRootGroup?.SortCalculationRounds();
    }

    private IQueryable<ProportionalElectionEndResult> BuildCalculationIncludes(IQueryable<ProportionalElectionEndResult> query)
    {
        if (IncludeCalculationRounds)
        {
            query = query
                .Include(x => x.HagenbachBischoffRootGroup!.CalculationRounds)
                .ThenInclude(x => x.GroupValues)
                .ThenInclude(x => x.Group!.ListUnion!.Translations)
                .Include(x => x.HagenbachBischoffRootGroup!.CalculationRounds)
                .ThenInclude(x => x.GroupValues)
                .ThenInclude(x => x.Group!.List!.Translations);
        }

        for (var level = 0; level <= NrOfHagenbachBischoffLevels; level++)
        {
            var withList = BuildChildrenInclude(query, level)
                .ThenInclude(x => x.List!.Translations);
            var withListUnion = BuildChildrenInclude(withList, level)
                .ThenInclude(x => x.ListUnion!.Translations);

            if (IncludeCalculationRounds)
            {
                var withCalculationRoundList = BuildChildrenInclude(withListUnion, level)
                    .ThenInclude(x => x.CalculationRounds)
                    .ThenInclude(x => x.GroupValues)
                    .ThenInclude(x => x.Group!.List!.Translations);
                query = BuildChildrenInclude(withCalculationRoundList, level)
                    .ThenInclude(x => x.CalculationRounds)
                    .ThenInclude(x => x.GroupValues)
                    .ThenInclude(x => x.Group!.ListUnion!.Translations);
            }
            else
            {
                query = withListUnion;
            }
        }

        return query;
    }

    private IIncludableQueryable<ProportionalElectionEndResult, ICollection<HagenbachBischoffGroup>>
        BuildChildrenInclude(
            IQueryable<ProportionalElectionEndResult> query,
            int level)
    {
        var includableQueryable = query.Include(x => x.HagenbachBischoffRootGroup!.Children);
        for (var i = 0; i < level; i++)
        {
            includableQueryable = includableQueryable.ThenInclude(x => x.Children);
        }

        return includableQueryable;
    }
}
