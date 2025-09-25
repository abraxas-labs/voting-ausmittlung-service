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
public class WabstiCWPKandidatenGdeRenderService : WabstiCWPBaseRenderService
{
    private readonly IDbRepository<DataContext, ProportionalElection> _repo;

    public WabstiCWPKandidatenGdeRenderService(TemplateService templateService, IDbRepository<DataContext, ProportionalElection> repo)
        : base(templateService, repo)
    {
        _repo = repo;
    }

    public override async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = _repo.Query()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.ProportionalElectionLists)
            .SelectMany(x => x.ProportionalElectionCandidates)
            .SelectMany(x => x.Results)
            .OrderBy(x => x.ListResult.List.ProportionalElection.PoliticalBusinessNumber)
            .ThenBy(x => x.ListResult.Result.CountingCircle.Code)
            .ThenBy(x => x.ListResult.Result.CountingCircle.Name)
            .ThenBy(x => x.ListResult.List.Position)
            .ThenBy(x => x.Candidate.Position)
            .Select(x => new Data
            {
                CandidateNumber = x.Candidate.Number,
                ListNumber = x.ListResult.List.OrderNumber,
                VoteCount = x.VoteCount,
                CountingCircleBfs = x.ListResult.Result.CountingCircle.Bfs,
                CountingCircleCode = x.ListResult.Result.CountingCircle.Code,
                PoliticalBusinessId = x.ListResult.Result.PoliticalBusinessId,
                PoliticalBusinessNumber = x.ListResult.Result.ProportionalElection.PoliticalBusinessNumber,
                DomainOfInfluenceType = x.ListResult.Result.ProportionalElection.DomainOfInfluence.Type,
                ElectionUnionIds = x.ListResult.Result.ProportionalElection.ProportionalElectionUnionEntries
                    .Select(y => y.ProportionalElectionUnionId)
                    .OrderBy(y => y)
                    .ToList(),
                ResultState = x.ListResult.Result.State,
            })
            .AsAsyncEnumerable()
            .Select(x =>
            {
                x.ResetDataIfSubmissionNotDone();
                return x;
            });

        return await RenderToCsv(
            ctx,
            results);
    }

    private class Data : IWabstiCPoliticalResultData
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

        [Name("KNR")]
        public string ListAndCandidateNumber => $"{ListNumber}.{CandidateNumber}";

        [Ignore]
        public string CandidateNumber { get; set; } = string.Empty;

        [Ignore]
        public string ListNumber { get; set; } = string.Empty;

        [Name("Stimmen")]
        public int? VoteCount { get; set; }

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

            VoteCount = null;
        }
    }
}
