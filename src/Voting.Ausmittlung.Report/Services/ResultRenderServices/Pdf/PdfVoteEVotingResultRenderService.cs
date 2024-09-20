// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfVoteEVotingResultRenderService : IRendererService
{
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly VoteDomainOfInfluenceResultBuilder _voteDoiResultBuilder;
    private readonly IMapper _mapper;
    private readonly TemplateService _templateService;
    private readonly IClock _clock;

    public PdfVoteEVotingResultRenderService(
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
            .Include(x => x.DomainOfInfluence)
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
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Contest.CantonDefaults)
            .Where(x => ctx.PoliticalBusinessIds.Contains(x.Id))
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .ToListAsync(ct);

        var pdfVotes = new List<PdfVote>();
        foreach (var vote in votes)
        {
            var pdfVote = _mapper.Map<PdfVote>(vote);
            var doiResults = BuildResultsGroupedByBallot(vote);

            pdfVote.DomainOfInfluenceBallotResults = _mapper.Map<List<PdfVoteBallotDomainOfInfluenceResult>>(doiResults);
            pdfVote.Results = null; // not needed in this protocol
            pdfVotes.Add(pdfVote);
        }

        PdfVoteUtil.SetLabels(pdfVotes);

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            Contest = _mapper.Map<PdfContest>(contest),
            Votes = pdfVotes,
        };

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            PdfDateUtil.BuildDateForFilename(templateBag.Contest.Date));
    }

    private IEnumerable<IGrouping<Ballot, VoteDomainOfInfluenceBallotResult>> BuildResultsGroupedByBallot(Vote vote)
    {
        var aggregatedResult = AggregateResults(vote);
        _voteDoiResultBuilder.MapContestDetails(aggregatedResult);
        var groupedResults = aggregatedResult.BallotResults
            .GroupBy(x => x.Ballot, x => x, VoteDomainOfInfluenceResultBuilder.BallotComparer)
            .OrderBy(x => x.Key.Position);
        return groupedResults;
    }

    private VoteDomainOfInfluenceResult AggregateResults(Vote vote)
    {
        var aggregatedResult = new VoteDomainOfInfluenceResult();

        vote.Results = vote
            .Results
            .OrderByCountingCircle(x => x.CountingCircle, vote.Contest.CantonDefaults)
            .ToList();

        var pbType = vote.DomainOfInfluence.Type;

        foreach (var ccResult in vote.Results)
        {
            var ccDetail = ccResult.CountingCircle.ContestDetails.FirstOrDefault();
            if (ccDetail != null)
            {
                _voteDoiResultBuilder.ApplyContestCountingCircleDetail(aggregatedResult, ccDetail, pbType);
            }

            _voteDoiResultBuilder.ApplyVoteCountingCircleResult(aggregatedResult, ccResult);
        }

        return aggregatedResult;
    }
}
