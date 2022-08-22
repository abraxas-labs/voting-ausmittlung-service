// (c) Copyright 2022 by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Xml;

public class XmlVoteEch0222RenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Vote> _voteRepo;
    private readonly MultiLanguageTranslationUtil _multiLanguageTranslationUtil;
    private readonly Ech0222Serializer _ech0222Serializer;

    public XmlVoteEch0222RenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Vote> voteRepo,
        MultiLanguageTranslationUtil multiLanguageTranslationUtil,
        Ech0222Serializer ech0222Serializer)
    {
        _templateService = templateService;
        _voteRepo = voteRepo;
        _multiLanguageTranslationUtil = multiLanguageTranslationUtil;
        _ech0222Serializer = ech0222Serializer;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var vote = await _voteRepo.Query()
            .AsSplitQuery()
            .IgnoreQueryFilters() // eCH exports need all languages, do not filter them
            .Include(x => x.Translations.OrderBy(t => t.Language))
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Contest.Translations.OrderBy(t => t.Language))
            .Include(x => x.Contest.DomainOfInfluence)
            .Include(x => x.Results).ThenInclude(x => x.CountingCircle)
            .Include(x => x.Results).ThenInclude(x => x.Results).ThenInclude(x => x.Bundles).ThenInclude(x => x.Ballots).ThenInclude(x => x.QuestionAnswers).ThenInclude(x => x.Question)
            .Include(x => x.Results).ThenInclude(x => x.Results).ThenInclude(x => x.Bundles).ThenInclude(x => x.Ballots).ThenInclude(x => x.TieBreakQuestionAnswers).ThenInclude(x => x.Question)
            .FirstOrDefaultAsync(x => x.Id == ctx.PoliticalBusinessId, ct)
            ?? throw new EntityNotFoundException(nameof(Vote), ctx.PoliticalBusinessId);

        var eventDelivery = _ech0222Serializer.ToDelivery(vote);
        var voteShortDescription = _multiLanguageTranslationUtil.GetShortDescription(vote);
        return _templateService.RenderToXml(ctx, eventDelivery.DeliveryHeader.MessageId, eventDelivery, voteShortDescription);
    }
}
