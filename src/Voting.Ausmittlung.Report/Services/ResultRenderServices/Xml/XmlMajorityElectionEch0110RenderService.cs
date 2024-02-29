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

public class XmlMajorityElectionEch0110RenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElection> _electionRepo;
    private readonly MultiLanguageTranslationUtil _multiLanguageTranslationUtil;
    private readonly Ech0110Serializer _ech0110Serializer;

    public XmlMajorityElectionEch0110RenderService(
        TemplateService templateService,
        IDbRepository<DataContext, MajorityElection> electionRepo,
        MultiLanguageTranslationUtil multiLanguageTranslationUtil,
        Ech0110Serializer ech0110Serializer)
    {
        _templateService = templateService;
        _electionRepo = electionRepo;
        _multiLanguageTranslationUtil = multiLanguageTranslationUtil;
        _ech0110Serializer = ech0110Serializer;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var election = await _electionRepo.Query()
            .AsSplitQuery()
            .IgnoreQueryFilters() // eCH exports need all languages, do not filter them
            .Include(me => me.Translations.OrderBy(x => x.Language))
            .Include(me => me.Contest.Translations.OrderBy(x => x.Language))
            .Include(me => me.Contest.DomainOfInfluence)
            .Include(me => me.Contest.CountingCircleDetails).ThenInclude(ccd => ccd.VotingCards)
            .Include(me => me.DomainOfInfluence)
            .Include(me => me.SecondaryMajorityElections).ThenInclude(r => r.Results).ThenInclude(r => r.CandidateResults).ThenInclude(c => c.Candidate.Translations.OrderBy(t => t.Language))
            .Include(me => me.Results).ThenInclude(r => r.CountingCircle)
            .Include(me => me.Results).ThenInclude(sr => sr.CandidateResults).ThenInclude(cr => cr.Candidate).ThenInclude(x => x.Translations.OrderBy(t => t.Language))
            .FirstOrDefaultAsync(me => me.Id == ctx.PoliticalBusinessId, ct)
            ?? throw new EntityNotFoundException(nameof(MajorityElection), ctx.PoliticalBusinessId);

        var eventDelivery = _ech0110Serializer.ToDelivery(election);
        var electionShortDescription = _multiLanguageTranslationUtil.GetShortDescription(election);
        return _templateService.RenderToXml(ctx, eventDelivery.DeliveryHeader.MessageId, eventDelivery, electionShortDescription);
    }
}
