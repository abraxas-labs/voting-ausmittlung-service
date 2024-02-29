// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
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
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWPStaticKandidatenRenderService : WabstiCWPBaseRenderService
{
    public WabstiCWPStaticKandidatenRenderService(TemplateService templateService, IDbRepository<DataContext, ProportionalElection> repo)
        : base(templateService, repo)
    {
    }

    public override async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = Repo.Query()
            .AsSplitQuery()
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
                PoliticalBusinessId = x.ProportionalElectionList.ProportionalElection.Id,
                PoliticalBusinessNumber = x.ProportionalElectionList.ProportionalElection.PoliticalBusinessNumber,
                DomainOfInfluenceType = x.ProportionalElectionList.ProportionalElection.DomainOfInfluence.Type,
                Incumbent = x.Incumbent,
                Locality = x.Locality,
                Sex = x.Sex,
                Title = x.Title,
                BirthYear = x.DateOfBirth.Year,
                FirstName = x.PoliticalFirstName,
                LastName = x.PoliticalLastName,
                CandidateTranslations = x.Translations,
                ListTranslations = x.ProportionalElectionList.Translations,
                ElectionUnionIds = x.ProportionalElectionList.ProportionalElection.ProportionalElectionUnionEntries
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

        [Name("KNR")]
        public string ListAndCandidateNumber => $"{ListNumber}.{CandidateNumber}";

        [Ignore]
        public string CandidateNumber { get; set; } = string.Empty;

        [Name("Nachname")]
        public string LastName { get; set; } = string.Empty;

        [Name("Vorname")]
        public string FirstName { get; set; } = string.Empty;

        [Name("Beruf")]
        public string Occupation => CandidateTranslations.GetTranslated(x => x.Occupation, true);

        [Name("Wohnort")]
        public string Locality { get; set; } = string.Empty;

        [Name("Jahrgang")]
        public int BirthYear { get; set; }

        [Ignore]
        public ICollection<ProportionalElectionListTranslation> ListTranslations { get; set; } = new HashSet<ProportionalElectionListTranslation>();

        [Name("ListeCode")]
        public string ListShortDescription => ListTranslations.GetTranslated(x => x.ShortDescription);

        [Name("Bisher")]
        [TypeConverter(typeof(WabstiCBooleanConverter))]
        public bool Incumbent { get; set; }

        [Name("Titel")]
        public string Title { get; set; } = string.Empty;

        [Ignore]
        public ICollection<ProportionalElectionCandidateTranslation> CandidateTranslations { get; set; } = new HashSet<ProportionalElectionCandidateTranslation>();

        [Name("TitelBeruf")]
        public string OccupationTitle => CandidateTranslations.GetTranslated(x => x.OccupationTitle, true);

        [Name("KandidatBezImExp")]
        public string Description => string.Join(
            ", ",
            new[]
            {
                    $"{LastName} {FirstName}",
                    BirthYear.ToString(CultureInfo.InvariantCulture),
                    Title,
                    Occupation,
                    Locality,
                    Incumbent ? WabstiCConstants.IncumbentText : null,
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

        [Name("Geschlecht")]
        [TypeConverter(typeof(WabstiCSexConverter))]
        public SexType Sex { get; set; }

        [Name("ListeNr")]
        public string ListNumber { get; set; } = string.Empty;

        [Name("GeLfNr")]
        public Guid PoliticalBusinessId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }
    }
}
