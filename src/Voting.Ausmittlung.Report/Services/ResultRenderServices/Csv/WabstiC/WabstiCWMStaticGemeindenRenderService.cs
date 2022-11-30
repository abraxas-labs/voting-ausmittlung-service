// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWMStaticGemeindenRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElection> _repo;
    private readonly WabstiCContestDetailsAttacher _contestDetailsAttacher;

    public WabstiCWMStaticGemeindenRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, MajorityElection> repo,
        WabstiCContestDetailsAttacher contestDetailsAttacher)
    {
        _templateService = templateService;
        _repo = repo;
        _contestDetailsAttacher = contestDetailsAttacher;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = await _repo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.Results)
            .OrderBy(x => x.MajorityElection.DomainOfInfluence.Type)
            .ThenBy(x => x.MajorityElection.PoliticalBusinessNumber)
            .ThenBy(y => y.CountingCircle.Code)
            .ThenBy(y => y.CountingCircle.Name)
            .Select(y => new Data
            {
                CountingCircleId = y.CountingCircleId,
                DomainOfInfluenceType = y.MajorityElection.DomainOfInfluence.Type,
                CountingCircleBfs = y.CountingCircle.Bfs,
                CountingCircleCode = y.CountingCircle.Code,
                TotalCountOfVoters = y.TotalCountOfVoters,
                ElectionId = y.MajorityElectionId,
                ElectionUnionIds = y.MajorityElection.MajorityElectionUnionEntries
                    .Select(z => z.MajorityElectionUnionId)
                    .OrderBy(z => z)
                    .ToList(),
                CountingCircleName = y.CountingCircle.Name,
                PoliticalBusinessNumber = y.MajorityElection.PoliticalBusinessNumber,
                DomainOfInfluenceCode = y.MajorityElection.DomainOfInfluence.Code,
                DomainOfInfluenceName = y.MajorityElection.DomainOfInfluence.Name,
                PoliticalBusinessTranslations = y.MajorityElection.Translations,
                DomainOfInfluenceSortNumber = y.MajorityElection.DomainOfInfluence.SortNumber,
            })
            .ToListAsync(ct);

        await _contestDetailsAttacher.AttachSwissAbroadCountOfVoters(ctx.ContestId, results, ct);

        return _templateService.RenderToCsv(
            ctx,
            results);
    }

    private class Data : IWabstiCSwissAbroadCountOfVoters
    {
        [Ignore]
        public Guid CountingCircleId { get; set; }

        [Name("Art")]
        [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
        public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

        [Name("Wahlkreis")]
        public string DomainOfInfluenceName { get; set; } = string.Empty;

        [Name("SortWahlkreis")]
        public int DomainOfInfluenceSortNumber { get; set; }

        [Name("SortGeschaeft")]
        public string PoliticalBusinessNumber { get; set; } = string.Empty;

        [Name("Gemeinde")]
        public string CountingCircleName { get; set; } = string.Empty;

        [Name("Einheitcode")]
        public string CountingCircleCode { get; set; } = string.Empty;

        [Name("GeLfNr")]
        public Guid ElectionId { get; set; }

        [Name("Wahlkreis-Code")]
        public string DomainOfInfluenceCode { get; set; } = string.Empty;

        [Ignore]
        public ICollection<MajorityElectionTranslation> PoliticalBusinessTranslations { get; set; } = new HashSet<MajorityElectionTranslation>();

        [Name("Geschaeft")]
        public string PoliticalBusinessShortDescription => PoliticalBusinessTranslations.GetTranslated(x => x.ShortDescription);

        [Name("Stimmberechtigte")]
        public int TotalCountOfVoters { get; set; }

        [Name("StimmberechtigteAusl")]
        public int CountOfVotersTotalSwissAbroad { get; set; }

        [Name("BfsNrGemeinde")]
        public string CountingCircleBfs { get; set; } = string.Empty;

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }
    }
}
