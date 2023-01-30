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
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfVoteEndResultRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IMapper _mapper;
    private readonly IClock _clock;

    public PdfVoteEndResultRenderService(
        TemplateService templateService,
        IMapper mapper,
        IDbRepository<DataContext, Contest> contestRepo,
        IDbRepository<DataContext, Vote> voteRepo,
        IClock clock)
    {
        _templateService = templateService;
        _mapper = mapper;
        _contestRepo = contestRepo;
        _voteRepo = voteRepo;
        _clock = clock;
    }

    public async Task<FileModel> Render(
        ReportRenderContext ctx,
        CancellationToken ct = default)
    {
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Details!.VotingCards)
            .FirstOrDefaultAsync(x => x.Id == ctx.ContestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), ctx.ContestId);
        contest.Details?.OrderVotingCardsAndSubTotals();

        var votes = await _voteRepo.Query()
            .AsSplitQuery()
            .Include(x => x.EndResult!.BallotEndResults.OrderBy(b => b.Ballot.Position)).ThenInclude(x => x.Ballot.Translations)
            .Include(x => x.EndResult!.BallotEndResults).ThenInclude(x => x.QuestionEndResults.OrderBy(q => q.Question.Number)).ThenInclude(x => x.Question.Translations)
            .Include(x => x.EndResult!.BallotEndResults).ThenInclude(x => x.TieBreakQuestionEndResults.OrderBy(q => q.Question.Number)).ThenInclude(x => x.Question.Translations)
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence)
            .Where(x =>
                x.ResultAlgorithm == VoteResultAlgorithm.PopularMajority
                && ctx.PoliticalBusinessIds.Contains(x.Id)
                && x.DomainOfInfluence.Type == ctx.DomainOfInfluenceType)
            .OrderBy(x => x.PoliticalBusinessNumber)
            .ToListAsync(ct);

        var pdfVotes = _mapper.Map<List<PdfVote>>(votes);
        PdfVoteUtil.SetLabels(pdfVotes);

        var pdfContest = _mapper.Map<PdfContest>(contest);
        pdfContest.Details ??= new PdfContestDetails();
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(pdfContest.Details, ctx.DomainOfInfluenceType);

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            Contest = pdfContest,
            Votes = pdfVotes,
            DomainOfInfluenceType = ctx.DomainOfInfluenceType,
        };

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            ctx.DomainOfInfluenceType.ToString().ToUpper(),
            PdfDateUtil.BuildDateForFilename(_clock.UtcNow));
    }
}
