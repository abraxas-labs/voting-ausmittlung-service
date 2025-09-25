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
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWPStaticGemeindenRenderService : WabstiCWPBaseRenderService
{
    private readonly WabstiCContestDetailsAttacher _contestDetailsAttacher;

    public WabstiCWPStaticGemeindenRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElection> repo,
        WabstiCContestDetailsAttacher contestDetailsAttacher)
        : base(templateService, repo)
    {
        _contestDetailsAttacher = contestDetailsAttacher;
    }

    public override async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = await Repo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.Results)
            .OrderBy(x => x.ProportionalElection.PoliticalBusinessNumber)
            .ThenBy(x => x.CountingCircle.Code)
            .ThenBy(x => x.CountingCircle.Name)
            .Select(x => new Data
            {
                CountingCircleId = x.CountingCircleId,
                PoliticalBusinessId = x.ProportionalElectionId,
                DomainOfInfluenceType = x.ProportionalElection.DomainOfInfluence.Type,
                DomainOfInfluenceCode = x.ProportionalElection.DomainOfInfluence.Code,
                DomainOfInfluenceName = x.ProportionalElection.DomainOfInfluence.Name,
                DomainOfInfluenceSortNumber = x.ProportionalElection.DomainOfInfluence.SortNumber,
                CountingCircleBfs = x.CountingCircle.Bfs,
                CountingCircleCode = x.CountingCircle.Code,
                CountingCircleName = x.CountingCircle.Name,
                PoliticalBusinessTranslations = x.ProportionalElection.Translations,
                PoliticalBusinessNumber = x.ProportionalElection.PoliticalBusinessNumber,
                TotalCountOfVoters = x.TotalCountOfVoters,
                ElectionUnionIds = x.ProportionalElection.ProportionalElectionUnionEntries
                    .Select(y => y.ProportionalElectionUnionId)
                    .OrderBy(y => y)
                    .ToList(),
                ResultState = x.State,
            })
            .ToListAsync(ct);

        await _contestDetailsAttacher.AttachSwissAbroadCountOfVoters(ctx.ContestId, results, ct);
        foreach (var result in results)
        {
            result.ResetDataIfSubmissionNotDone();
        }

        return await RenderToCsv(
            ctx,
            results);
    }

    private class Data : IWabstiCSwissAbroadCountOfVoters, IWabstiCPoliticalResultData
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

        [Name("BfsNrGemeinde")]
        public string CountingCircleBfs { get; set; } = string.Empty;

        [Name("EinheitCode")]
        public string CountingCircleCode { get; set; } = string.Empty;

        [Name("Wahlkreis-Code")]
        public string DomainOfInfluenceCode { get; set; } = string.Empty;

        [Ignore]
        public ICollection<ProportionalElectionTranslation> PoliticalBusinessTranslations { get; set; } = new HashSet<ProportionalElectionTranslation>();

        [Name("Geschaeft")]
        public string PoliticalBusinessShortDescription => PoliticalBusinessTranslations.GetTranslated(x => x.ShortDescription);

        [Name("Stimmberechtigte")]
        public int? TotalCountOfVoters { get; set; }

        [Name("StimmberechtigteAusl")]
        public int? CountOfVotersTotalSwissAbroad { get; set; }

        [Name("GeLfNr")]
        public Guid PoliticalBusinessId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }

        [Ignore]
        public CountingCircleResultState ResultState { get; set; }

        public void ResetDataIfSubmissionNotDone()
        {
            if (ResultState.IsSubmissionDone())
            {
                return;
            }

            TotalCountOfVoters = null;
            CountOfVotersTotalSwissAbroad = null;
        }
    }
}
