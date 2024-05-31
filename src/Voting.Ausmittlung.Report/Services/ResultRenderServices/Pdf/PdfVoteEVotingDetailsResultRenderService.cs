// (c) Copyright 2024 by Abraxas Informatik AG
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

public class PdfVoteEVotingDetailsResultRenderService : IRendererService
{
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly VoteDomainOfInfluenceResultBuilder _voteDoiResultBuilder;
    private readonly IMapper _mapper;
    private readonly TemplateService _templateService;
    private readonly IClock _clock;

    public PdfVoteEVotingDetailsResultRenderService(
        IDbRepository<DataContext, Vote> voteRepo,
        IMapper mapper,
        TemplateService templateService,
        IDbRepository<DataContext, Contest> contestRepo,
        VoteDomainOfInfluenceResultBuilder voteDoiResultBuilder,
        IClock clock)
    {
        _voteRepo = voteRepo;
        _mapper = mapper;
        _templateService = templateService;
        _contestRepo = contestRepo;
        _voteDoiResultBuilder = voteDoiResultBuilder;
        _clock = clock;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence.Details!.VotingCards)
            .FirstOrDefaultAsync(x => x.Id == ctx.ContestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), ctx.ContestId);

        var votes = await _voteRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Results.Where(r => r.CountingCircle.ContestDetails.Any(cd => cd.EVoting)))
            .ThenInclude(x => x.Results.OrderBy(b => b.Ballot.Position))
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
            .Include(x => x.Results)
            .ThenInclude(x => x.Results)
            .ThenInclude(x => x.Ballot.BallotQuestions.OrderBy(q => q.Number))
            .ThenInclude(x => x.Translations)
            .Include(x => x.Results)
            .ThenInclude(x => x.Results)
            .ThenInclude(x => x.Ballot.TieBreakQuestions.OrderBy(q => q.Number))
            .ThenInclude(x => x.Translations)
            .Include(x => x.Results)
            .ThenInclude(x => x.CountingCircle)
            .ThenInclude(x => x.ContestDetails)
            .ThenInclude(x => x.VotingCards)
            .Include(x => x.EndResult!.BallotEndResults.OrderBy(b => b.Ballot.Position)).ThenInclude(x => x.Ballot)
            .Include(x => x.EndResult!.BallotEndResults).ThenInclude(x => x.QuestionEndResults.OrderBy(q => q.Question.Number)).ThenInclude(x => x.Question.Translations)
            .Include(x => x.EndResult!.BallotEndResults).ThenInclude(x => x.TieBreakQuestionEndResults.OrderBy(q => q.Question.Number)).ThenInclude(x => x.Question.Translations)
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence)
            .Where(x => ctx.PoliticalBusinessIds.Contains(x.Id))
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .ToListAsync(ct);

        contest.DomainOfInfluence.Details!.OrderVotingCardsAndSubTotals();
        var pdfContest = _mapper.Map<PdfContest>(contest);
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(pdfContest.DomainOfInfluence!.Details!, contest.DomainOfInfluence.Type);

        var pdfVotes = new List<PdfVote>();
        foreach (var vote in votes)
        {
            var pdfVote = _mapper.Map<PdfVote>(vote);
            var countingCirclesById = vote.Results.ToDictionary(x => x.CountingCircleId, x => x.CountingCircle);

            if (pdfVote.Results?.Any() != true)
            {
                continue;
            }

            foreach (var result in pdfVote.Results ?? Enumerable.Empty<PdfVoteResult>())
            {
                if (countingCirclesById.TryGetValue(result.CountingCircle!.Id, out var cc) && cc.ContestDetails.Any())
                {
                    result.CountingCircle = _mapper.Map<PdfCountingCircle>(cc);
                    var ccDetails = cc.ContestDetails.FirstOrDefault();
                    ccDetails?.OrderVotingCardsAndSubTotals();

                    result.CountingCircle.ContestCountingCircleDetails = _mapper.Map<PdfContestCountingCircleDetails>(ccDetails);
                    PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(result.CountingCircle.ContestCountingCircleDetails, vote.DomainOfInfluence.Type);
                }
            }

            pdfVotes.Add(pdfVote);
        }

        PdfVoteUtil.SetLabels(pdfVotes);

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            Contest = pdfContest,
            Votes = pdfVotes,
        };

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            PdfDateUtil.BuildDateForFilename(templateBag.Contest.Date));
    }
}
