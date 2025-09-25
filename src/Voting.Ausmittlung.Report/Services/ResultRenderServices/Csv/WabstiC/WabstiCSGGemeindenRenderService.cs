// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Lib.Database.Repositories;
using IndexAttribute = CsvHelper.Configuration.Attributes.IndexAttribute;

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
            .SelectMany(x => x.Results)
            .OrderBy(x => x.Ballot.Vote.PoliticalBusinessNumber)
            .ThenBy(x => x.VoteResult.CountingCircle.Code)
            .ThenBy(x => x.VoteResult.CountingCircle.Name)
            .ThenBy(x => x.Ballot.Position)
            .Select(x => new Data
            {
                DomainOfInfluenceType = x.VoteResult.Vote.DomainOfInfluence.Type,
                DomainOfInfluenceSortNumber = x.VoteResult.Vote.DomainOfInfluence.SortNumber,
                PoliticalBusinessId = x.VoteResult.Vote.Type == VoteType.QuestionsOnSingleBallot ? x.VoteResult.VoteId : x.BallotId,
                PoliticalBusinessNumber = x.VoteResult.Vote.PoliticalBusinessNumber,
                VoterParticipation = x.CountOfVoters.VoterParticipation,
                CountingCircleBfs = x.VoteResult.CountingCircle.Bfs,
                CountingCircleCode = x.VoteResult.CountingCircle.Code,
                SortNumber = x.VoteResult.CountingCircle.SortNumber,
                CountingCircleId = x.VoteResult.CountingCircleId,
                SubmissionDoneTimestamp = x.VoteResult.SubmissionDoneTimestamp,
                AuditedTentativelyTimestamp = x.VoteResult.AuditedTentativelyTimestamp,
                TotalReceivedBallots = x.CountOfVoters.TotalReceivedBallots,
                CountOfInvalidBallots = x.CountOfVoters.TotalInvalidBallots,
                CountOfAccountedBallots = x.CountOfVoters.TotalAccountedBallots,
                CountOfBlankBallots = x.CountOfVoters.TotalBlankBallots,
                CountOfVotersTotal = x.VoteResult.TotalCountOfVoters,
                QuestionResults = x.QuestionResults.OrderBy(qr => qr.Question.Number).ToList(),
                TieBreakQuestionResults = x.TieBreakQuestionResults.OrderBy(qr => qr.Question.Number).ToList(),
                VoteType = x.VoteResult.Vote.Type,
                Position = x.Ballot.Position,
                ResultState = x.VoteResult.State,
            })
            .ToListAsync(ct);

        await _contestDetailsAttacher.AttachContestDetails(ctx.ContestId, results, ct);
        foreach (var result in results)
        {
            result.ResetDataIfSubmissionNotDone();
        }

        return _templateService.RenderToCsv(
            ctx,
            results);
    }

    private class Data : WabstiCVoteResultData, IWabstiCContestDetails
    {
        private new const int StartIndex = WabstiCVoteResultData.StartIndex - 100;

        [Name("StiAusweiseUrne")]
        [Index(StartIndex)]
        public int? VotingCardsBallotBox { get; set; }

        [Name("StiAusweiseVorzeitig")]
        [Index(StartIndex + 2)]
        public int? VotingCardsPaper { get; set; }

        [Name("StiAusweiseBriefGueltig")]
        [Index(StartIndex + 3)]
        public int? VotingCardsByMail { get; set; }

        [Name("StiAusweiseBriefNiUz")]
        [Index(StartIndex + 4)]
        public int? VotingCardsByMailNotValid { get; set; }

        [Name("StiAusweiseEVoting")]
        [Index(StartIndex + 5)]
        public int? VotingCardsEVoting { get; set; }

        [Name("GeSubNr")]
        [Index(WabstiCPoliticalBusinessData.EndIndex + 1)]
        public string BallotSubType => WabstiCPositionUtil.BuildPosition(Position, VoteType);

        [Ignore]
        public VoteType VoteType { get; set; }

        [Ignore]
        public int Position { get; set; }

        public override void ResetDataIfSubmissionNotDone()
        {
            if (ResultState.IsSubmissionDone())
            {
                return;
            }

            base.ResetDataIfSubmissionNotDone();
            VotingCardsBallotBox = null;
            VotingCardsPaper = null;
            VotingCardsByMail = null;
            VotingCardsByMailNotValid = null;
            VotingCardsEVoting = null;
        }
    }
}
