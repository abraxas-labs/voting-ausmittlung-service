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
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWMWahlRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElection> _repo;

    public WabstiCWMWahlRenderService(TemplateService templateService, IDbRepository<DataContext, MajorityElection> repo)
    {
        _templateService = templateService;
        _repo = repo;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var secondaryElections = await _repo.Query()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.SecondaryMajorityElections)
            .OrderByNaturalOrder()
            .Include(x => x.EndResult)
            .Include(x => x.Translations)
            .ToListAsync(ct);

        var results = await _repo.Query()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .OrderByNaturalOrder()
            .Select(x => new Data
            {
                PoliticalBusinessId = x.Id,
                PoliticalBusinessNumber = x.PoliticalBusinessNumber,
                DomainOfInfluenceType = x.DomainOfInfluence.Type,
                ContestDate = x.Contest.Date,
                ElectionOfficialDescription = x.Translations.First().OfficialDescription,
                ElectionShortDescription = x.Translations.First().ShortDescription,
                DomainOfInfluenceBfs = x.DomainOfInfluence.Bfs,
                DomainOfInfluenceCanton = x.DomainOfInfluence.Canton,
                DomainOfInfluenceCode = x.DomainOfInfluence.Code,
                DomainOfInfluenceName = x.DomainOfInfluence.Name,
                TotalCountOfVotes = x.EndResult!.TotalCandidateVoteCountInclIndividual,
                CountOfVotesPerMandate = x.EndResult!.TotalCandidateVoteCountInclIndividual / x.NumberOfMandates,
                CountOfDoneCountingCircles = x.EndResult!.CountOfDoneCountingCircles,
                TotalCountOfCountingCircles = x.EndResult!.TotalCountOfCountingCircles,
                Finalized = x.EndResult!.Finalized,
                DomainOfInfluenceSortNumber = x.DomainOfInfluence.SortNumber,
                ElectionUnionIds = x.MajorityElectionUnionEntries
                    .Select(y => y.MajorityElectionUnionId)
                    .OrderBy(y => y)
                    .ToList(),
                AbsoluteMajority = x.EndResult!.Calculation.AbsoluteMajority,
            })
            .ToListAsync(ct);

        AttachSecondaryElections(results, secondaryElections);

        return _templateService.RenderToCsv(
            ctx,
            results);
    }

    private void AttachSecondaryElections(
        IEnumerable<Data> results,
        IEnumerable<SecondaryMajorityElection> secondaryElections)
    {
        var secondaryElectionsByPrimaryId = secondaryElections
            .GroupBy(x => x.PrimaryMajorityElectionId)
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var result in results)
        {
            if (!secondaryElectionsByPrimaryId.TryGetValue(result.PoliticalBusinessId, out var selectedSecondaryElections))
            {
                continue;
            }

            var secondaryElection1 = selectedSecondaryElections.FirstOrDefault();
            if (secondaryElection1 == null)
            {
                continue;
            }

            result.SecondaryElectionOfficialDescription = secondaryElection1.OfficialDescription;
            result.SecondaryElectionShortDescription = secondaryElection1.ShortDescription;
            result.SecondaryElectionTotalCountOfVotes = secondaryElection1.EndResult?.TotalCandidateVoteCountInclIndividual;
            result.SecondaryElectionCountOfVotesPerMandate = secondaryElection1.EndResult?.TotalCandidateVoteCountInclIndividual / secondaryElection1.NumberOfMandates;

            var secondaryElection2 = selectedSecondaryElections.ElementAtOrDefault(1);
            if (secondaryElection2 == null)
            {
                continue;
            }

            result.SecondaryElection2OfficialDescription = secondaryElection2.OfficialDescription;
            result.SecondaryElection2ShortDescription = secondaryElection2.ShortDescription;
            result.SecondaryElection2TotalCountOfVotes = secondaryElection2.EndResult?.TotalCandidateVoteCountInclIndividual;
            result.SecondaryElection2CountOfVotesPerMandate = secondaryElection2.EndResult?.TotalCandidateVoteCountInclIndividual / secondaryElection2.NumberOfMandates;
        }
    }

    private class Data
    {
        [Name("Art")]
        [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
        public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

        [Name("Typ")]
        public string Typ => "MW";

        [Name("SortGeschaeft")]
        public string PoliticalBusinessNumber { get; set; } = string.Empty;

        [Ignore]
        public int TotalCountOfCountingCircles { get; set; }

        [Ignore]
        public int CountOfDoneCountingCircles { get; set; }

        [Name("AnzGdePendent")]
        public int CountOfPendingCountingCircles => TotalCountOfCountingCircles - CountOfDoneCountingCircles;

        [Name("AbsolutesMehr")]
        public int? AbsoluteMajority { get; set; }

        [Name("GeBezKurz")]
        public string ElectionShortDescription { get; set; } = string.Empty;

        [Name("GeBezOffiziell")]
        public string ElectionOfficialDescription { get; set; } = string.Empty;

        [Name("Ausmittlungsstand")]
        [TypeConverter(typeof(WabstiCEndResultFinalizedConverter))]
        public bool Finalized { get; set; }

        [Name("Sonntag")]
        [TypeConverter(typeof(WabstiCDateConverter))]
        public DateTime ContestDate { get; set; }

        [Name("Kanton")]
        [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
        public DomainOfInfluenceCanton DomainOfInfluenceCanton { get; set; }

        [Name("MassgebStimmen")]
        public int TotalCountOfVotes { get; set; }

        [Name("EinfacheStimmen")]
        public int CountOfVotesPerMandate { get; set; }

        [Name("SortWahlkreis")]
        public int DomainOfInfluenceSortNumber { get; set; }

        [Name("WahlkreisBez")]
        public string DomainOfInfluenceName { get; set; } = string.Empty;

        [Name("BfsNrWKreis")]
        public string DomainOfInfluenceBfs { get; set; } = string.Empty;

        [Name("Wahlkreis-Code")]
        public string DomainOfInfluenceCode { get; set; } = string.Empty;

        [Name("GeBezKurzNW")]
        public string SecondaryElectionShortDescription { get; set; } = string.Empty;

        [Name("GeBezOffiziellNW")]
        public string SecondaryElectionOfficialDescription { get; set; } = string.Empty;

        [Name("MassgebStimmenNW")]
        public int? SecondaryElectionTotalCountOfVotes { get; set; }

        [Name("EinfacheStimmenNW")]
        public int? SecondaryElectionCountOfVotesPerMandate { get; set; }

        [Name("GeBezKurzNW2")]
        public string SecondaryElection2ShortDescription { get; set; } = string.Empty;

        [Name("GeBezOffiziellNW2")]
        public string SecondaryElection2OfficialDescription { get; set; } = string.Empty;

        [Name("MassgebStimmenNW2")]
        public int? SecondaryElection2TotalCountOfVotes { get; set; }

        [Name("EinfacheStimmenNW2")]
        public int? SecondaryElection2CountOfVotesPerMandate { get; set; }

        [Name("GeLfNr")]
        public Guid PoliticalBusinessId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }
    }
}
