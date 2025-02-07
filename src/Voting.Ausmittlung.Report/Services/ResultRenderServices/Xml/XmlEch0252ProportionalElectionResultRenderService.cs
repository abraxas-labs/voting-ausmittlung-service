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
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Xml;

public class XmlEch0252ProportionalElectionResultRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly Ech0252Serializer _ech0252Serializer;

    public XmlEch0252ProportionalElectionResultRenderService(
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
            .Include(x => x.ProportionalElections.Where(v => ctx.PoliticalBusinessIds.Contains(v.Id)))
                .ThenInclude(x => x.DomainOfInfluence)
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountingCircle.ContestDetails)
                .ThenInclude(x => x.VotingCards)
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountingCircle.ContestDetails)
                .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountOfVoters)
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.ListResults.OrderBy(y => y.ListId))
                .ThenInclude(x => x.CandidateResults.OrderBy(y => y.CandidateId))
                .ThenInclude(x => x.VoteSources.OrderBy(y => y.ListId))
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.EndResult!)
                .ThenInclude(x => x.ListEndResults.OrderBy(y => y.ListId))
                .ThenInclude(x => x.CandidateEndResults.OrderBy(y => y.CandidateId))
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.EndResult!)
                .ThenInclude(x => x.ListLotDecisions.OrderBy(y => y.Id))
                .ThenInclude(x => x.Entries.OrderBy(y => y.Id))
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.DoubleProportionalResult)
                .ThenInclude(x => x!.Columns.OrderBy(y => y.ListId).ThenBy(y => y.UnionListId))
            .Include(x => x.ProportionalElections)
                .ThenInclude(x => x.ProportionalElectionUnionEntries)
                .ThenInclude(x => x.ProportionalElectionUnion)
                .ThenInclude(x => x.DoubleProportionalResult)
                .ThenInclude(x => x!.Columns.OrderBy(y => y.ListId).ThenBy(y => y.UnionListId))
                .ThenInclude(x => x.Cells.OrderBy(y => y.ListId))
                .ThenInclude(x => x.List)
            .FirstOrDefaultAsync(x => x.Id == ctx.ContestId, ct)
            ?? throw new EntityNotFoundException(nameof(Contest), ctx.ContestId);

        var eventDelivery = _ech0252Serializer.ToProportionalElectionResultDelivery(contest, null);
        return _templateService.RenderToXml(ctx, eventDelivery.DeliveryHeader.MessageId, eventDelivery, contest.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    }
}
