// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfProportionalElectionEndResultRenderService : IRendererService
{
    private readonly IDbRepository<DataContext, ProportionalElectionEndResult> _repo;
    private readonly IMapper _mapper;
    private readonly TemplateService _templateService;

    public PdfProportionalElectionEndResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElectionEndResult> repo,
        IMapper mapper)
    {
        _templateService = templateService;
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var data = await BuildQuery()
            .FirstOrDefaultAsync(x => x.ProportionalElectionId == ctx.PoliticalBusinessId, ct)
            ?? throw new ValidationException($"invalid data requested: politicalBusinessId: {ctx.PoliticalBusinessId}");

        // with ef core 5 this could be inlined
        PrepareAndSortData(data);

        var proportionalElection = _mapper.Map<PdfProportionalElection>(data.ProportionalElection);
        if (proportionalElection.EndResult != null)
        {
            SetTotalListResults(proportionalElection.EndResult);
        }

        // reset the domain of influence on the result, since this is a single domain of influence report
        var domainOfInfluence = proportionalElection.DomainOfInfluence;
        proportionalElection.DomainOfInfluence = null;

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            Contest = _mapper.Map<PdfContest>(data.ProportionalElection.Contest),
            ProportionalElections = new List<PdfProportionalElection>
                {
                    proportionalElection,
                },
            DomainOfInfluence = domainOfInfluence,
        };

        PreparePdfData(templateBag);

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            data.ProportionalElection.ShortDescription);
    }

    protected virtual void PreparePdfData(PdfTemplateBag templateBag)
    {
    }

    protected virtual void PrepareAndSortData(ProportionalElectionEndResult data)
    {
        data.ListEndResults = data.ListEndResults.OrderBy(lr => lr.List.Position).ToList();
    }

    protected virtual IQueryable<ProportionalElectionEndResult> BuildQuery()
    {
        return _repo.Query()
            .AsSplitQuery()
            .Include(x => x.ListEndResults).ThenInclude(lr => lr.List.Translations)
            .Include(x => x.ProportionalElection.Translations)
            .Include(x => x.ProportionalElection.DomainOfInfluence)
            .Include(x => x.ProportionalElection.Contest.Translations)
            .Include(x => x.ProportionalElection.Contest.DomainOfInfluence);
    }

    private void SetTotalListResults(PdfProportionalElectionEndResult result)
    {
        result.TotalListEndResult = new PdfProportionalElectionListEndResult
        {
            NumberOfMandates = result.ListEndResults.Sum(x => x.NumberOfMandates),
            TotalVoteCount = result.ListEndResults.Sum(x => x.TotalVoteCount),
            BlankRowsCount = result.ListEndResults.Sum(x => x.BlankRowsCount),
            ListCount = result.ListEndResults.Sum(x => x.ListCount),
            ListVotesCount = result.ListEndResults.Sum(x => x.ListVotesCount),
            ModifiedListsCount = result.ListEndResults.Sum(x => x.ModifiedListsCount),
            UnmodifiedListsCount = result.ListEndResults.Sum(x => x.UnmodifiedListsCount),
            ModifiedListVotesCount = result.ListEndResults.Sum(x => x.ModifiedListVotesCount),
            UnmodifiedListVotesCount = result.ListEndResults.Sum(x => x.UnmodifiedListVotesCount),
            ModifiedListBlankRowsCount = result.ListEndResults.Sum(x => x.ModifiedListBlankRowsCount),
            UnmodifiedListBlankRowsCount = result.ListEndResults.Sum(x => x.UnmodifiedListBlankRowsCount),
        };
        result.TotalListResultInclWithoutParty = new PdfProportionalElectionListEndResult
        {
            TotalVoteCount = result.TotalListEndResult.TotalVoteCount + result.TotalCountOfBlankRowsOnListsWithoutParty,
            BlankRowsCount = result.TotalListEndResult.BlankRowsCount + result.TotalCountOfBlankRowsOnListsWithoutParty,
            ListCount = result.TotalListEndResult.ListCount + result.TotalCountOfListsWithoutParty,
            ListVotesCount = result.TotalListEndResult.ListVotesCount,
            ModifiedListsCount = result.TotalListEndResult.ModifiedListsCount + result.TotalCountOfListsWithoutParty,
            UnmodifiedListsCount = result.TotalListEndResult.UnmodifiedListsCount,
            ModifiedListVotesCount = result.TotalListEndResult.ModifiedListVotesCount,
            UnmodifiedListVotesCount = result.TotalListEndResult.UnmodifiedListVotesCount,
            ModifiedListBlankRowsCount = result.TotalListEndResult.ModifiedListBlankRowsCount + result.TotalCountOfBlankRowsOnListsWithoutParty,
            UnmodifiedListBlankRowsCount = result.TotalListEndResult.UnmodifiedListBlankRowsCount,
        };
    }
}
