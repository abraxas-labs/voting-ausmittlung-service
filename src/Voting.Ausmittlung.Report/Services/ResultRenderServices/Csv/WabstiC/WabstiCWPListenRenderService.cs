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
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWPListenRenderService : WabstiCWPBaseRenderService
{
    public WabstiCWPListenRenderService(TemplateService templateService, IDbRepository<DataContext, ProportionalElection> repo)
        : base(templateService, repo)
    {
    }

    public override async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = Repo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.ProportionalElectionLists)
            .OrderBy(x => x.ProportionalElection.PoliticalBusinessNumber)
            .ThenBy(x => x.Position)
            .Select(x => new Data
            {
                PoliticalBusinessNumber = x.ProportionalElection.PoliticalBusinessNumber,
                DomainOfInfluenceType = x.ProportionalElection.DomainOfInfluence.Type,
                ListTranslations = x.Translations,
                ListNr = x.OrderNumber,
                VoteCount = x.EndResult!.TotalVoteCount,
                BlankRowsCount = x.EndResult!.BlankRowsCount,
                PoliticalBusinessId = x.ProportionalElection.Id,
                ListUnionPosition = x.ProportionalElectionListUnionEntries
                    .Select(y => y.ProportionalElectionListUnion)
                    .FirstOrDefault(y => !y.ProportionalElectionRootListUnionId.HasValue)!
                    .Position,
                ListSubUnionPosition = x.ProportionalElectionListUnionEntries
                    .Select(y => y.ProportionalElectionListUnion)
                    .FirstOrDefault(y => y.ProportionalElectionRootListUnionId.HasValue)!
                    .Position,
                ElectionUnionIds = x.ProportionalElection.ProportionalElectionUnionEntries
                    .Select(y => y.ProportionalElectionUnionId)
                    .OrderBy(y => y)
                    .ToList(),
                NumberOfMandates = x.ProportionalElection.EndResult!.Finalized
                    ? (int?)x.EndResult!.NumberOfMandates
                    : null,
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

        [Name("ListNr")]
        public string ListNr { get; set; } = string.Empty;

        [Ignore]
        public ICollection<ProportionalElectionListTranslation> ListTranslations { get; set; } = new HashSet<ProportionalElectionListTranslation>();

        [Name("ListCode")]
        public string ListShortDescription => ListTranslations.GetTranslated(x => x.ShortDescription);

        [Name("ListBez")]
        public string ListDescription => ListTranslations.GetTranslated(x => x.Description);

        [Name("StimmenTotal")]
        public int VoteCount { get; set; }

        [Name("StimmenZusatz")]
        public int BlankRowsCount { get; set; }

        [Name("Sitze")]
        public int? NumberOfMandates { get; set; }

        [Name("ListVerb")]
        public int? ListUnionPosition { get; set; }

        [Name("ListUntVerb")]
        public string ListSubUnionPositionInclRootUnionPosition => ListSubUnionPosition.HasValue
            ? $"{ListUnionPosition}.{ListSubUnionPosition}"
            : string.Empty;

        [Ignore]
        public int? ListSubUnionPosition { get; set; }

        [Name("GeLfNr")]
        public Guid PoliticalBusinessId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }
    }
}
