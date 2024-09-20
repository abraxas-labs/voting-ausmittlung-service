// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWPWahlRenderService : WabstiCWPBaseRenderService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, ProportionalElection> _repo;

    public WabstiCWPWahlRenderService(TemplateService templateService, IDbRepository<DataContext, ProportionalElection> repo)
        : base(templateService, repo)
    {
        _templateService = templateService;
        _repo = repo;
    }

    public override async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = _repo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .OrderByNaturalOrder()
            .Select(x => new Data
            {
                PoliticalBusinessId = x.Id,
                PoliticalBusinessNumber = x.PoliticalBusinessNumber,
                DomainOfInfluenceType = x.DomainOfInfluence.Type,
                ContestDate = x.Contest.Date,
                ElectionTranslations = x.Translations,
                DomainOfInfluenceBfs = x.DomainOfInfluence.Bfs,
                DomainOfInfluenceCanton = x.DomainOfInfluence.Canton,
                DomainOfInfluenceCode = x.DomainOfInfluence.Code,
                DomainOfInfluenceName = x.DomainOfInfluence.Name,
                CountOfDoneCountingCircles = x.EndResult!.CountOfDoneCountingCircles,
                TotalCountOfCountingCircles = x.EndResult!.TotalCountOfCountingCircles,
                Finalized = x.EndResult!.Finalized,
                DomainOfInfluenceSortNumber = x.DomainOfInfluence.SortNumber,
                ElectionUnionIds = x.ProportionalElectionUnionEntries
                    .Select(y => y.ProportionalElectionUnionId)
                    .OrderBy(y => y)
                    .ToList(),
            })
            .AsAsyncEnumerable();

        return await RenderToCsv(
            ctx,
            results);
    }

    private class Data
    {
        [Name("Art")]
        [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
        public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

        [Name("SortGeschaeft")]
        public string PoliticalBusinessNumber { get; set; } = string.Empty;

        [Ignore]
        public int TotalCountOfCountingCircles { get; set; }

        [Ignore]
        public int CountOfDoneCountingCircles { get; set; }

        [Name("AnzPendentGde")]
        public int CountOfPendingCountingCircles => TotalCountOfCountingCircles - CountOfDoneCountingCircles;

        [Ignore]
        public ICollection<ProportionalElectionTranslation> ElectionTranslations { get; set; } = new HashSet<ProportionalElectionTranslation>();

        [Name("GeBezKurz")]
        public string ElectionShortDescription => ElectionTranslations.GetTranslated(x => x.ShortDescription);

        [Name("GeBezOffiziell")]
        public string ElectionOfficialDescription => ElectionTranslations.GetTranslated(x => x.OfficialDescription);

        [Name("Ausmittlungsstand")]
        [TypeConverter(typeof(WabstiCEndResultFinalizedConverter))]
        public bool Finalized { get; set; }

        [Name("Sonntag")]
        [TypeConverter(typeof(WabstiCDateConverter))]
        public DateTime ContestDate { get; set; }

        [Name("Kanton")]
        [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
        public DomainOfInfluenceCanton DomainOfInfluenceCanton { get; set; }

        [Name("SortWahlkreis")]
        public int DomainOfInfluenceSortNumber { get; set; }

        [Name("WahlkreisBez")]
        public string DomainOfInfluenceName { get; set; } = string.Empty;

        [Name("Wahlkreis-Code")]
        public string DomainOfInfluenceCode { get; set; } = string.Empty;

        [Name("BfsNrWKreis")]
        public string DomainOfInfluenceBfs { get; set; } = string.Empty;

        [Name("GeLfNr")]
        public Guid PoliticalBusinessId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }
    }
}
