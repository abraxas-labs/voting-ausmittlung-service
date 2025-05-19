// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfProportionalElectionCountingCircleResultRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, ProportionalElectionResult> _repo;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;
    private readonly IMapper _mapper;
    private readonly IClock _clock;

    public PdfProportionalElectionCountingCircleResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElectionResult> repo,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        IMapper mapper,
        IClock clock)
    {
        _templateService = templateService;
        _repo = repo;
        _mapper = mapper;
        _clock = clock;
        _ccDetailsRepo = ccDetailsRepo;
    }

    public async Task<FileModel> Render(
        ReportRenderContext ctx,
        CancellationToken ct = default)
    {
        var data = await BuildQuery()
            .FirstOrDefaultAsync(x => x.ProportionalElectionId == ctx.PoliticalBusinessId && x.CountingCircle.BasisCountingCircleId == ctx.BasisCountingCircleId, ct)
            ?? throw new ValidationException($"invalid data requested: politicalBusinessId: {ctx.PoliticalBusinessId}, countingCircleId: {ctx.BasisCountingCircleId}");

        data.MoveECountingToConventional();

        // this could be inlined with ef core 5
        PrepareAndSortData(data);

        var ccDetails = await _ccDetailsRepo.Query()
            .AsSplitQuery()
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.VotingCards)
            .FirstOrDefaultAsync(
                x =>
                    x.ContestId == data.ProportionalElection.ContestId
                    && x.CountingCircleId == data.CountingCircleId,
                ct);

        ccDetails?.OrderVotingCardsAndSubTotals();

        var proportionalElection = _mapper.Map<PdfProportionalElection>(data.ProportionalElection);
        var result = proportionalElection.Results[0];
        PdfProportionalElectionResultUtil.SetTotalListResults(result);

        var countingCircle = result.CountingCircle!;
        result.CountingCircle = null;
        countingCircle.ContestCountingCircleDetails = _mapper.Map<PdfContestCountingCircleDetails>(ccDetails);
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotalsAndCountOfVoters(countingCircle.ContestCountingCircleDetails, data.ProportionalElection.DomainOfInfluence);

        // we don't need this data in the xml
        countingCircle.ContestCountingCircleDetails.VotingCards = new List<PdfVotingCardResultDetail>();

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            GeneratedAt = _clock.UtcNow.ConvertUtcTimeToSwissTime(),
            Contest = _mapper.Map<PdfContest>(data.ProportionalElection.Contest),
            CountingCircle = countingCircle,
            ProportionalElections = new List<PdfProportionalElection>
            {
                proportionalElection,
            },
        };

        PreparePdfData(templateBag);

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            data.ProportionalElection.ShortDescription,
            PdfDateUtil.BuildDateForFilename(templateBag.Contest.Date));
    }

    protected virtual void PrepareAndSortData(ProportionalElectionResult data)
    {
        data.ListResults = data.ListResults.OrderBy(l => l.List.Position).ToList();
    }

    protected virtual IQueryable<ProportionalElectionResult> BuildQuery()
    {
        return _repo.Query()
            .AsSplitQuery()
            .Include(x => x.ListResults).ThenInclude(lr => lr.List.Translations)
            .Include(x => x.ProportionalElection.DomainOfInfluence)
            .Include(x => x.ProportionalElection.Translations)
            .Include(x => x.ProportionalElection.Contest.Translations)
            .Include(x => x.ProportionalElection.Contest.DomainOfInfluence)
            .Include(x => x.CountingCircle.ResponsibleAuthority)
            .Include(x => x.CountingCircle.ContactPersonDuringEvent)
            .Include(x => x.CountingCircle.ContactPersonAfterEvent);
    }

    protected virtual void PreparePdfData(PdfTemplateBag templateBag)
    {
    }
}
