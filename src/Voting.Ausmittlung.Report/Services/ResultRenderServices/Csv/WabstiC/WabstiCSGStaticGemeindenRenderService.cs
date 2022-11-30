// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCSGStaticGemeindenRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Vote> _repo;
    private readonly WabstiCContestDetailsAttacher _contestDetailsAttacher;

    public WabstiCSGStaticGemeindenRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Vote> repo,
        WabstiCContestDetailsAttacher contestDetailsAttacher)
    {
        _templateService = templateService;
        _repo = repo;
        _contestDetailsAttacher = contestDetailsAttacher;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = await _repo.Query()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.Results)
            .OrderBy(x => x.Vote.PoliticalBusinessNumber)
            .ThenBy(x => x.CountingCircle.Code)
            .ThenBy(x => x.CountingCircle.Name)
            .Select(x => new Data
            {
                VoteId = x.VoteId,
                DomainOfInfluenceName = x.Vote.DomainOfInfluence.Name,
                DomainOfInfluenceType = x.Vote.DomainOfInfluence.Type,
                DomainOfInfluenceSortNumber = x.Vote.DomainOfInfluence.SortNumber,
                PoliticalBusinessNumber = x.Vote.PoliticalBusinessNumber,
                CountingCircleBfs = x.CountingCircle.Bfs,
                CountingCircleCode = x.CountingCircle.Code,
                CountingCircleName = x.CountingCircle.Name,
                CountingCircleId = x.CountingCircle.Id,
                CountOfVotersTotal = x.TotalCountOfVoters,
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

        [Name("BfsNrGemeinde")]
        public string CountingCircleBfs { get; set; } = string.Empty;

        [Name("EinheitCode")]
        public string CountingCircleCode { get; set; } = string.Empty;

        [Name("SortGeschaeft")]
        public string PoliticalBusinessNumber { get; set; } = string.Empty;

        [Name("Gemeinde")]
        public string CountingCircleName { get; set; } = string.Empty;

        [Name("Stimmberechtigte")]
        public int CountOfVotersTotal { get; set; }

        [Name("StimmberechtigteAusl")]
        public int CountOfVotersTotalSwissAbroad { get; set; }

        [Name("GeLfNr")]
        public Guid VoteId { get; set; }
    }
}
