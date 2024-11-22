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
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Lib.Database.Repositories;
using IndexAttribute = CsvHelper.Configuration.Attributes.IndexAttribute;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCSGGeschaefteRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Vote> _repo;

    public WabstiCSGGeschaefteRenderService(TemplateService templateService, IDbRepository<DataContext, Vote> repo)
    {
        _templateService = templateService;
        _repo = repo;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = await _repo.Query()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.Ballots)
            .OrderBy(x => x.Vote.PoliticalBusinessNumber)
            .ThenBy(x => x.Position)
            .Select(x => new Data
            {
                ContestDate = x.Vote.Contest.Date,
                PoliticalBusinessId = x.Vote.Type == VoteType.QuestionsOnSingleBallot ? x.VoteId : x.Id,
                DomainOfInfluenceType = x.Vote.DomainOfInfluence.Type,
                DomainOfInfluenceSortNumber = x.Vote.DomainOfInfluence.SortNumber,
                DomainOfInfluenceCanton = x.Vote.DomainOfInfluence.Canton,
                PoliticalBusinessNumber = x.Vote.PoliticalBusinessNumber,
                VoteTranslations = x.Vote.Translations,
                CountOfDoneCountingCircles = x.Vote.EndResult!.CountOfDoneCountingCircles,
                TotalCountOfCountingCircles = x.Vote.EndResult!.TotalCountOfCountingCircles,
                Finalized = x.Vote.EndResult!.Finalized,
                VoteType = x.Vote.Type,
                Position = x.Position,
            })
            .ToListAsync(ct);

        return _templateService.RenderToCsv(
            ctx,
            results);
    }

    private class Data : WabstiCPoliticalBusinessData
    {
        private new const int StartIndex = WabstiCPoliticalBusinessData.StartIndex * 10;

        [Ignore]
        public int TotalCountOfCountingCircles { get; set; }

        [Ignore]
        public int CountOfDoneCountingCircles { get; set; }

        [Name("AnzPendentGde")]
        [Index(StartIndex + 1)]
        public int CountOfPendingCountingCircles => TotalCountOfCountingCircles - CountOfDoneCountingCircles;

        [Ignore]
        public ICollection<VoteTranslation> VoteTranslations { get; set; } = new HashSet<VoteTranslation>();

        [Name("GeBezKurz")]
        [Index(StartIndex + 2)]
        public string VoteShortDescription => VoteTranslations.GetTranslated(x => x.ShortDescription);

        [Name("GeBezOffiziell")]
        [Index(StartIndex + 3)]
        public string VoteOfficialDescription => VoteTranslations.GetTranslated(x => x.OfficialDescription);

        [Name("Ausmittlungsstand")]
        [Index(StartIndex + 4)]
        [TypeConverter(typeof(WabstiCEndResultFinalizedConverter))]
        public bool Finalized { get; set; }

        [Name("Sonntag")]
        [TypeConverter(typeof(WabstiCDateConverter))]
        [Index(StartIndex + 5)]
        public DateTime ContestDate { get; set; }

        [Name("Kanton")]
        [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
        [Index(StartIndex + 6)]
        public DomainOfInfluenceCanton DomainOfInfluenceCanton { get; set; }

        [Ignore]
        public VoteType VoteType { get; set; }

        [Ignore]
        public int Position { get; set; }

        [Name("GeSubNr")]
        [Index(EndIndex + 1)]
        public string BallotSubType => WabstiCPositionUtil.BuildPosition(Position, VoteType);
    }
}
