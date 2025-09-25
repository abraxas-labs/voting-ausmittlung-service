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
    private readonly IClock _clock;
    private readonly IDbRepository<DataContext, VoteResult> _voteResultRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IMapper _mapper;

    public PdfVoteEndResultRenderService(
        TemplateService templateService,
        IMapper mapper,
        IDbRepository<DataContext, Contest> contestRepo,
        IDbRepository<DataContext, VoteResult> voteResultRepo,
        IDbRepository<DataContext, Vote> voteRepo,
        IClock clock)
    {
        _templateService = templateService;
        _mapper = mapper;
        _contestRepo = contestRepo;
        _voteResultRepo = voteResultRepo;
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
            .FirstOrDefaultAsync(x => x.Id == ctx.ContestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), ctx.ContestId);

        var votes = await _voteRepo.Query()
            .AsSplitQuery()
            .Include(x => x.EndResult!.BallotEndResults.OrderBy(b => b.Ballot.Position)).ThenInclude(x => x.Ballot.Translations)
            .Include(x => x.EndResult!.BallotEndResults).ThenInclude(x => x.QuestionEndResults.OrderBy(q => q.Question.Number)).ThenInclude(x => x.Question.Translations)
            .Include(x => x.EndResult!.BallotEndResults).ThenInclude(x => x.TieBreakQuestionEndResults.OrderBy(q => q.Question.Number)).ThenInclude(x => x.Question.Translations)
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence.Details!.VotingCards)
            .Include(x => x.DomainOfInfluence.Details!.CountOfVotersInformationSubTotals)
            .Where(x => ctx.PoliticalBusinessIds.Contains(x.Id) && x.DomainOfInfluence.BasisDomainOfInfluenceId == ctx.BasisDomainOfInfluenceId)
            .OrderBy(x => x.PoliticalBusinessNumber)
            .ToListAsync(ct);

        if (votes.Count == 0)
        {
            throw new ValidationException("Cannot export this report with zero votes");
        }

        // Bugfix for VOTING-2833, where the protocol expects ContestDetails, even though that is not correct
        // This should be fixed in the future by no longer sending the contest details, but the domain of influence detail.
        var domainOfInfluence = votes[0].DomainOfInfluence;
        contest.Details = BuildContestDetails(domainOfInfluence);

        // All votes are from the same domain of influence, so all or none are partial results.
        var isPartialResult = votes[0].DomainOfInfluence.SecureConnectId != ctx.TenantId;

        if (isPartialResult)
        {
            var voteIds = votes.ConvertAll(v => v.Id);
            var results = await _voteResultRepo.Query()
                .AsSplitQuery()
                .Include(x => x.CountingCircle.ContestDetails)
                    .ThenInclude(x => x.VotingCards)
                .Include(x => x.CountingCircle.ContestDetails)
                    .ThenInclude(x => x.CountOfVotersInformationSubTotals)
                .Include(x => x.Results)
                    .ThenInclude(x => x.Ballot.Translations)
                .Include(x => x.Results)
                    .ThenInclude(x => x.QuestionResults.OrderBy(q => q.Question.Number))
                        .ThenInclude(x => x.Question.Translations)
                .Include(x => x.Results)
                    .ThenInclude(x => x.TieBreakQuestionResults.OrderBy(q => q.Question.Number))
                    .ThenInclude(x => x.Question.Translations)
                .Where(x => voteIds.Contains(x.VoteId) && ctx.ViewablePartialResultsCountingCircleIds!.Contains(x.CountingCircleId))
                .ToListAsync(ct);

            var allCcDetails = results
                .SelectMany(r => r.CountingCircle.ContestDetails)
                .DistinctBy(c => c.CountingCircleId)
                .ToList();
            PdfCountingCircleResultUtil.ResetResultsIfNotDone(results, allCcDetails);

            var resultsByVoteId = results
                .GroupBy(x => x.VoteId)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var vote in votes)
            {
                _ = resultsByVoteId.GetValueOrDefault(vote.Id)
                    ?? throw new ValidationException($"no results found for: {nameof(ctx.PoliticalBusinessId)}: {vote.Id}");

                vote.EndResult = PartialEndResultUtils.MergeIntoPartialEndResult(
                    vote,
                    results);
            }

            contest.Details = AggregatedContestCountingCircleDetailsBuilder.BuildContestDetails(results
                .SelectMany(x => x.CountingCircle!.ContestDetails)
                .DistinctBy(x => x.CountingCircleId)
                .ToList());
        }

        foreach (var vote in votes)
        {
            vote.MoveECountingToConventional();
        }

        contest.Details?.OrderVotingCardsAndSubTotals();
        var pdfContest = _mapper.Map<PdfContest>(contest);
        pdfContest.Details ??= new PdfContestDetails();
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotalsAndCountOfVoters(pdfContest.Details, domainOfInfluence);
        pdfContest.Details.CountOfVotersInformationSubTotals = null!;

        PdfCountingCircle? countingCircle = null;
        if (votes[0].EndResult!.TotalCountOfCountingCircles == 1)
        {
            var cc = await _voteResultRepo.Query()
                .Include(x => x.CountingCircle)
                    .ThenInclude(x => x.ResponsibleAuthority)
                .Include(x => x.CountingCircle)
                    .ThenInclude(x => x.ContactPersonDuringEvent)
                .Include(x => x.CountingCircle)
                    .ThenInclude(x => x.ContactPersonAfterEvent)
                .Include(x => x.CountingCircle)
                    .ThenInclude(x => x.ContestDetails)
                .Where(x => x.VoteId == votes[0].Id)
                .Select(x => x.CountingCircle)
                .FirstAsync(ct);
            pdfContest.Details.CountingMachine = cc.ContestDetails.First().CountingMachine;
            countingCircle = _mapper.Map<PdfCountingCircle>(cc);
        }

        // Do not need this in the report, since we fill out the contest details for the time being
        foreach (var vote in votes)
        {
            vote.DomainOfInfluence.Details = null;
        }

        var pdfVotes = _mapper.Map<List<PdfVote>>(votes);
        PdfVoteUtil.SetLabels(pdfVotes);

        var templateBag = new PdfTemplateBag
        {
            TemplateKey = ctx.Template.Key,
            GeneratedAt = _clock.UtcNow.ConvertUtcTimeToSwissTime(),
            Contest = pdfContest,
            Votes = pdfVotes,
            DomainOfInfluenceType = domainOfInfluence.Type,
            CountingCircle = countingCircle,
        };

        return await _templateService.RenderToPdf(
            ctx,
            templateBag,
            votes[0].DomainOfInfluence.ShortName,
            PdfDateUtil.BuildDateForFilename(templateBag.Contest.Date));
    }

    private ContestDetails? BuildContestDetails(DomainOfInfluence domainOfInfluence)
    {
        return domainOfInfluence.Details == null
            ? null
            : new ContestDetails
            {
                VotingCards = domainOfInfluence.Details.VotingCards.Select(x =>
                    new ContestVotingCardResultDetail
                    {
                        DomainOfInfluenceType = x.DomainOfInfluenceType,
                        Channel = x.Channel,
                        Valid = x.Valid,
                        CountOfReceivedVotingCards = x.CountOfReceivedVotingCards,
                    }).ToList(),
                CountOfVotersInformationSubTotals = domainOfInfluence.Details.CountOfVotersInformationSubTotals.Select(x =>
                    new ContestCountOfVotersInformationSubTotal
                    {
                        DomainOfInfluenceType = x.DomainOfInfluenceType,
                        Sex = x.Sex,
                        VoterType = x.VoterType,
                        CountOfVoters = x.CountOfVoters,
                    }).ToList(),
            };
    }
}
