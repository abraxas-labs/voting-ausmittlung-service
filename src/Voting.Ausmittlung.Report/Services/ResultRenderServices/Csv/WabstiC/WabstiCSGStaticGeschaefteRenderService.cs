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
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCSGStaticGeschaefteRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Vote> _repo;

    public WabstiCSGStaticGeschaefteRenderService(TemplateService templateService, IDbRepository<DataContext, Vote> repo)
    {
        _templateService = templateService;
        _repo = repo;
    }

    public Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = _repo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.Ballots)
            .OrderBy(x => x.Vote.PoliticalBusinessNumber)
            .ThenBy(x => x.Position)
            .Select(x => new Data
            {
                ContestDate = x.Vote.Contest.Date,
                VoteId = x.Id,
                DomainOfInfluenceType = x.Vote.DomainOfInfluence.Type,
                DomainOfInfluenceSortNumber = x.Vote.DomainOfInfluence.SortNumber,
                DomainOfInfluenceName = x.Vote.DomainOfInfluence.Name,
                DomainOfInfluenceShortName = x.Vote.DomainOfInfluence.ShortName,
                DomainOfInfluenceBfs = x.Vote.DomainOfInfluence.Bfs,
                DomainOfInfluenceCode = x.Vote.DomainOfInfluence.Code,
                PoliticalBusinessNumber = x.Vote.PoliticalBusinessNumber,
                VoteTranslations = x.Vote.Translations,
                BallotType = x.BallotType,
                HasTieBreakQuestions = x.HasTieBreakQuestions,
                BallotQuestionTranslations = x.BallotQuestions.OrderBy(b => b.Number).Select(b => b.Translations),
                TieBreakQuestionTranslations = x.TieBreakQuestions.OrderBy(t => t.Number).Select(t => t.Translations),
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

        [Name("SortWahlkreis")]
        public int DomainOfInfluenceSortNumber { get; set; }

        [Name("SortGeschaeft")]
        public string PoliticalBusinessNumber { get; set; } = string.Empty;

        [Ignore]
        public ICollection<VoteTranslation> VoteTranslations { get; set; } = new HashSet<VoteTranslation>();

        [Name("Geschaeft")]
        public string VoteShortDescription => VoteTranslations.GetTranslated(x => x.ShortDescription);

        [Name("Wahlkreis")]
        public string DomainOfInfluenceName { get; set; } = string.Empty;

        [Name("WahlkreisKuerzel")]
        public string DomainOfInfluenceShortName { get; set; } = string.Empty;

        [Name("Wahlkreis-Code")]
        public string DomainOfInfluenceCode { get; set; } = string.Empty;

        [Name("BfsNrWKreis")]
        public string DomainOfInfluenceBfs { get; set; } = string.Empty;

        [Ignore]
        public BallotType BallotType { get; set; }

        [Ignore]
        public bool HasTieBreakQuestions { get; set; }

        // these values are defined by the wabstiC spec
        [Name("Typ")]
        public string WabstiCType => BallotType switch
        {
            BallotType.StandardBallot => "SG",
            BallotType.VariantsBallot when !HasTieBreakQuestions => "SGGV",
            BallotType.VariantsBallot when HasTieBreakQuestions => "SGGVSF",
            _ => string.Empty,
        };

        [Name("Sonntag")]
        [TypeConverter(typeof(WabstiCDateConverter))]
        public DateTime ContestDate { get; set; }

        [Name("GeBezOffiziell")]
        public string VoteOfficialDescription => VoteTranslations.GetTranslated(x => x.OfficialDescription);

        [Ignore]
        public IEnumerable<ICollection<BallotQuestionTranslation>>? BallotQuestionTranslations { get; set; }

        [Name("GeBezOffiziellGV")]
        public string BallotQuestion => string.Join(", ", BallotQuestionTranslations!.Select(t => t.GetTranslated(x => x.Question)));

        [Ignore]
        public IEnumerable<ICollection<TieBreakQuestionTranslation>>? TieBreakQuestionTranslations { get; set; }

        [Name("GeBezOffiziellSF")]
        public string? TieBreakQuestion => TieBreakQuestionTranslations?.ElementAtOrDefault(0)?.GetTranslated(x => x.Question);

        [Name("GeBezOffiziellSF2")]
        public string? TieBreakQuestion2 => TieBreakQuestionTranslations?.ElementAtOrDefault(1)?.GetTranslated(x => x.Question);

        [Name("GeBezOffiziellSF3")]
        public string? TieBreakQuestion3 => TieBreakQuestionTranslations?.ElementAtOrDefault(2)?.GetTranslated(x => x.Question);

        [Name("GeLfNr")]
        public Guid VoteId { get; set; }
    }
}
