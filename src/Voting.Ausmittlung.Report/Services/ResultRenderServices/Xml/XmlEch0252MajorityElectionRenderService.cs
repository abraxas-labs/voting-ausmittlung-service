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

public class XmlEch0252MajorityElectionRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly Ech0252Serializer _ech0252Serializer;

    public XmlEch0252MajorityElectionRenderService(
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
            .Include(x => x.MajorityElections.Where(v => ctx.PoliticalBusinessIds.Contains(v.Id)))
                .ThenInclude(x => x.DomainOfInfluence)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountingCircle)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountOfVoters)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CandidateResults)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.EndResult!)
                .ThenInclude(x => x.Calculation)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.EndResult!)
                .ThenInclude(x => x.CandidateEndResults)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.SecondaryMajorityElections)
                .ThenInclude(x => x.EndResult!)
                .ThenInclude(x => x.CandidateEndResults)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.SecondaryMajorityElections)
                .ThenInclude(x => x.EndResult!)
                .ThenInclude(x => x.PrimaryMajorityElectionEndResult)
                .ThenInclude(x => x.Calculation)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.SecondaryMajorityElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CandidateResults)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.SecondaryMajorityElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.PrimaryResult)
                .ThenInclude(x => x.CountingCircle)
            .FirstOrDefaultAsync(x => x.Id == ctx.ContestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), ctx.ContestId);

        var eventDelivery = _ech0252Serializer.ToMajorityElectionDelivery(contest);
        return _templateService.RenderToXml(ctx, eventDelivery.DeliveryHeader.MessageId, eventDelivery, contest.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    }
}
