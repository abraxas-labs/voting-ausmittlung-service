// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Converters;
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Ech.Ech0110_4_0.Schemas;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Xml;

public class XmlVoteEch0110RenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly MultiLanguageTranslationUtil _multiLanguageTranslationUtil;
    private readonly Ech0110Serializer _ech0110Serializer;

    public XmlVoteEch0110RenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Vote> voteRepo,
        MultiLanguageTranslationUtil multiLanguageTranslationUtil,
        Ech0110Serializer ech0110Serializer)
    {
        _templateService = templateService;
        _voteRepo = voteRepo;
        _multiLanguageTranslationUtil = multiLanguageTranslationUtil;
        _ech0110Serializer = ech0110Serializer;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var vote = await _voteRepo.Query()
            .AsSplitQuery()
            .IgnoreQueryFilters() // eCH exports need all languages, do not filter them
            .Include(v => v.Translations.OrderBy(t => t.Language))
            .Include(v => v.DomainOfInfluence)
            .Include(v => v.Contest.Translations.OrderBy(t => t.Language))
            .Include(v => v.Contest.DomainOfInfluence)
            .Include(v => v.Contest.CountingCircleDetails).ThenInclude(ccd => ccd.VotingCards)
            .Include(x => x.Ballots)
            .Include(x => x.Ballots).ThenInclude(x => x.BallotQuestions).ThenInclude(x => x.Translations.OrderBy(t => t.Language))
            .Include(x => x.Ballots).ThenInclude(x => x.TieBreakQuestions).ThenInclude(x => x.Translations.OrderBy(t => t.Language))
            .Include(v => v.Results).ThenInclude(r => r.CountingCircle)
            .Include(v => v.Results).ThenInclude(r => r.Results).ThenInclude(br => br.CountOfVoters)
            .Include(v => v.Results).ThenInclude(r => r.Results).ThenInclude(br => br.QuestionResults)
            .Include(v => v.Results).ThenInclude(r => r.Results).ThenInclude(br => br.TieBreakQuestionResults)
            .FirstOrDefaultAsync(v => v.Id == ctx.PoliticalBusinessId, ct)
            ?? throw new EntityNotFoundException(nameof(Vote), ctx.PoliticalBusinessId);
        vote.MoveECountingToConventional();

        var ballotsById = vote.Ballots.ToDictionary(x => x.Id);

        foreach (var ballotResult in vote.Results.SelectMany(r => r.Results))
        {
            ballotResult.Ballot = ballotsById[ballotResult.BallotId];

            var questionsById = ballotResult.Ballot.BallotQuestions.ToDictionary(x => x.Id);
            var tieBreakQuestionsById = ballotResult.Ballot.TieBreakQuestions.ToDictionary(x => x.Id);

            foreach (var questionResult in ballotResult.QuestionResults)
            {
                questionResult.Question = questionsById[questionResult.QuestionId];
            }

            foreach (var tieBreakQuestionResult in ballotResult.TieBreakQuestionResults)
            {
                tieBreakQuestionResult.Question = tieBreakQuestionsById[tieBreakQuestionResult.QuestionId];
            }
        }

        var eventDelivery = _ech0110Serializer.ToDelivery(vote);
        var voteShortDescription = _multiLanguageTranslationUtil.GetShortDescription(vote);
        return _templateService.RenderToXml(
            ctx,
            eventDelivery.DeliveryHeader.MessageId,
            eventDelivery,
            Ech0110Schemas.LoadEch0110Schemas(),
            voteShortDescription);
    }
}
