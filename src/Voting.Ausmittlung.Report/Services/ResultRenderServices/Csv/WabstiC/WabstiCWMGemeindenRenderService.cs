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
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWMGemeindenRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElection> _repo;
    private readonly WabstiCContestDetailsAttacher _contestDetailsAttacher;

    public WabstiCWMGemeindenRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, MajorityElection> repo,
        WabstiCContestDetailsAttacher contestDetailsAttacher)
    {
        _templateService = templateService;
        _repo = repo;
        _contestDetailsAttacher = contestDetailsAttacher;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = await _repo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .Select(x => new
            {
                PrimaryResults = x.Results
                    .OrderBy(y => y.CountingCircle.Code)
                    .ThenBy(y => y.CountingCircle.Name)
                    .Select(y => new Data
                    {
                        CountingCircleId = y.CountingCircleId,
                        DomainOfInfluenceType = y.MajorityElection.DomainOfInfluence.Type,
                        CountingCircleBfs = y.CountingCircle.Bfs,
                        CountingCircleCode = y.CountingCircle.Code,
                        PoliticalBusinessNumber = y.MajorityElection.PoliticalBusinessNumber,
                        SortNumber = y.CountingCircle.SortNumber,
                        CountOfVotersTotal = y.TotalCountOfVoters,
                        VoterParticipation = y.CountOfVoters.VoterParticipation,
                        TotalReceivedBallots = y.CountOfVoters.TotalReceivedBallots,
                        CountOfInvalidBallots = y.CountOfVoters.TotalInvalidBallots,
                        CountOfBlankBallots = y.CountOfVoters.TotalBlankBallots,
                        CountOfAccountedBallots = y.CountOfVoters.TotalAccountedBallots,
                        InvalidVoteCount = y.InvalidVoteCount,
                        EmptyVoteCount = y.EmptyVoteCount,
                        SubmissionDoneTimestamp = y.SubmissionDoneTimestamp,
                        AuditedTentativelyTimestamp = y.AuditedTentativelyTimestamp,
                        ElectionId = y.MajorityElectionId,
                        ElectionUnionIds = y.MajorityElection.MajorityElectionUnionEntries
                            .Select(z => z.MajorityElectionUnionId)
                            .OrderBy(z => z)
                            .ToList(),
                    })
                    .ToList(),
                SecondaryResults = x.SecondaryMajorityElections
                    .OrderBy(y => y.PoliticalBusinessNumber)
                    .Take(WabstiCConstants.MaxSecondaryMajorityElectionsSupported)
                    .SelectMany(y => y.Results)
                    .Select(y => new
                    {
                        y.PrimaryResult.MajorityElectionId,
                        y.PrimaryResult.CountingCircleId,
                        y.InvalidVoteCount,
                        y.EmptyVoteCount,
                    })
                    .ToList(),
            })
            .ToListAsync(ct);

        var groupedSecondaryResults = results.SelectMany(x => x.SecondaryResults)
            .GroupBy(x => (x.CountingCircleId, x.MajorityElectionId))
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => (y.InvalidVoteCount, y.EmptyVoteCount)).ToList());

        var majorityElectionResults = results
            .SelectMany(x => x.PrimaryResults)
            .ToList();

        await _contestDetailsAttacher.AttachContestDetails(ctx.ContestId, majorityElectionResults, ct);
        AttachSecondaryResults(majorityElectionResults, groupedSecondaryResults);

        return _templateService.RenderToCsv(
            ctx,
            majorityElectionResults);
    }

    /// <summary>
    /// Attaches secondary election results (only the first 2, since only 2 are supported by wabstiC) to the respective primary result.
    /// </summary>
    /// <param name="primaryResults">The primary results.</param>
    /// <param name="secondaryResults">The secondary results.</param>
    private void AttachSecondaryResults(
        IEnumerable<Data> primaryResults,
        IReadOnlyDictionary<(Guid CountingCircleId, Guid PrimaryElectionId), List<(int InvalidVoteCount, int EmptyVoteCount)>> secondaryResults)
    {
        foreach (var result in primaryResults)
        {
            if (!secondaryResults.TryGetValue((result.CountingCircleId, result.ElectionId), out var secondaryResult))
            {
                continue;
            }

            (result.InvalidVoteCountSecondary, result.EmptyVoteCountSecondary) = secondaryResult.ElementAtOrDefault(0);
            (result.InvalidVoteCountSecondary2, result.EmptyVoteCountSecondary2) = secondaryResult.ElementAtOrDefault(1);
        }
    }

    private class Data : IWabstiCContestDetails
    {
        [Ignore]
        public Guid CountingCircleId { get; set; }

        [Name("Art")]
        [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
        public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

        [Name("BfsNrGemeinde")]
        public string CountingCircleBfs { get; set; } = string.Empty;

        [Name("EinheitCode")]
        public string CountingCircleCode { get; set; } = string.Empty;

        [Name("SortGeschaeft")]
        public string PoliticalBusinessNumber { get; set; } = string.Empty;

        [Name("SortGemeinde")]
        public int SortNumber { get; set; }

        [Name("Stimmberechtigte")]
        public int CountOfVotersTotal { get; set; }

        [Name("StimmberechtigteAusl")]
        public int CountOfVotersTotalSwissAbroad { get; set; }

        [Name("StiAusweiseUrne")]
        public int VotingCardsBallotBox { get; set; }

        [Name("StiAusweiseVorzeitig")]
        public int VotingCardsPaper { get; set; }

        [Name("StiAusweiseBriefGueltig")]
        public int VotingCardsByMail { get; set; }

        [Name("StiAusweiseBriefNiUz")]
        public int VotingCardsByMailNotValid { get; set; }

        [Name("StiAusweiseEVoting")]
        public int VotingCardsEVoting { get; set; }

        [Name("Stimmbeteiligung")]
        [TypeConverter(typeof(WabstiCPercentageConverter))]
        public decimal VoterParticipation { get; set; }

        [Name("StmAbgegeben")]
        public int TotalReceivedBallots { get; set; }

        [Name("StmUngueltig")]
        public int CountOfInvalidBallots { get; set; }

        [Name("StmLeer")]
        public int CountOfBlankBallots { get; set; }

        [Name("StmGueltig")]
        public int CountOfAccountedBallots { get; set; }

        [Name("StimmenUngueltig")]
        public int InvalidVoteCount { get; set; }

        [Name("StimmenUngueltigNW")]
        public int InvalidVoteCountSecondary { get; set; }

        [Name("StimmenLeerNW")]
        public int EmptyVoteCountSecondary { get; set; }

        [Name("StimmenUngueltigNW2")]
        public int InvalidVoteCountSecondary2 { get; set; }

        [Name("StimmenLeerNW2")]
        public int EmptyVoteCountSecondary2 { get; set; }

        [Name("StimmenLeer")]
        public int EmptyVoteCount { get; set; }

        [Name("FreigabeGde")]
        [TypeConverter(typeof(WabstiCTimeConverter))]
        public DateTime? SubmissionDoneTimestamp { get; set; }

        [Name("Sperrung")]
        [TypeConverter(typeof(WabstiCTimeConverter))]
        public DateTime? AuditedTentativelyTimestamp { get; set; }

        [Name("GeLfNr")]
        public Guid ElectionId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }
    }
}
