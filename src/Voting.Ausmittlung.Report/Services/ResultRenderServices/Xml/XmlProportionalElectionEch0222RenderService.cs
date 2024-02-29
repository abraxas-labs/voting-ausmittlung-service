// (c) Copyright 2024 by Abraxas Informatik AG
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

public class XmlProportionalElectionEch0222RenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, ProportionalElection> _electionRepo;
    private readonly MultiLanguageTranslationUtil _multiLanguageTranslationUtil;
    private readonly Ech0222Serializer _ech0222Serializer;

    public XmlProportionalElectionEch0222RenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElection> electionRepo,
        MultiLanguageTranslationUtil multiLanguageTranslationUtil,
        Ech0222Serializer ech0222Serializer)
    {
        _templateService = templateService;
        _electionRepo = electionRepo;
        _multiLanguageTranslationUtil = multiLanguageTranslationUtil;
        _ech0222Serializer = ech0222Serializer;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var election = await _electionRepo.Query()
            .AsSplitQuery()
            .IgnoreQueryFilters() // eCH exports need all languages, do not filter them
            .Include(me => me.Translations.OrderBy(t => t.Language))
            .Include(me => me.DomainOfInfluence)
            .Include(me => me.Contest.Translations.OrderBy(t => t.Language))
            .Include(me => me.Contest.DomainOfInfluence)
            .Include(me => me.Results).ThenInclude(r => r.CountingCircle)
            .Include(me => me.Results).ThenInclude(r => r.Bundles).ThenInclude(b => b.Ballots).ThenInclude(b => b.BallotCandidates).ThenInclude(r => r.Candidate)
            .Include(me => me.Results).ThenInclude(r => r.UnmodifiedListResults).ThenInclude(r => r.List).ThenInclude(r => r.ProportionalElectionCandidates)
            .FirstOrDefaultAsync(me => me.Id == ctx.PoliticalBusinessId, ct)
            ?? throw new EntityNotFoundException(nameof(ProportionalElection), ctx.PoliticalBusinessId);

        var eventDelivery = _ech0222Serializer.ToDelivery(election);
        var electionShortDescription = _multiLanguageTranslationUtil.GetShortDescription(election);
        return _templateService.RenderToXml(ctx, eventDelivery.DeliveryHeader.MessageId, eventDelivery, electionShortDescription);
    }
}
