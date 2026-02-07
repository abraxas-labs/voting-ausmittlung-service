// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
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

public class PdfVoteResultBundleReviewRenderService : IRendererService
{
    private readonly IDbRepository<DataContext, VoteResultBundle> _voteResultBundleRepo;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly TemplateService _templateService;

    public PdfVoteResultBundleReviewRenderService(
        IDbRepository<DataContext, VoteResultBundle> voteResultBundleRepo,
        IMapper mapper,
        TemplateService templateService,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        IClock clock)
    {
        _voteResultBundleRepo = voteResultBundleRepo;
        _mapper = mapper;
        _templateService = templateService;
        _countingCircleRepo = countingCircleRepo;
        _clock = clock;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        if (ctx.PoliticalBusinessResultBundleId == null || ctx.PoliticalBusinessResultBundleId == Guid.Empty)
        {
            throw new ValidationException("Political business result bundle id must not be null or empty.");
        }

        var bundle = await _voteResultBundleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Ballots.Where(b => b.MarkedForReview))
            .ThenInclude(x => x.QuestionAnswers)
            .ThenInclude(x => x.Question.Translations)
            .Include(x => x.Ballots.Where(b => b.MarkedForReview))
            .ThenInclude(x => x.TieBreakQuestionAnswers)
            .ThenInclude(x => x.Question.Translations)
            .Include(x => x.BallotResult.VoteResult.Vote.Translations)
            .FirstOrDefaultAsync(x => x.Id == ctx.PoliticalBusinessResultBundleId, ct)
            ?? throw new EntityNotFoundException(nameof(VoteResultBundle), ctx.PoliticalBusinessResultBundleId);

        if (bundle.BallotResult.VoteResult.VoteId != ctx.PoliticalBusinessId)
        {
            throw new ValidationException("political business id is not valid for bundle");
        }

        if (!bundle.BallotResult.VoteResult.PoliticalBusiness.Active)
        {
            throw new ValidationException("political business is not active");
        }

        bundle.Ballots = bundle.Ballots.OrderBy(x => x.Index).ToList();
        foreach (var ballot in bundle.Ballots)
        {
            ballot.QuestionAnswers = ballot.QuestionAnswers.OrderBy(x => x.Question.Number).ToList();
            ballot.TieBreakQuestionAnswers = ballot.TieBreakQuestionAnswers.OrderBy(x => x.Question.Number).ToList();
        }

        var countingCircle = await _countingCircleRepo.Query()
            .AsSplitQuery()
            .Include(x => x.ResponsibleAuthority)
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ContactPersonAfterEvent)
            .FirstOrDefaultAsync(x => x.SnapshotContestId == ctx.ContestId && x.BasisCountingCircleId == ctx.BasisCountingCircleId, ct)
            ?? throw new EntityNotFoundException(nameof(CountingCircle), new { ctx.ContestId, ctx.BasisCountingCircleId });

        var pdfCountingCircle = _mapper.Map<PdfCountingCircle>(countingCircle);
        var pdfBundle = _mapper.Map<PdfVoteResultBundle>(bundle);
        foreach (var pdfBallotResult in pdfBundle.Ballots)
        {
            PdfVoteUtil.SetLabels(
                pdfBallotResult,
                x => x.QuestionAnswers.Select(a => a.Question!),
                x => x.TieBreakQuestionAnswers.Select(a => a.Question!));
        }

        var bundleReview = new PdfPoliticalBusinessResultBundleReview
        {
            TemplateKey = ctx.Template.Key,
            GeneratedAt = _clock.UtcNow.ConvertUtcTimeToSwissTime(),
            CountingCircle = pdfCountingCircle,
            VoteResultBundle = pdfBundle,
            PoliticalBusiness = _mapper.Map<PdfPoliticalBusiness>(bundle.BallotResult.VoteResult.Vote),
        };

        return await _templateService.RenderToPdf(ctx, bundleReview, bundle.Number.ToString());
    }
}
