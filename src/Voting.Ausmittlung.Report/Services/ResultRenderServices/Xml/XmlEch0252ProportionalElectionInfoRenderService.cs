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
using Voting.Lib.Ech.Ech0252_2_0.Schemas;
using Voting.Lib.Iam.Store;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Xml;

public class XmlEch0252ProportionalElectionInfoRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly Ech0252Serializer _ech0252Serializer;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepo;
    private readonly IAuth _auth;

    public XmlEch0252ProportionalElectionInfoRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Contest> contestRepo,
        Ech0252Serializer ech0252Serializer,
        IDbRepository<DataContext, DomainOfInfluence> doiRepo,
        IAuth auth)
    {
        _templateService = templateService;
        _contestRepo = contestRepo;
        _ech0252Serializer = ech0252Serializer;
        _doiRepo = doiRepo;
        _auth = auth;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        // Note: contest owners should see ALL proportional elections of the contest
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .IgnoreQueryFilters() // eCH exports need all languages, do not filter them
            .Include(x => x.Translations.OrderBy(t => t.Language))
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.ProportionalElections.Where(v => ctx.PoliticalBusinessIds.Contains(v.Id) || v.Contest.DomainOfInfluence.SecureConnectId == _auth.Tenant.Id))
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountingCircle.DomainOfInfluences)
                .ThenInclude(x => x.DomainOfInfluence)
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.Translations)
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.DomainOfInfluence)
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.ProportionalElectionLists)
                .ThenInclude(x => x.Translations)
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.ProportionalElectionLists)
                .ThenInclude(x => x.ProportionalElectionCandidates)
                .ThenInclude(x => x.Translations)
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.ProportionalElectionLists)
                .ThenInclude(x => x.ProportionalElectionCandidates)
                .ThenInclude(x => x.Party!)
                .ThenInclude(x => x.Translations)
            .Include(x => x.ProportionalElectionUnions)
                .ThenInclude(x => x.ProportionalElectionUnionEntries)
                .ThenInclude(x => x.ProportionalElection)
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.ProportionalElectionListUnions)
                .ThenInclude(x => x.Translations)
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.ProportionalElectionListUnions)
                .ThenInclude(x => x.ProportionalElectionListUnionEntries)
            .FirstOrDefaultAsync(x => x.Id == ctx.ContestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), ctx.ContestId);
        contest.MoveECountingToConventional();

        var domainOfInfluences = await _doiRepo.Query()
            .Include(doi => doi.SuperiorAuthorityDomainOfInfluence)
            .Where(doi => doi.SnapshotContestId == ctx.ContestId)
            .ToListAsync(ct);

        var mappingCtx = new Ech0252MappingContext(contest.EVoting, contest.DomainOfInfluence.Canton, domainOfInfluences);
        var eventDelivery = _ech0252Serializer.ToProportionalElectionInformationDelivery(contest, mappingCtx);
        return _templateService.RenderToXml(
            ctx,
            eventDelivery.DeliveryHeader.MessageId,
            eventDelivery,
            Ech0252Schemas.LoadEch0252Schemas(),
            contest.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    }
}
