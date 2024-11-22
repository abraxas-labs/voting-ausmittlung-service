// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Converters;
using Voting.Ausmittlung.Ech.Models;
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Xml;

public class XmlEch0252MajorityElectionInfoRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly Ech0252Serializer _ech0252Serializer;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepo;

    public XmlEch0252MajorityElectionInfoRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Contest> contestRepo,
        Ech0252Serializer ech0252Serializer,
        IDbRepository<DataContext, DomainOfInfluence> doiRepo)
    {
        _templateService = templateService;
        _contestRepo = contestRepo;
        _ech0252Serializer = ech0252Serializer;
        _doiRepo = doiRepo;
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
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountingCircle.DomainOfInfluences)
                .ThenInclude(x => x.DomainOfInfluence)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.DomainOfInfluence)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.Translations)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.MajorityElectionCandidates)
                .ThenInclude(x => x.Translations)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.SecondaryMajorityElections)
                .ThenInclude(x => x.Translations)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.SecondaryMajorityElections)
                .ThenInclude(x => x.Candidates)
                .ThenInclude(x => x.Translations)
            .Include(x => x.MajorityElectionUnions)
                .ThenInclude(x => x.MajorityElectionUnionEntries)
            .FirstOrDefaultAsync(x => x.Id == ctx.ContestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), ctx.ContestId);

        var domainOfInfluences = await _doiRepo.Query()
            .Where(doi => doi.SnapshotContestId == ctx.ContestId)
            .Include(doi => doi.SuperiorAuthorityDomainOfInfluence)
            .ToListAsync(ct);

        var mappingCtx = new Ech0252MappingContext(domainOfInfluences);
        var eventDelivery = _ech0252Serializer.ToMajorityElectionInformationDelivery(contest, mappingCtx);
        return _templateService.RenderToXml(ctx, eventDelivery.DeliveryHeader.MessageId, eventDelivery, contest.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    }
}
