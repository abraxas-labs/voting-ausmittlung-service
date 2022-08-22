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
public class WabstiCWPKandidatenRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, ProportionalElection> _repo;

    public WabstiCWPKandidatenRenderService(TemplateService templateService, IDbRepository<DataContext, ProportionalElection> repo)
    {
        _templateService = templateService;
        _repo = repo;
    }

    public Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = _repo.Query()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.ProportionalElectionLists)
            .SelectMany(x => x.ProportionalElectionCandidates)
            .OrderBy(x => x.ProportionalElectionList.ProportionalElection.PoliticalBusinessNumber)
            .ThenBy(x => x.ProportionalElectionList.Position)
            .ThenBy(x => x.Position)
            .Select(x => new Data
            {
                CandidateNumber = x.Number,
                ListNumber = x.ProportionalElectionList.OrderNumber,
                VoteCount = x.EndResult!.VoteCount,
                Elected = !x.EndResult!.ListEndResult.ElectionEndResult.Finalized
                    ? (bool?)null
                    : x.EndResult!.State == ProportionalElectionCandidateEndResultState.Elected,
                PoliticalBusinessId = x.ProportionalElectionList.ProportionalElection.Id,
                PoliticalBusinessNumber = x.ProportionalElectionList.ProportionalElection.PoliticalBusinessNumber,
                DomainOfInfluenceType = x.ProportionalElectionList.ProportionalElection.DomainOfInfluence.Type,
                ElectionUnionIds = x.ProportionalElectionList.ProportionalElection.ProportionalElectionUnionEntries
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

        [Name("KNR")]
        public string ListAndCandidateNumber => $"{ListNumber}.{CandidateNumber}";

        [Ignore]
        public string CandidateNumber { get; set; } = string.Empty;

        [Ignore]
        public string ListNumber { get; set; } = string.Empty;

        [Name("Gewaehlt")]
        [TypeConverter(typeof(WabstiCBooleanConverter))]
        public bool? Elected { get; set; }

        [Name("Stimmen")]
        public int VoteCount { get; set; }

        [Name("GeLfNr")]
        public Guid PoliticalBusinessId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }
    }
}
