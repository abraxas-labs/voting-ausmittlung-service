// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCSGGemeindenRenderService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Vote> _repo;
    private readonly WabstiCContestDetailsAttacher _contestDetailsAttacher;

    public WabstiCSGGemeindenRenderService(
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
            .AsSplitQuery()
            .Where(x => x.ContestId == ctx.ContestId && ctx.PoliticalBusinessIds.Contains(x.Id))
            .SelectMany(x => x.Results)
            .SelectMany(r => r.Results)
            .OrderBy(x => x.Ballot.Vote.PoliticalBusinessNumber)
            .ThenBy(x => x.VoteResult.CountingCircle.Code)
            .ThenBy(x => x.VoteResult.CountingCircle.Name)
            .ThenBy(x => x.Ballot.Position)
            .Select(x => new WabstiCVoteResultData
            {
                DomainOfInfluenceType = x.VoteResult.Vote.DomainOfInfluence.Type,
                DomainOfInfluenceSortNumber = x.VoteResult.Vote.DomainOfInfluence.SortNumber,
                PoliticalBusinessId = x.VoteResult.VoteId,
                PoliticalBusinessNumber = x.VoteResult.Vote.PoliticalBusinessNumber,
                VoterParticipation = x.CountOfVoters.VoterParticipation,
                CountingCircleBfs = x.VoteResult.CountingCircle.Bfs,
                CountingCircleCode = x.VoteResult.CountingCircle.Code,
                CountingCircleId = x.VoteResult.CountingCircleId,
                SubmissionDoneTimestamp = x.VoteResult.SubmissionDoneTimestamp,
                TotalReceivedBallots = x.CountOfVoters.TotalReceivedBallots,
                CountOfInvalidBallots = x.CountOfVoters.ConventionalInvalidBallots.GetValueOrDefault(),
                CountOfAccountedBallots = x.CountOfVoters.TotalAccountedBallots,
                CountOfBlankBallots = x.CountOfVoters.ConventionalBlankBallots.GetValueOrDefault(),
                CountOfVotersTotal = x.VoteResult.TotalCountOfVoters,
                QuestionResults = x.QuestionResults.OrderBy(qr => qr.Question.Number).ToList(),
                TieBreakQuestionResults = x.TieBreakQuestionResults.OrderBy(qr => qr.Question.Number).ToList(),
            })
            .ToListAsync(ct);

        await _contestDetailsAttacher.AttachSwissAbroadCountOfVoters(ctx.ContestId, results, ct);

        return _templateService.RenderToCsv(
            ctx,
            results);
    }
}
