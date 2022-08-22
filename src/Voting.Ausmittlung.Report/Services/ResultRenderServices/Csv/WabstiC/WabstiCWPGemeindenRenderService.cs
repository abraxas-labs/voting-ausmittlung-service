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
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWPGemeindenRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, ProportionalElection> _repo;
    private readonly WabstiCContestDetailsAttacher _contestDetailsAttacher;

    public WabstiCWPGemeindenRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElection> repo,
        WabstiCContestDetailsAttacher contestDetailsAttacher)
    {
        _templateService = templateService;
        _repo = repo;
        _contestDetailsAttacher = contestDetailsAttacher;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = await _repo.Query()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.Results)
            .OrderBy(x => x.ProportionalElection.PoliticalBusinessNumber)
            .ThenBy(x => x.CountingCircle.Code)
            .ThenBy(x => x.CountingCircle.Name)
            .Select(x => new Data
            {
                CountingCircleId = x.CountingCircleId,
                PoliticalBusinessId = x.ProportionalElectionId,
                DomainOfInfluenceType = x.ProportionalElection.DomainOfInfluence.Type,
                CountingCircleBfs = x.CountingCircle.Bfs,
                CountingCircleCode = x.CountingCircle.Code,
                PoliticalBusinessNumber = x.ProportionalElection.PoliticalBusinessNumber,
                TotalCountOfVoters = x.TotalCountOfVoters,
                ElectionUnionIds = x.ProportionalElection.ProportionalElectionUnionEntries
                    .Select(y => y.ProportionalElectionUnionId)
                    .OrderBy(y => y)
                    .ToList(),
                VoterParticipation = x.CountOfVoters.VoterParticipation,
                SubmissionDoneTimestamp = x.SubmissionDoneTimestamp,
                TotalReceivedBallots = x.CountOfVoters.TotalReceivedBallots,
                CountOfAccountedBallots = x.CountOfVoters.TotalAccountedBallots,
                CountOfBlankBallots = x.CountOfVoters.ConventionalBlankBallots.GetValueOrDefault(),
                CountOfInvalidBallots = x.CountOfVoters.ConventionalInvalidBallots.GetValueOrDefault(),
                TotalCountOfModifiedLists = x.TotalCountOfModifiedLists,
                TotalCountOfUnmodifiedLists = x.TotalCountOfUnmodifiedLists,
                TotalCountOfBallots = x.TotalCountOfBallots,
                TotalCountOfListsWithoutParty = x.TotalCountOfListsWithoutParty,
            })
            .ToListAsync(ct);

        await _contestDetailsAttacher.AttachSwissAbroadCountOfVoters(ctx.ContestId, results, ct);

        return _templateService.RenderToCsv(
            ctx,
            results);
    }

    private class Data : IWabstiCSwissAbroadCountOfVoters
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

        [Name("Stimmberechtigte")]
        public int TotalCountOfVoters { get; set; }

        [Name("StimmberechtigteAusl")]
        public int CountOfVotersTotalSwissAbroad { get; set; }

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

        [Name("AnzWZListe")]
        public int TotalCountOfBallots { get; set; }

        [Name("AnzWZAmtlLeer")]
        public int TotalCountOfListsWithoutParty { get; set; }

        [Name("AnzWZUnveraendert")]
        public int TotalCountOfUnmodifiedLists { get; set; }

        [Name("AnzWZVeraendert")]
        public int TotalCountOfModifiedLists { get; set; }

        [Name("FreigabeGde")]
        [TypeConverter(typeof(WabstiCTimeConverter))]
        public DateTime? SubmissionDoneTimestamp { get; set; }

        [Name("GeLfNr")]
        public Guid PoliticalBusinessId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }
    }
}
