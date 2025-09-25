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
public class WabstiCWPListenGdeRenderService : WabstiCWPBaseRenderService
{
    public WabstiCWPListenGdeRenderService(TemplateService templateService, IDbRepository<DataContext, ProportionalElection> repo)
        : base(templateService, repo)
    {
    }

    public override async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = Repo.Query()
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
                VoteCountFromUnmodifiedLists = x.UnmodifiedListVotesCount,
                VoteCountFromModifiedLists = x.ModifiedListVotesCount,
                BlankRowsCount = x.BlankRowsCount,
                ElectionUnionIds = x.Result.ProportionalElection.ProportionalElectionUnionEntries
                    .Select(y => y.ProportionalElectionUnionId)
                    .OrderBy(y => y)
                    .ToList(),
                ResultState = x.Result.State,
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

        [Name("ListNr")]
        public string ListNr { get; set; } = string.Empty;

        [Name("StimmenTotal")]
        public int? VoteCount { get; set; }

        [Name("StimmenWzUnveraendert")]
        public int? VoteCountFromUnmodifiedLists { get; set; }

        [Name("StimmenWzVeraendert")]
        public int? VoteCountFromModifiedLists { get; set; }

        [Name("StimmenZusatz")]
        public int? BlankRowsCount { get; set; }

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
            VoteCountFromUnmodifiedLists = null;
            VoteCountFromModifiedLists = null;
            BlankRowsCount = null;
        }
    }
}
