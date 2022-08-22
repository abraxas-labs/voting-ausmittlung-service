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
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWPListenGdeRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, ProportionalElection> _repo;

    public WabstiCWPListenGdeRenderService(TemplateService templateService, IDbRepository<DataContext, ProportionalElection> repo)
    {
        _templateService = templateService;
        _repo = repo;
    }

    public Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = _repo.Query()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.ProportionalElectionLists)
            .SelectMany(x => x.Results)
            .OrderBy(x => x.List.ProportionalElection.PoliticalBusinessNumber)
            .ThenBy(x => x.Result.CountingCircle.Code)
            .ThenBy(x => x.Result.CountingCircle.Name)
            .ThenBy(x => x.List.Position)
            .Select(x => new Data
            {
                ListNr = x.List.OrderNumber,
                VoteCount = x.TotalVoteCount,
                CountingCircleBfs = x.Result.CountingCircle.Bfs,
                CountingCircleCode = x.Result.CountingCircle.Code,
                PoliticalBusinessId = x.Result.ProportionalElectionId,
                PoliticalBusinessNumber = x.Result.ProportionalElection.PoliticalBusinessNumber,
                DomainOfInfluenceType = x.Result.ProportionalElection.DomainOfInfluence.Type,
                VoteCountFromOtherLists = x.ListVotesCountOnOtherLists,
                ElectionUnionIds = x.Result.ProportionalElection.ProportionalElectionUnionEntries
                    .Select(y => y.ProportionalElectionUnionId)
                    .OrderBy(y => y)
                    .ToList(),
            })
            .AsAsyncEnumerable();

        return Task.FromResult(_templateService.RenderToCsv(
            ctx,
            results));
    }

    private class Data
    {
        [Name("Art")]
        [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
        public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

        [Name("SortGeschaeft")]
        public string PoliticalBusinessNumber { get; set; } = string.Empty;

        [Name("BfsNrGemeinde")]
        public string CountingCircleBfs { get; set; } = string.Empty;

        [Name("EinheitCode")]
        public string CountingCircleCode { get; set; } = string.Empty;

        [Name("ListNr")]
        public string ListNr { get; set; } = string.Empty;

        [Name("StimmenTotal")]
        public int VoteCount { get; set; }

        [Name("StimmenZusatz")]
        public int VoteCountFromOtherLists { get; set; }

        [Name("GeLfNr")]
        public Guid PoliticalBusinessId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }
    }
}
