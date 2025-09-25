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
using Voting.Ausmittlung.Data.Utils;
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
    private readonly IDbRepository<DataContext, VoteResult> _resultRepo;
    private readonly IMapper _mapper;
    private readonly VoteDomainOfInfluenceResultBuilder _doiResultBuilder;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;
    private readonly IClock _clock;

    public PdfVoteDomainOfInfluenceResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Vote> repo,
        IDbRepository<DataContext, VoteResult> resultRepo,
        IMapper mapper,
        VoteDomainOfInfluenceResultBuilder doiResultBuilder,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo,
        IClock clock)
    {
        _templateService = templateService;
        _repo = repo;
        _resultRepo = resultRepo;
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
                .ThenInclude(x => x.Ballot.Translations)
            .Include(x => x.EndResult!.BallotEndResults)
                .ThenInclude(x => x.QuestionEndResults.OrderBy(q => q.Question.Number))
                    .ThenInclude(x => x.Question.Translations)
            .Include(x => x.EndResult!.BallotEndResults)
                .ThenInclude(x => x.TieBreakQuestionEndResults.OrderBy(q => q.Question.Number))
                    .ThenInclude(x => x.Question.Translations)
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence.Details!.VotingCards)
            .Include(x => x.DomainOfInfluence.Details!.CountOfVotersInformationSubTotals)
            .Include(x => x.Contest.Translations)
            .Include(x => x.Contest.DomainOfInfluence)
            .Include(x => x.Contest.CantonDefaults)
            .FirstOrDefaultAsync(v => v.Id == ctx.PoliticalBusinessId, ct)
            ?? throw new ValidationException($"invalid data requested: {nameof(ctx.PoliticalBusinessId)}: {ctx.PoliticalBusinessId}");

        var isPartialResult = vote.DomainOfInfluence.SecureConnectId != ctx.TenantId;
        if (isPartialResult && ctx.ViewablePartialResultsCountingCircleIds?.Count == 0)
        {
            throw new ValidationException("invalid partial result without any viewable counting circle ids");
        }

        var results = await _resultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle.ContestDetails)
                .ThenInclude(x => x.VotingCards)
            .Include(x => x.CountingCircle.ContestDetails)
                .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.Results)
                .ThenInclude(x => x.Ballot.Translations)
            .Include(x => x.Results)
                .ThenInclude(x => x.Ballot.BallotQuestions.OrderBy(q => q.Number))
                    .ThenInclude(x => x.Translations)
            .Include(x => x.Results)
                .ThenInclude(x => x.Ballot.TieBreakQuestions.OrderBy(q => q.Number))
                    .ThenInclude(x => x.Translations)
            .Include(x => x.Results)
                .ThenInclude(x => x.QuestionResults.OrderBy(q => q.Question.Number))
                    .ThenInclude(x => x.Question.Translations)
            .Include(x => x.Results)
                .ThenInclude(x => x.TieBreakQuestionResults.OrderBy(q => q.Question.Number))
                    .ThenInclude(x => x.Question.Translations)
            .Where(x => x.VoteId == ctx.PoliticalBusinessId && (!isPartialResult || ctx.ViewablePartialResultsCountingCircleIds!.Contains(x.CountingCircleId)))
            .ToListAsync(ct);

        if (results.Count == 0)
        {
            throw new ValidationException($"no results found for: {nameof(ctx.PoliticalBusinessId)}: {ctx.PoliticalBusinessId}");
        }

        var ccDetailsList = await _ccDetailsRepo.Query()
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Where(x => x.ContestId == vote.ContestId)
            .ToListAsync(ct);

        PdfCountingCircleResultUtil.ResetResultsIfNotDone(results, ccDetailsList);

        vote.Results = results;
        vote.MoveECountingToConventional();

        if (isPartialResult)
        {
            vote.EndResult = PartialEndResultUtils.MergeIntoPartialEndResult(vote, results);
        }

        vote.EndResult!.OrderVotingCardsAndSubTotals();
        vote.EndResult!.OrderBallotResults();

        var (doiResults, notAssignableResult, aggregatedResult) = await _doiResultBuilder.BuildResultsGroupedByBallot(
                vote,
                ccDetailsList,
                ctx.TenantId ?? vote.DomainOfInfluence.SecureConnectId,
                isPartialResult ? ctx.ViewablePartialResultsCountingCircleIds : null);

        var ballots = vote.EndResult.BallotEndResults.Select(x => x.Ballot).ToList();

        // we don't need this data in the xml
        vote.Results = new List<VoteResult>();

        var pdfCcDetails = _mapper.Map<List<PdfContestCountingCircleDetails>>(ccDetailsList);

        foreach (var details in pdfCcDetails)
        {
            PdfBaseDetailsUtil.FilterAndBuildVotingCardTotalsAndCountOfVoters(details, vote.DomainOfInfluence);

            // we don't need this data in the xml
            details.VotingCards = new List<PdfVotingCardResultDetail>();
            details.CountOfVotersInformationSubTotals = new List<PdfCountOfVotersInformationSubTotal>();
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
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotalsAndCountOfVoters(domainOfInfluence.Details, vote.DomainOfInfluence);

        // we don't need this data in the xml
        domainOfInfluence.Details.VotingCards = new List<PdfVotingCardResultDetail>();
        domainOfInfluence.Details.CountOfVotersInformationSubTotals = new List<PdfCountOfVotersInformationSubTotal>();
        pdfVote.DomainOfInfluence = null;

        var contest = _mapper.Map<PdfContest>(vote.Contest);

        PdfVoteUtil.SetLabels(pdfVote);

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            GeneratedAt = _clock.UtcNow.ConvertUtcTimeToSwissTime(),
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
            PdfDateUtil.BuildDateForFilename(templateBag.Contest.Date));
    }
}
