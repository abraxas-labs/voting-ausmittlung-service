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
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf;

public class PdfVoteDomainOfInfluenceResultRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Vote> _repo;
    private readonly IMapper _mapper;
    private readonly VoteDomainOfInfluenceResultBuilder _doiResultBuilder;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;
    private readonly IClock _clock;

    public PdfVoteDomainOfInfluenceResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Vote> repo,
        IMapper mapper,
        VoteDomainOfInfluenceResultBuilder doiResultBuilder,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        IClock clock)
    {
        _templateService = templateService;
        _repo = repo;
        _mapper = mapper;
        _doiResultBuilder = doiResultBuilder;
        _ccDetailsRepo = ccDetailsRepo;
        _clock = clock;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var vote = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.EndResult!.BallotEndResults)
                .ThenInclude(x => x.Ballot)
            .Include(x => x.EndResult!.BallotEndResults)
                .ThenInclude(x => x.QuestionEndResults)
                    .ThenInclude(x => x.Question)
                        .ThenInclude(x => x.Translations)
            .Include(x => x.EndResult!.BallotEndResults)
                .ThenInclude(x => x.TieBreakQuestionEndResults)
                    .ThenInclude(x => x.Question)
                        .ThenInclude(x => x.Translations)
            .Include(x => x.Results).ThenInclude(x => x.Results).ThenInclude(x => x.Ballot.Translations)
            .Include(x => x.Results).ThenInclude(x => x.Results).ThenInclude(x => x.Ballot.BallotQuestions)
                .ThenInclude(x => x.Translations)
            .Include(x => x.Results).ThenInclude(x => x.Results).ThenInclude(x => x.Ballot.TieBreakQuestions)
                .ThenInclude(x => x.Translations)
            .Include(x => x.Results).ThenInclude(x => x.Results).ThenInclude(x => x.QuestionResults)
                .ThenInclude(x => x.Question.Translations)
            .Include(x => x.Results).ThenInclude(x => x.Results).ThenInclude(x => x.TieBreakQuestionResults)
                .ThenInclude(x => x.Question.Translations)
            .Include(x => x.Results).ThenInclude(x => x.CountingCircle)
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence.Details!.VotingCards)
            .Include(x => x.Contest.Translations)
            .Include(x => x.Contest.DomainOfInfluence)
            .FirstOrDefaultAsync(v => v.Id == ctx.PoliticalBusinessId, ct)
            ?? throw new ValidationException($"invalid data requested: {nameof(ctx.PoliticalBusinessId)}: {ctx.PoliticalBusinessId}");

        vote.EndResult!.OrderBallotResults();

        var ccDetailsList = await _ccDetailsRepo.Query()
            .Include(x => x.VotingCards)
            .Where(x => x.ContestId == vote.ContestId)
            .ToListAsync(ct);

        var (doiResults, notAssignableResult, aggregatedResult) = await _doiResultBuilder.BuildResultsGroupedByBallot(vote, ccDetailsList);
        var ballots = vote.EndResult.BallotEndResults.Select(x => x.Ballot).ToList();

        // we don't need this data in the xml
        vote.Results = new List<VoteResult>();

        var pdfCcDetails = _mapper.Map<List<PdfContestCountingCircleDetails>>(ccDetailsList);

        foreach (var details in pdfCcDetails)
        {
            PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(details, vote.DomainOfInfluence.Type);

            // we don't need this data in the xml
            details.VotingCards = new List<PdfVotingCardResultDetail>();
        }

        var pdfVote = _mapper.Map<PdfVote>(vote);
        pdfVote.DomainOfInfluenceBallotResults = _mapper.Map<List<PdfVoteBallotDomainOfInfluenceResult>>(doiResults);

        // only show cc results in election which are not included in doi results (ex: reporting level 1 and cc Auslandschweizer)
        var doiResultsByBallotId = pdfVote.DomainOfInfluenceBallotResults.ToDictionary(x => x.Ballot!.Id);
        var notAssignableBallotResultsByBallotId = notAssignableResult.BallotResults.ToDictionary(x => x.Ballot.Id);

        foreach (var ballot in ballots)
        {
            if (!doiResultsByBallotId.TryGetValue(ballot.Id, out var doiBallotResult))
            {
                doiBallotResult = new PdfVoteBallotDomainOfInfluenceResult { Ballot = _mapper.Map<PdfBallot>(ballot) };
                pdfVote.DomainOfInfluenceBallotResults.Add(doiBallotResult);
            }

            if (notAssignableBallotResultsByBallotId.TryGetValue(ballot.Id, out var notAssignableBallotResult))
            {
                doiBallotResult.NotAssignableResult = _mapper.Map<PdfVoteDomainOfInfluenceBallotResult>(notAssignableBallotResult);
                doiBallotResult.NotAssignableResult.DomainOfInfluence = null;
            }

            if (aggregatedResult.ResultsByBallotId.TryGetValue(ballot.Id, out var aggregatedBallotResult))
            {
                doiBallotResult.AggregatedResult = _mapper.Map<PdfVoteDomainOfInfluenceBallotResult>(aggregatedBallotResult);
                doiBallotResult.AggregatedResult.Results = new();
            }
        }

        var pdfCcResults = pdfVote.DomainOfInfluenceBallotResults
            .SelectMany(x => x.Results)
            .Concat(pdfVote.DomainOfInfluenceBallotResults.Where(x => x.NotAssignableResult != null).Select(x => x.NotAssignableResult!))
            .SelectMany(x => x.Results)
            .ToList();

        PdfCountingCircleResultUtil.MapContestCountingCircleDetailsToResults(pdfCcDetails, pdfCcResults);
        PdfCountingCircleResultUtil.RemoveContactPersonDetails(pdfCcResults);

        // reset the domain of influence on the result, since this is a single domain of influence report
        var domainOfInfluence = pdfVote.DomainOfInfluence;
        domainOfInfluence!.Details ??= new PdfContestDomainOfInfluenceDetails();
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(domainOfInfluence.Details, vote.DomainOfInfluence.Type);

        // we don't need this data in the xml
        domainOfInfluence.Details.VotingCards = new List<PdfVotingCardResultDetail>();
        pdfVote.DomainOfInfluence = null;

        var contest = _mapper.Map<PdfContest>(vote.Contest);

        PdfVoteUtil.SetLabels(pdfVote);

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            Contest = contest,
            DomainOfInfluence = domainOfInfluence,
            Votes = new List<PdfVote>
            {
                pdfVote,
            },
        };

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            PdfDomainOfInfluenceUtil.MapDomainOfInfluenceType(vote.DomainOfInfluence.Type),
            vote.ShortDescription,
            PdfDateUtil.BuildDateForFilename(_clock.UtcNow));
    }
}
