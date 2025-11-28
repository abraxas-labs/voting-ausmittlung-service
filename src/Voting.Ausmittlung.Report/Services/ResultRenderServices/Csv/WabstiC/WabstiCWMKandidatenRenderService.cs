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
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWMKandidatenRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElection> _repo;

    public WabstiCWMKandidatenRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, MajorityElection> repo)
    {
        _templateService = templateService;
        _repo = repo;
    }

    [Flags]
    private enum CandidateParticipationState
    {
        None = 0,
        PrimaryElection = 1 << 0,
        SecondaryElection = 1 << 1,
        SecondaryElection2 = 1 << 2,
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var result = await _repo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .Select(x => new
            {
                PrimaryResults = x.EndResult!.CandidateEndResults
                    .OrderBy(y => y.Candidate.Position)
                    .Select(y => new Data
                    {
                        DomainOfInfluenceType = y.MajorityElectionEndResult.MajorityElection.DomainOfInfluence.Type,
                        PoliticalBusinessNumber = y.MajorityElectionEndResult.MajorityElection.PoliticalBusinessNumber,
                        CandidateNumber = y.Candidate.Number,
                        CandidateId = y.CandidateId,
                        LastName = y.Candidate.PoliticalLastName,
                        FirstName = y.Candidate.PoliticalFirstName,
                        Party = y.Candidate.Translations.First().PartyShortDescription,
                        Occupation = y.Candidate.Translations.First().Occupation,
                        OccupationTitle = y.Candidate.Translations.First().OccupationTitle,
                        Title = y.Candidate.Title,
                        Locality = y.Candidate.Locality,
                        YearOfBirth = y.Candidate.DateOfBirth.HasValue ? y.Candidate.DateOfBirth.Value.Year : WabstiCConstants.CandidateDefaultBirthYear,
                        Incumbent = y.Candidate.Incumbent,
                        Sex = y.Candidate.Sex,
                        ReportingType = y.Candidate.ReportingType,
                        ParticipationState = CandidateParticipationState.PrimaryElection,
                        Elected = !x.EndResult.Finalized || y.State == MajorityElectionCandidateEndResultState.Pending
                            ? (bool?)null
                            : y.State == MajorityElectionCandidateEndResultState.Elected
                              || y.State == MajorityElectionCandidateEndResultState.AbsoluteMajorityAndElected,
                        VoteCount = y.VoteCount,
                        AbsoluteMajority = y.MajorityElectionEndResult.Calculation.AbsoluteMajority,
                        ElectionId = y.MajorityElectionEndResult.MajorityElectionId,
                        IndividualVoteCount = y.MajorityElectionEndResult.IndividualVoteCount,
                        ElectionUnionIds = y.MajorityElectionEndResult.MajorityElection.MajorityElectionUnionEntries
                            .Select(z => z.MajorityElectionUnionId)
                            .OrderBy(z => z)
                            .ToList(),
                        VoteCountPercent = y.MajorityElectionEndResult.TotalCandidateVoteCountInclIndividual != 0 ? y.VoteCount / (decimal)y.MajorityElectionEndResult.TotalCandidateVoteCountInclIndividual : null,
                        TotalCandidateVoteCountInclIndividual = y.MajorityElectionEndResult.TotalCandidateVoteCountInclIndividual,
                    })
                    .ToList(),
                SecondaryElectionIds = x.SecondaryMajorityElections
                    .OrderBy(y => y.PoliticalBusinessNumber)
                    .Take(WabstiCConstants.MaxSecondaryMajorityElectionsSupported)
                    .Select(y => y.Id)
                    .ToList(),
                SecondaryCandidateResults = x.SecondaryMajorityElections
                    .OrderBy(y => y.PoliticalBusinessNumber)
                    .Take(WabstiCConstants.MaxSecondaryMajorityElectionsSupported)
                    .SelectMany(y => y.EndResult!.CandidateEndResults)
                    .OrderBy(y => y.Candidate.Position)
                    .Select(y => new Data
                    {
                        DomainOfInfluenceType = y.SecondaryMajorityElectionEndResult.SecondaryMajorityElection.PrimaryMajorityElection
                            .DomainOfInfluence.Type,
                        PoliticalBusinessNumber =
                            y.SecondaryMajorityElectionEndResult.SecondaryMajorityElection.PoliticalBusinessNumber,
                        CandidateId = y.Candidate.CandidateReferenceId ?? y.CandidateId,
                        CandidateNumber = y.Candidate.Number,
                        LastName = y.Candidate.PoliticalLastName,
                        FirstName = y.Candidate.PoliticalFirstName,
                        Party = y.Candidate.Translations.First().PartyShortDescription,
                        Occupation = y.Candidate.Translations.First().Occupation,
                        OccupationTitle = y.Candidate.Translations.First().OccupationTitle,
                        Title = y.Candidate.Title,
                        Locality = y.Candidate.Locality,
                        YearOfBirth = y.Candidate.DateOfBirth.HasValue ? y.Candidate.DateOfBirth.Value.Year : WabstiCConstants.CandidateDefaultBirthYear,
                        Incumbent = y.Candidate.Incumbent,
                        Sex = y.Candidate.Sex,
                        ReportingType = y.Candidate.ReportingType,
                        Elected = !x.EndResult.Finalized || y.State == MajorityElectionCandidateEndResultState.Pending
                            ? (bool?)null
                            : y.State == MajorityElectionCandidateEndResultState.Elected
                              || y.State == MajorityElectionCandidateEndResultState.AbsoluteMajorityAndElected,
                        VoteCount = y.VoteCount,
                        ElectionId = y.SecondaryMajorityElectionEndResult.SecondaryMajorityElectionId,
                        PrimaryElectionId = y.SecondaryMajorityElectionEndResult.SecondaryMajorityElection.PrimaryMajorityElectionId,
                        IndividualVoteCount = y.SecondaryMajorityElectionEndResult.IndividualVoteCount,
                        ElectionUnionIds = y.SecondaryMajorityElectionEndResult.SecondaryMajorityElection.PrimaryMajorityElection.MajorityElectionUnionEntries
                            .Select(z => z.MajorityElectionUnionId)
                            .OrderBy(z => z)
                            .ToList(),
                        AbsoluteMajority = y.SecondaryMajorityElectionEndResult.Calculation.AbsoluteMajority,
                        VoteCountPercent = y.SecondaryMajorityElectionEndResult.TotalCandidateVoteCountInclIndividual != 0 ? y.VoteCount / (decimal)y.SecondaryMajorityElectionEndResult.TotalCandidateVoteCountInclIndividual : null,
                        TotalCandidateVoteCountInclIndividual = y.SecondaryMajorityElectionEndResult.TotalCandidateVoteCountInclIndividual,
                    })
                    .ToList(),
            })
            .ToListAsync(ct);

        var candidateResults = result
            .Select(x => RemoveCountToIndividualCandidatesAndAdjustTotals(x.PrimaryResults, x.SecondaryElectionIds, x.SecondaryCandidateResults))
            .SelectMany(x => Merge(x.FilteredPrimaryResults, x.SecondaryElectionIds, x.FilteredSecondaryResults));

        return _templateService.RenderToCsv(
            ctx,
            candidateResults);
    }

    /// <summary>
    /// Attaches secondary election results to the primary results if possible.
    /// Max. 2 secondary elections are supported (limitation of wabstiC).
    /// If there are results of these 2 secondary elections which are not part of a primary election, these are added at the end.
    /// </summary>
    /// <param name="primaryResults">The primary results.</param>
    /// <param name="secondaryElectionIds">The secondary election ids to use. 2 at max.</param>
    /// <param name="secondaryResults">The secondary results.</param>
    /// <returns>The merged results.</returns>
    private IEnumerable<Data> Merge(
        IReadOnlyCollection<Data> primaryResults,
        IReadOnlyCollection<Guid> secondaryElectionIds,
        IReadOnlyCollection<Data> secondaryResults)
    {
        // Without any primary results, we cannot create any meaningful data, even for the individual candidate.
        if (primaryResults.Count == 0)
        {
            return primaryResults;
        }

        if (secondaryElectionIds.Count == 0)
        {
            return primaryResults.Append(CreateIndividualCandidateData(primaryResults, secondaryResults, Guid.Empty, Guid.Empty));
        }

        var secondaryElectionId1 = secondaryElectionIds.ElementAtOrDefault(0);
        var secondaryElectionId2 = secondaryElectionIds.ElementAtOrDefault(1);

        var groupedSecondaryResults = secondaryResults
            .GroupBy(x => (x.PrimaryElectionId, x.CandidateId))
            .ToDictionary(
                x => x.Key,
                x => x.ToDictionary(y => y.ElectionId));

        // merge secondary into primaries
        // then, add the individual candidate
        // then, add candidates which are not in a primary election separately
        return MergeIntoPrimary(primaryResults, groupedSecondaryResults, secondaryElectionId1, secondaryElectionId2)
            .Append(CreateIndividualCandidateData(primaryResults, secondaryResults, secondaryElectionId1, secondaryElectionId2))
            .Concat(MergeSecondary(groupedSecondaryResults.Values, secondaryElectionId1, secondaryElectionId2));
    }

    private IEnumerable<Data> MergeIntoPrimary(
        IEnumerable<Data> primaryResults,
        Dictionary<(Guid? PrimaryElectionId, Guid CandidateId), Dictionary<Guid, Data>> groupedSecondaryResults,
        Guid secondaryElectionId1,
        Guid secondaryElectionId2)
    {
        foreach (var entry in primaryResults)
        {
            var key = (entry.PrimaryElectionId, entry.CandidateId);
            if (!groupedSecondaryResults.Remove(key, out var secondaryCandidateResults))
            {
                yield return entry;
                continue;
            }

            TryAddSecondary(entry, secondaryElectionId1, secondaryCandidateResults);
            TryAddSecondary2(entry, secondaryElectionId2, secondaryCandidateResults);

            yield return entry;
        }
    }

    private IEnumerable<Data> MergeSecondary(
        IEnumerable<Dictionary<Guid, Data>> secondaryResultGroups,
        Guid secondaryElectionId1,
        Guid secondaryElectionId2)
    {
        foreach (var secondaryResults in secondaryResultGroups)
        {
            var participationState = CandidateParticipationState.None;
            if (secondaryResults.TryGetValue(secondaryElectionId1, out var result1))
            {
                participationState |= CandidateParticipationState.SecondaryElection;
            }

            if (secondaryResults.TryGetValue(secondaryElectionId2, out var result2))
            {
                participationState |= CandidateParticipationState.SecondaryElection2;
            }

            if (participationState == CandidateParticipationState.None)
            {
                continue;
            }

            yield return new Data
            {
                DomainOfInfluenceType = result1?.DomainOfInfluenceType ?? result2!.DomainOfInfluenceType,
                PoliticalBusinessNumber = result1?.PoliticalBusinessNumber ?? result2!.PoliticalBusinessNumber,
                CandidateId = result1?.CandidateId ?? result2!.CandidateId,
                CandidateNumber = result1?.CandidateNumber ?? result2!.CandidateNumber,
                LastName = result1?.LastName ?? result2!.LastName,
                FirstName = result1?.FirstName ?? result2!.FirstName,
                Party = result1?.Party ?? result2?.Party ?? string.Empty,
                Occupation = result1?.Occupation ?? result2?.Occupation ?? string.Empty,
                OccupationTitle = result1?.OccupationTitle ?? result2?.OccupationTitle ?? string.Empty,
                Title = result1?.Title ?? result2!.Title,
                Locality = result1?.Locality ?? result2!.CandidateNumber,
                YearOfBirth = result1?.YearOfBirth ?? result2!.YearOfBirth,
                IncumbentSecondary = result1?.Incumbent,
                IncumbentSecondary2 = result2?.Incumbent,
                Sex = result1?.Sex ?? result2!.Sex,
                ElectedSecondary = result1?.Elected,
                ElectedSecondary2 = result2?.Elected,
                VoteCountSecondary = result1?.VoteCount,
                VoteCountSecondary2 = result2?.VoteCount,
                ElectionId = result1?.ElectionId ?? result2!.ElectionId,
                ParticipationState = participationState,
                IndividualVoteCountSecondary = result1?.IndividualVoteCount,
                IndividualVoteCountSecondary2 = result2?.IndividualVoteCount,
                ElectionUnionIds = result1?.ElectionUnionIds ?? result2!.ElectionUnionIds,
                AbsoluteMajority = result1?.AbsoluteMajority ?? result2?.AbsoluteMajority,
                AbsoluteMajoritySecondary = result1?.AbsoluteMajority,
                AbsoluteMajoritySecondary2 = result2?.AbsoluteMajority,
                VoteCountPercentSecondary = result1?.VoteCountPercent,
                VoteCountPercentSecondary2 = result2?.VoteCountPercent,
            };
        }
    }

    private void TryAddSecondary(Data entry, Guid electionId, IReadOnlyDictionary<Guid, Data> secondaryCandidateResults)
    {
        if (!secondaryCandidateResults.TryGetValue(electionId, out var result))
        {
            return;
        }

        entry.ParticipationState |= CandidateParticipationState.SecondaryElection;
        entry.ElectedSecondary = result.Elected;
        entry.IncumbentSecondary = result.Incumbent;
        entry.VoteCountSecondary = result.VoteCount;
        entry.IndividualVoteCountSecondary = result.IndividualVoteCount;
        entry.AbsoluteMajoritySecondary = result.AbsoluteMajority;
        entry.VoteCountPercentSecondary = result.VoteCountPercent;
    }

    private void TryAddSecondary2(Data entry, Guid electionId, IReadOnlyDictionary<Guid, Data> secondaryCandidateResults)
    {
        if (!secondaryCandidateResults.TryGetValue(electionId, out var result))
        {
            return;
        }

        entry.ParticipationState |= CandidateParticipationState.SecondaryElection2;
        entry.ElectedSecondary2 = result.Elected;
        entry.IncumbentSecondary2 = result.Incumbent;
        entry.VoteCountSecondary2 = result.VoteCount;
        entry.IndividualVoteCountSecondary2 = result.IndividualVoteCount;
        entry.AbsoluteMajoritySecondary2 = result.AbsoluteMajority;
        entry.VoteCountPercentSecondary2 = result.VoteCountPercent;
    }

    private Data CreateIndividualCandidateData(
        IReadOnlyCollection<Data> primaryResults,
        IReadOnlyCollection<Data> secondaryResults,
        Guid secondaryElectionId1,
        Guid secondaryElectionId2)
    {
        var primaryResult = primaryResults.First();
        var secondaryResult1 = secondaryResults.FirstOrDefault(x => x.ElectionId == secondaryElectionId1);
        var secondaryResult2 = secondaryResults.FirstOrDefault(x => x.ElectionId == secondaryElectionId2);

        var data = new Data
        {
            DomainOfInfluenceType = primaryResult.DomainOfInfluenceType,
            CandidateNumber = WabstiCConstants.IndividualMajorityCandidateNumber,
            LastName = WabstiCConstants.IndividualMajorityCandidateLastName,
            ElectionId = primaryResult.ElectionId,
            VoteCount = primaryResult.IndividualVoteCount,
            ElectionUnionIds = primaryResult.ElectionUnionIds,
            PoliticalBusinessNumber = primaryResult.PoliticalBusinessNumber,
            Incumbent = false,
            ParticipationState = CandidateParticipationState.PrimaryElection,
            Elected = primaryResult.Elected == null ? null : false,
            AbsoluteMajority = primaryResult.AbsoluteMajority,
            IndividualVoteCount = primaryResult.IndividualVoteCount,
            PrimaryElectionId = primaryResult.PrimaryElectionId,
            YearOfBirth = WabstiCConstants.IndividualMajorityCandidateYearOfBirth,
            VoteCountPercent = primaryResult.TotalCandidateVoteCountInclIndividual != null && primaryResult.TotalCandidateVoteCountInclIndividual != 0 ? primaryResult.IndividualVoteCount / (decimal)primaryResult.TotalCandidateVoteCountInclIndividual : null,
        };

        if (secondaryResult1 != null)
        {
            data.ParticipationState |= CandidateParticipationState.SecondaryElection;
            data.ElectedSecondary = secondaryResult1.Elected == null ? null : false;
            data.IncumbentSecondary = false;
            data.VoteCountSecondary = secondaryResult1.IndividualVoteCount;
            data.IndividualVoteCountSecondary = secondaryResult1.IndividualVoteCount;
            data.AbsoluteMajoritySecondary = secondaryResult1.AbsoluteMajority;
            data.VoteCountPercentSecondary = secondaryResult1.TotalCandidateVoteCountInclIndividual != null && secondaryResult1.TotalCandidateVoteCountInclIndividual != 0 ? secondaryResult1.IndividualVoteCount / (decimal)secondaryResult1.TotalCandidateVoteCountInclIndividual : null;
        }

        if (secondaryResult2 != null)
        {
            data.ParticipationState |= CandidateParticipationState.SecondaryElection2;
            data.ElectedSecondary2 = secondaryResult2.Elected == null ? null : false;
            data.IncumbentSecondary2 = false;
            data.VoteCountSecondary2 = secondaryResult2.IndividualVoteCount;
            data.IndividualVoteCountSecondary2 = secondaryResult2.IndividualVoteCount;
            data.AbsoluteMajoritySecondary2 = secondaryResult2.AbsoluteMajority;
            data.VoteCountPercentSecondary2 = secondaryResult2.TotalCandidateVoteCountInclIndividual != null && secondaryResult2.TotalCandidateVoteCountInclIndividual != 0 ? secondaryResult2.IndividualVoteCount / (decimal)secondaryResult2.TotalCandidateVoteCountInclIndividual : null;
        }

        return data;
    }

    private (IReadOnlyCollection<Data> FilteredPrimaryResults, IReadOnlyCollection<Guid> SecondaryElectionIds, IReadOnlyCollection<Data> FilteredSecondaryResults) RemoveCountToIndividualCandidatesAndAdjustTotals(
        IReadOnlyCollection<Data> primaryResults,
        IReadOnlyCollection<Guid> secondaryElectionIds,
        IReadOnlyCollection<Data> secondaryResults)
    {
        var rows = primaryResults.Concat(secondaryResults).ToList();

        var filteredPrimaryResults = new List<Data>();
        var filteredSecondaryResults = new List<Data>();

        var rowsGroupedByElectionId = rows
            .GroupBy(x => x.ElectionId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var additionalIndividualVoteCountByElectionId = rowsGroupedByElectionId
            .Select(x => x.Key)
            .ToDictionary(x => x, _ => 0);

        foreach (var (_, electionRows) in rowsGroupedByElectionId)
        {
            foreach (var row in electionRows)
            {
                if (row.ReportingType is not MajorityElectionCandidateReportingType.CountToIndividual)
                {
                    if (!secondaryElectionIds.Contains(row.ElectionId))
                    {
                        filteredPrimaryResults.Add(row);
                    }
                    else
                    {
                        filteredSecondaryResults.Add(row);
                    }

                    continue;
                }

                additionalIndividualVoteCountByElectionId[row.ElectionId] += row.VoteCount.GetValueOrDefault();
            }
        }

        foreach (var electionId in rowsGroupedByElectionId.Keys)
        {
            var additionalIndividualVoteCount = additionalIndividualVoteCountByElectionId[electionId];

            foreach (var row in rowsGroupedByElectionId[electionId])
            {
                row.IndividualVoteCount += additionalIndividualVoteCount;
            }
        }

        return (filteredPrimaryResults, secondaryElectionIds, filteredSecondaryResults);
    }

    private class Data
    {
        [Name("Art")]
        [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
        public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

        [Name("SortGeschaeft")]
        public string PoliticalBusinessNumber { get; set; } = string.Empty;

        [Ignore]
        public Guid CandidateId { get; set; }

        [Name("KNR")]
        public string CandidateNumber { get; set; } = string.Empty;

        [Name("Nachname")]
        public string LastName { get; set; } = string.Empty;

        [Name("Vorname")]
        public string FirstName { get; set; } = string.Empty;

        [Name("Partei")]
        public string Party { get; set; } = string.Empty;

        [Name("Beruf")]
        public string Occupation { get; set; } = string.Empty;

        [Name("Titel und Berufsbezeichnung")]
        public string OccupationTitle { get; set; } = string.Empty;

        [Name("Titel")]
        public string Title { get; set; } = string.Empty;

        [Name("Wohnort")]
        public string Locality { get; set; } = string.Empty;

        [Name("Jahrgang")]
        public int YearOfBirth { get; set; }

        [Name("Bisher")]
        [TypeConverter(typeof(WabstiCBooleanConverter))]
        public bool? Incumbent { get; set; }

        [Name("Gewaehlt")]
        [TypeConverter(typeof(WabstiCBooleanConverter))]
        public bool? Elected { get; set; }

        [Name("Stimmen")]
        public int? VoteCount { get; set; }

        [Name("AbsolutesMehr")]
        public int? AbsoluteMajority { get; set; }

        [Name("StimmenProz")]
        [TypeConverter(typeof(WabstiCPercentDecimalConverter))]
        public decimal? VoteCountPercent { get; set; }

        [Name("KandidatHwNw")]
        [TypeConverter(typeof(WabstiCIntEnumConverter))]
        public CandidateParticipationState ParticipationState { get; set; }

        [Name("BisherNW")]
        [TypeConverter(typeof(WabstiCBooleanConverter))]
        public bool? IncumbentSecondary { get; set; }

        [Name("GewaehltNW")]
        [TypeConverter(typeof(WabstiCBooleanConverter))]
        public bool? ElectedSecondary { get; set; }

        [Name("StimmenNW")]
        public int? VoteCountSecondary { get; set; }

        [Name("AbsolutesMehrNW")]
        public int? AbsoluteMajoritySecondary { get; set; }

        [Name("StimmenProzNW")]
        [TypeConverter(typeof(WabstiCPercentDecimalConverter))]
        public decimal? VoteCountPercentSecondary { get; set; }

        [Name("BisherNW2")]
        [TypeConverter(typeof(WabstiCBooleanConverter))]
        public bool? IncumbentSecondary2 { get; set; }

        [Name("GewaehltNW2")]
        [TypeConverter(typeof(WabstiCBooleanConverter))]
        public bool? ElectedSecondary2 { get; set; }

        [Name("StimmenNW2")]
        public int? VoteCountSecondary2 { get; set; }

        [Name("AbsolutesMehrNW2")]
        public int? AbsoluteMajoritySecondary2 { get; set; }

        [Name("StimmenProzNW2")]
        [TypeConverter(typeof(WabstiCPercentDecimalConverter))]
        public decimal? VoteCountPercentSecondary2 { get; set; }

        [Name("Geschlecht")]
        [TypeConverter(typeof(WabstiCSexConverter))]
        public SexType Sex { get; set; }

        [Name("StimTotVereinzelteHW")]
        public int? IndividualVoteCount { get; set; }

        [Name("StimTotVereinzelteNW")]
        public int? IndividualVoteCountSecondary { get; set; }

        [Name("StimTotVereinzelteNW2")]
        public int? IndividualVoteCountSecondary2 { get; set; }

        [Name("GeLfNr")]
        public Guid ElectionId { get; set; }

        [Ignore]
        public Guid? PrimaryElectionId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }

        [Ignore]
        public int? TotalCandidateVoteCountInclIndividual { get; set; }

        [Ignore]
        public MajorityElectionCandidateReportingType ReportingType { get; set; }
    }
}
