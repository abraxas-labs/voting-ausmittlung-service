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
using Voting.Lib.Ech.Ech0252_2_0.Schemas;
using Voting.Lib.Iam.Store;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Xml;

public class XmlEch0252MajorityElectionResultRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly Ech0252Serializer _ech0252Serializer;
    private readonly IAuth _auth;

    public XmlEch0252MajorityElectionResultRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Contest> contestRepo,
        Ech0252Serializer ech0252Serializer,
        IAuth auth)
    {
        _templateService = templateService;
        _contestRepo = contestRepo;
        _ech0252Serializer = ech0252Serializer;
        _auth = auth;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        // Note: contest owners should see ALL majority elections of the contest
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .IgnoreQueryFilters() // eCH exports need all languages, do not filter them
            .Include(x => x.Translations.OrderBy(t => t.Language))
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.MajorityElections.Where(v => ctx.PoliticalBusinessIds.Contains(v.Id) || v.Contest.DomainOfInfluence.SecureConnectId == _auth.Tenant.Id))
                .ThenInclude(x => x.DomainOfInfluence)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountingCircle.ContestDetails)
                .ThenInclude(x => x.VotingCards)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountingCircle.ContestDetails)
                .ThenInclude(x => x.CountOfVotersInformationSubTotals)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.Results)
                .ThenInclude(x => x.CountingCircle.DomainOfInfluences)
                .ThenInclude(x => x.DomainOfInfluence)
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
                .ThenInclude(x => x.CandidateEndResults.OrderBy(y => y.CandidateId))
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.EndResult!)
                .ThenInclude(x => x.CandidateEndResults)
                .ThenInclude(x => x.Candidate.Translations)
            .Include(x => x.MajorityElections)
                .ThenInclude(x => x.SecondaryMajorityElections)
                .ThenInclude(x => x.EndResult!)
                .ThenInclude(x => x.CandidateEndResults.OrderBy(y => y.CandidateId))
                .ThenInclude(x => x.Candidate.Translations)
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

        contest.MoveECountingToConventional();

        var eventDelivery = _ech0252Serializer.ToMajorityElectionResultDelivery(contest, null);
        return _templateService.RenderToXml(
            ctx,
            eventDelivery.DeliveryHeader.MessageId,
            eventDelivery,
            Ech0252Schemas.LoadEch0252Schemas(),
            contest.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    }
}
