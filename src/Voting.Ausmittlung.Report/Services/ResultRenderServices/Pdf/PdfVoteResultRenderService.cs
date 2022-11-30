// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfVoteResultRenderService : IRendererService
{
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IMapper _mapper;
    private readonly TemplateService _templateService;

    public PdfVoteResultRenderService(
        IDbRepository<DataContext, Vote> voteRepo,
        IMapper mapper,
        TemplateService templateService,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IDbRepository<DataContext, Contest> contestRepo)
    {
        _voteRepo = voteRepo;
        _mapper = mapper;
        _templateService = templateService;
        _countingCircleRepo = countingCircleRepo;
        _contestRepo = contestRepo;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence)
            .FirstOrDefaultAsync(x => x.Id == ctx.ContestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), ctx.ContestId);

        var votes = await _voteRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Results.Where(r => r.CountingCircle.BasisCountingCircleId == ctx.BasisCountingCircleId))
            .ThenInclude(x => x.Results.OrderBy(b => b.Ballot.Position))
            .ThenInclude(x => x.Ballot.Translations)
            .Include(x => x.Results)
            .ThenInclude(x => x.Results)
            .ThenInclude(x => x.CountOfVoters)
            .Include(x => x.Results)
            .ThenInclude(x => x.Results)
            .ThenInclude(x => x.QuestionResults.OrderBy(q => q.Question.Number))
            .ThenInclude(x => x.Question.Translations)
            .Include(x => x.Results)
            .ThenInclude(x => x.Results)
            .ThenInclude(x => x.TieBreakQuestionResults.OrderBy(q => q.Question.Number))
            .ThenInclude(x => x.Question.Translations)
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence)
            .Where(x =>
                x.DomainOfInfluence.Type == ctx.DomainOfInfluenceType
                && ctx.PoliticalBusinessIds.Contains(x.Id))
            .OrderBy(x => x.PoliticalBusinessNumber)
            .ToListAsync(ct);

        var countingCircle = await _countingCircleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ResponsibleAuthority)
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ContestDetails.Where(co => co.ContestId == ctx.ContestId))
            .ThenInclude(x => x.VotingCards)
            .Include(x => x.ContestDetails)
            .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .FirstOrDefaultAsync(x => x.SnapshotContestId == ctx.ContestId && x.BasisCountingCircleId == ctx.BasisCountingCircleId, ct)
            ?? throw new EntityNotFoundException(nameof(CountingCircle), new { ctx.ContestId, ctx.BasisCountingCircleId });
        var ccDetails = countingCircle.ContestDetails.FirstOrDefault();
        ccDetails?.OrderVotingCardsAndSubTotals();

        var pdfCountingCircle = _mapper.Map<PdfCountingCircle>(countingCircle);
        pdfCountingCircle.ContestCountingCircleDetails = _mapper.Map<PdfContestCountingCircleDetails>(ccDetails);
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(pdfCountingCircle.ContestCountingCircleDetails, ctx.DomainOfInfluenceType);

        var pdfVotes = _mapper.Map<List<PdfVote>>(votes);
        PdfVoteUtil.SetLabels(pdfVotes);

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            Contest = _mapper.Map<PdfContest>(contest),
            CountingCircle = pdfCountingCircle,
            Votes = pdfVotes,
            DomainOfInfluenceType = ctx.DomainOfInfluenceType,
        };

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            ctx.DomainOfInfluenceType.ToString().ToUpper());
    }
}
