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
            .FirstOrDefaultAsync(x => x.Id == ctx.ContestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), ctx.ContestId);

        var votes = await _voteRepo.Query()
            .AsSplitQuery()
            .Include(x => x.EndResult!.BallotEndResults.OrderBy(b => b.Ballot.Position)).ThenInclude(x => x.Ballot.Translations)
            .Include(x => x.EndResult!.BallotEndResults).ThenInclude(x => x.QuestionEndResults.OrderBy(q => q.Question.Number)).ThenInclude(x => x.Question.Translations)
            .Include(x => x.EndResult!.BallotEndResults).ThenInclude(x => x.TieBreakQuestionEndResults.OrderBy(q => q.Question.Number)).ThenInclude(x => x.Question.Translations)
            .Include(x => x.Translations)
            .Include(x => x.DomainOfInfluence.Details!.VotingCards)
            .Where(x => ctx.PoliticalBusinessIds.Contains(x.Id) && x.DomainOfInfluence.Type == ctx.DomainOfInfluenceType)
            .OrderBy(x => x.PoliticalBusinessNumber)
            .ToListAsync(ct);

        // Bugfix for VOTING-2833, where the protocol expects ContestDetails, even though that is not correct
        // For example, when ctx.DomainOfInfluenceType is a communal type (ex. municipality), the ContestDetails do not work.
        // It contains the voting cards for all counting circles in the contest, but we only want the voting cards for the
        // domain of influence of the votes.
        // This should be fixed in the future by no longer sending the contest details, but individual domain of influence details.
        contest.Details = BuildContestDetails(votes);

        contest.Details?.OrderVotingCardsAndSubTotals();
        var pdfContest = _mapper.Map<PdfContest>(contest);
        pdfContest.Details ??= new PdfContestDetails();
        PdfBaseDetailsUtil.FilterAndBuildVotingCardTotals(pdfContest.Details, ctx.DomainOfInfluenceType);

        var pdfVotes = _mapper.Map<List<PdfVote>>(votes);
        PdfVoteUtil.SetLabels(pdfVotes);

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
            PdfDomainOfInfluenceUtil.MapDomainOfInfluenceType(ctx.DomainOfInfluenceType),
            PdfDateUtil.BuildDateForFilename(_clock.UtcNow));
    }

    private ContestDetails? BuildContestDetails(IReadOnlyCollection<Vote> votes)
    {
        var domainOfInfluences = votes
            .Select(x => x.DomainOfInfluence)
            .DistinctBy(x => x.Id)
            .ToList();
        if (domainOfInfluences.Count > 1)
        {
            throw new ValidationException("Cannot export votes with more than one domain of influence");
        }

        var domainOfInfluence = domainOfInfluences[0];

        var contestDetails = domainOfInfluence.Details == null
            ? null
            : new ContestDetails
            {
                TotalCountOfVoters = domainOfInfluence.Details.TotalCountOfVoters,
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
                        Sex = x.Sex,
                        VoterType = x.VoterType,
                        CountOfVoters = x.CountOfVoters,
                    }).ToList(),
            };

        // do not need this in the report, since we fill out the contest details for the time being
        foreach (var vote in votes)
        {
            vote.DomainOfInfluence.Details = null;
        }

        return contestDetails;
    }
}
