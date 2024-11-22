// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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
public class WabstiCSGStaticGemeindenRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Vote> _repo;
    private readonly WabstiCContestDetailsAttacher _contestDetailsAttacher;

    public WabstiCSGStaticGemeindenRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, Vote> repo,
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
            .SelectMany(x => x.Results)
            .OrderBy(x => x.Ballot.Vote.PoliticalBusinessNumber)
            .ThenBy(x => x.VoteResult.CountingCircle.Code)
            .ThenBy(x => x.VoteResult.CountingCircle.Name)
            .ThenBy(x => x.Ballot.Position)
            .Select(x => new Data
            {
                VoteId = x.VoteResult.Vote.Type == VoteType.QuestionsOnSingleBallot ? x.VoteResult.VoteId : x.BallotId,
                DomainOfInfluenceName = x.VoteResult.Vote.DomainOfInfluence.Name,
                DomainOfInfluenceType = x.VoteResult.Vote.DomainOfInfluence.Type,
                DomainOfInfluenceSortNumber = x.VoteResult.Vote.DomainOfInfluence.SortNumber,
                PoliticalBusinessNumber = x.VoteResult.Vote.PoliticalBusinessNumber,
                CountingCircleBfs = x.VoteResult.CountingCircle.Bfs,
                CountingCircleCode = x.VoteResult.CountingCircle.Code,
                CountingCircleName = x.VoteResult.CountingCircle.Name,
                CountingCircleId = x.VoteResult.CountingCircle.Id,
                CountOfVotersTotal = x.VoteResult.TotalCountOfVoters,
                VoteType = x.VoteResult.Vote.Type,
                Position = x.Ballot.Position,
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

        [Name("Wahlkreis")]
        public string DomainOfInfluenceName { get; set; } = string.Empty;

        [Name("SortWahlkreis")]
        public int DomainOfInfluenceSortNumber { get; set; }

        [Name("BfsNrGemeinde")]
        public string CountingCircleBfs { get; set; } = string.Empty;

        [Name("EinheitCode")]
        public string CountingCircleCode { get; set; } = string.Empty;

        [Name("SortGeschaeft")]
        public string PoliticalBusinessNumber { get; set; } = string.Empty;

        [Name("Gemeinde")]
        public string CountingCircleName { get; set; } = string.Empty;

        [Name("Stimmberechtigte")]
        public int CountOfVotersTotal { get; set; }

        [Name("StimmberechtigteAusl")]
        public int CountOfVotersTotalSwissAbroad { get; set; }

        [Name("GeLfNr")]
        public Guid VoteId { get; set; }

        [Name("GeSubNr")]
        public string BallotSubType => WabstiCPositionUtil.BuildPosition(Position, VoteType);

        [Ignore]
        public VoteType VoteType { get; set; }

        [Ignore]
        public int Position { get; set; }
    }
}
