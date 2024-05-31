// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Globalization;
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

public class XmlEch0252VoteRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly Ech0252Serializer _ech0252Serializer;

    public XmlEch0252VoteRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Contest> contestRepo,
        Ech0252Serializer ech0252Serializer)
    {
        _templateService = templateService;
        _contestRepo = contestRepo;
        _ech0252Serializer = ech0252Serializer;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .IgnoreQueryFilters() // eCH exports need all languages, do not filter them
            .Include(x => x.Translations.OrderBy(t => t.Language))
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Votes.Where(v => ctx.PoliticalBusinessIds.Contains(v.Id)))
                .ThenInclude(x => x.Ballots)
                .ThenInclude(x => x.BallotQuestions)
                .ThenInclude(x => x.Translations)
            .Include(x => x.Votes)
                .ThenInclude(x => x.Ballots)
                .ThenInclude(x => x.TieBreakQuestions)
                .ThenInclude(x => x.Translations)
            .Include(x => x.Votes)
                .ThenInclude(x => x.DomainOfInfluence)
            .Include(x => x.Votes)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountingCircle)
            .Include(x => x.Votes)
                .ThenInclude(x => x.Ballots)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountOfVoters)
            .Include(x => x.Votes)
                .ThenInclude(x => x.Ballots)
                .ThenInclude(x => x.BallotQuestions)
                .ThenInclude(x => x.Results)
            .Include(x => x.Votes)
                .ThenInclude(x => x.Ballots)
                .ThenInclude(x => x.TieBreakQuestions)
                .ThenInclude(x => x.Results)
            .FirstOrDefaultAsync(x => x.Id == ctx.ContestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), ctx.ContestId);

        var eventDelivery = _ech0252Serializer.ToVoteDelivery(contest);
        return _templateService.RenderToXml(ctx, eventDelivery.DeliveryHeader.MessageId, eventDelivery, contest.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    }
}
