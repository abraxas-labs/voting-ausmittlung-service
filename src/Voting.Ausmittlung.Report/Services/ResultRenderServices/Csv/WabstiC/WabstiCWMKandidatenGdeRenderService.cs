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
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWMKandidatenGdeRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElection> _repo;

    public WabstiCWMKandidatenGdeRenderService(TemplateService templateService, IDbRepository<DataContext, MajorityElection> repo)
    {
        _templateService = templateService;
        _repo = repo;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var result = await _repo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .Include(x => x.MajorityElectionUnionEntries)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.SecondaryMajorityElections)
            .Include(x => x.Results).ThenInclude(x => x.CountingCircle)
            .Include(x => x.Results).ThenInclude(x => x.CandidateResults).ThenInclude(x => x.Candidate)
            .Include(x => x.Results).ThenInclude(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults).ThenInclude(x => x.Candidate)
            .ToListAsync(ct);

        var candidateResults = result.SelectMany(Build);

        return _templateService.RenderToCsv(
            ctx,
            candidateResults);
    }

    /// <summary>
    /// Builds a list of candidate results from the majority election and its secondary elections.
    /// Candidate results contains primary candidate results with attached secondary candidate results (if existing),
    /// secondary candidate results (for max. 2 secondary elections; no more than 2 are supported by wabstiC)
    /// for candidates which are not registered in the primary election
    /// and a virtual individual candidate for each primary election with secondary counts attached.
    /// </summary>
    /// <param name="election">The primary election.</param>
    /// <returns>The candidate results.</returns>
    private IEnumerable<CandidateData> Build(MajorityElection election)
    {
        foreach (var result in election.Results)
        {
            MajorityElectionResultUtils.RemoveCountToIndividualCandidatesAndAdjustTotals(result);
        }

        var sortedResults = election.Results
            .OrderBy(x => x.CountingCircle.Code)
            .ThenBy(x => x.CountingCircle.Name);

        var secondaryElectionIds = election.SecondaryMajorityElections
            .OrderBy(y => y.PoliticalBusinessNumber)
            .Take(WabstiCConstants.MaxSecondaryMajorityElectionsSupported)
            .Select(y => y.Id)
            .ToList();

        var secondaryElectionId1 = secondaryElectionIds.ElementAtOrDefault(0);
        var secondaryElectionId2 = secondaryElectionIds.ElementAtOrDefault(1);

        foreach (var result in sortedResults)
        {
            var submissionDone = result.State.IsSubmissionDone();
            var groupedSecondaryResults = result.SecondaryMajorityElectionResults
                .ToDictionary(x => x.SecondaryMajorityElectionId);
            groupedSecondaryResults.TryGetValue(secondaryElectionId1, out var result1);
            groupedSecondaryResults.TryGetValue(secondaryElectionId2, out var result2);

            var candidateResults1 = result1?.CandidateResults
                                        .ToDictionary(x => x.Candidate.CandidateReferenceId ?? x.CandidateId)
                                    ?? new Dictionary<Guid, SecondaryMajorityElectionCandidateResult>();
            var candidateResults2 = result2?.CandidateResults
                                        .ToDictionary(x => x.Candidate.CandidateReferenceId ?? x.CandidateId)
                                    ?? new Dictionary<Guid, SecondaryMajorityElectionCandidateResult>();

            // add candidates of primary election
            foreach (var candidateResult in result.CandidateResults.OrderBy(x => x.CandidatePosition))
            {
                candidateResults1.Remove(candidateResult.CandidateId, out var candidateResult1);
                candidateResults2.Remove(candidateResult.CandidateId, out var candidateResult2);

                yield return new CandidateData
                {
                    DomainOfInfluenceType = result.MajorityElection.DomainOfInfluence.Type,
                    Bfs = result.CountingCircle.Bfs,
                    Code = result.CountingCircle.Code,
                    CandidateNumber = candidateResult.Candidate.Number,
                    ElectionId = result.MajorityElectionId,
                    VoteCount = submissionDone ? candidateResult.VoteCount : null,
                    ElectionUnionIds = result.MajorityElection.MajorityElectionUnionEntries
                        .Select(z => z.MajorityElectionUnionId)
                        .OrderBy(z => z)
                        .ToList(),
                    PoliticalBusinessNumber = result.MajorityElection.PoliticalBusinessNumber,
                    VoteCountSecondary = submissionDone ? candidateResult1?.VoteCount : null,
                    VoteCountSecondary2 = submissionDone ? candidateResult2?.VoteCount : null,
                };
            }

            // add candidates not part of primary election
            foreach (var candidateResult in candidateResults1.Values.OrderBy(x => x.CandidatePosition))
            {
                yield return new CandidateData
                {
                    DomainOfInfluenceType = result.MajorityElection.DomainOfInfluence.Type,
                    Bfs = result.CountingCircle.Bfs,
                    Code = result.CountingCircle.Code,
                    CandidateNumber = candidateResult.Candidate.Number,
                    ElectionId = result.MajorityElectionId,
                    ElectionUnionIds = result.MajorityElection.MajorityElectionUnionEntries
                        .Select(z => z.MajorityElectionUnionId)
                        .OrderBy(z => z)
                        .ToList(),
                    PoliticalBusinessNumber = result.MajorityElection.PoliticalBusinessNumber,
                    VoteCountSecondary = submissionDone ? candidateResult.VoteCount : null,
                };
            }

            foreach (var candidateResult in candidateResults2.Values.OrderBy(x => x.CandidatePosition))
            {
                yield return new CandidateData
                {
                    DomainOfInfluenceType = result.MajorityElection.DomainOfInfluence.Type,
                    Bfs = result.CountingCircle.Bfs,
                    Code = result.CountingCircle.Code,
                    CandidateNumber = candidateResult.Candidate.Number,
                    ElectionId = result.MajorityElectionId,
                    ElectionUnionIds = result.MajorityElection.MajorityElectionUnionEntries
                        .Select(z => z.MajorityElectionUnionId)
                        .OrderBy(z => z)
                        .ToList(),
                    PoliticalBusinessNumber = result.MajorityElection.PoliticalBusinessNumber,
                    VoteCountSecondary2 = submissionDone ? candidateResult.VoteCount : null,
                };
            }

            // add individual candidate
            yield return new CandidateData
            {
                DomainOfInfluenceType = result.MajorityElection.DomainOfInfluence.Type,
                Bfs = result.CountingCircle.Bfs,
                Code = result.CountingCircle.Code,
                CandidateNumber = WabstiCConstants.IndividualMajorityCandidateNumber,
                ElectionId = result.MajorityElectionId,
                VoteCount = submissionDone ? result.IndividualVoteCount : null,
                ElectionUnionIds = result.MajorityElection.MajorityElectionUnionEntries
                    .Select(z => z.MajorityElectionUnionId)
                    .OrderBy(z => z)
                    .ToList(),
                PoliticalBusinessNumber = result.MajorityElection.PoliticalBusinessNumber,
                VoteCountSecondary = submissionDone ? result1?.IndividualVoteCount : null,
                VoteCountSecondary2 = submissionDone ? result2?.IndividualVoteCount : null,
            };
        }
    }

    private class CandidateData
    {
        [Name("Art")]
        [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
        public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

        [Name("SortGeschaeft")]
        public string PoliticalBusinessNumber { get; set; } = string.Empty;

        [Name("BfsNrGemeinde")]
        public string Bfs { get; set; } = string.Empty;

        [Name("EinheitCode")]
        public string Code { get; set; } = string.Empty;

        [Name("KNR")]
        public string CandidateNumber { get; set; } = string.Empty;

        [Name("Stimmen")]
        public int? VoteCount { get; set; }

        [Name("StimmenNW")]
        public int? VoteCountSecondary { get; set; }

        [Name("StimmenNW2")]
        public int? VoteCountSecondary2 { get; set; }

        [Name("GeLfNr")]
        public Guid ElectionId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }
    }
}
