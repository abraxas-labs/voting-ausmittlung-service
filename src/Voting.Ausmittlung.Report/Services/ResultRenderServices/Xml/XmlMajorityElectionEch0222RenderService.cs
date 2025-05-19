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
using Voting.Lib.Ech.Ech0222_1_0.Schemas;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Xml;

public class XmlMajorityElectionEch0222RenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElection> _electionRepo;
    private readonly MultiLanguageTranslationUtil _multiLanguageTranslationUtil;
    private readonly Ech0222Serializer _ech0222Serializer;

    public XmlMajorityElectionEch0222RenderService(
        TemplateService templateService,
        IDbRepository<DataContext, MajorityElection> electionRepo,
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
            .Include(me => me.Translations)
            .Include(me => me.Contest.Translations)
            .Include(me => me.Contest.DomainOfInfluence)
            .Include(me => me.ElectionGroup)
            .Include(me => me.Results)
                .ThenInclude(r => r.CountingCircle)
            .Include(me => me.Results)
                .ThenInclude(r => r.Bundles)
                .ThenInclude(b => b.Ballots)
                .ThenInclude(b => b.BallotCandidates)
                .ThenInclude(b => b.Candidate)
            .Include(me => me.Results)
                .ThenInclude(r => r.SecondaryMajorityElectionResults)
                .ThenInclude(b => b.ResultBallots)
                .ThenInclude(b => b.BallotCandidates)
                .ThenInclude(b => b.Candidate)
            .Include(me => me.Results)
                .ThenInclude(r => r.BallotGroupResults)
                .ThenInclude(bgr => bgr.BallotGroup)
                .ThenInclude(bg => bg.Entries)
                .ThenInclude(e => e.Candidates)
                .ThenInclude(e => e.PrimaryElectionCandidate)
            .Include(me => me.Results)
                .ThenInclude(r => r.BallotGroupResults)
                .ThenInclude(bgr => bgr.BallotGroup)
                .ThenInclude(bg => bg.Entries)
                .ThenInclude(e => e.Candidates)
                .ThenInclude(e => e.SecondaryElectionCandidate)
            .FirstOrDefaultAsync(me => me.Id == ctx.PoliticalBusinessId, ct)
            ?? throw new EntityNotFoundException(nameof(MajorityElection), ctx.PoliticalBusinessId);
        election.MoveECountingToConventional();

        election.Translations = election.Translations.OrderBy(x => x.Language).ToList();
        election.Contest.Translations = election.Contest.Translations.OrderBy(x => x.Language).ToList();

        var eventDelivery = _ech0222Serializer.ToDelivery(election);
        var electionShortDescription = _multiLanguageTranslationUtil.GetShortDescription(election);
        return _templateService.RenderToXml(
            ctx,
            eventDelivery.DeliveryHeader.MessageId,
            eventDelivery,
            Ech0222Schemas.LoadEch0222Schemas(),
            electionShortDescription);
    }
}
