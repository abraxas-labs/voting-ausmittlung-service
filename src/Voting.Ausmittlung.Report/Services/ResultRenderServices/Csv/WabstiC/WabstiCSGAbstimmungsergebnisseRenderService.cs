// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

public class WabstiCSGAbstimmungsergebnisseRenderService : IRendererService
{
    private static readonly HashSet<CountingCircleResultState> StatesToCleanResult = new()
    {
        CountingCircleResultState.Initial,
        CountingCircleResultState.SubmissionOngoing,
        CountingCircleResultState.ReadyForCorrection,
    };

    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, Vote> _repo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;

    public WabstiCSGAbstimmungsergebnisseRenderService(TemplateService templateService, IDbRepository<DataContext, Vote> repo, IDbRepository<DataContext, Contest> contestRepo)
    {
        _templateService = templateService;
        _repo = repo;
        _contestRepo = contestRepo;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var contest = await _contestRepo.GetByKey(ctx.ContestId)
            ?? throw new EntityNotFoundException(nameof(Contest), ctx.ContestId);

        var votes = await _repo.Query()
            .AsSingleQuery()
            .Where(x => ctx.PoliticalBusinessIds.Contains(x.Id))
            .Include(x => x.Contest)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Translations)
            .Include(x => x.EndResult!)
            .OrderBy(x => x.PoliticalBusinessNumber)
            .ToListAsync(ct);

        var data = new List<Data>();

        foreach (var vote in votes)
        {
            var countOfVotersByCountingCircleId = await _repo.Query()
                .SelectMany(x => x.Contest.CountingCircleDetails)
                .Where(x => x.ContestId == vote.ContestId)
                .GroupBy(x => x.CountingCircle.BasisCountingCircleId)
                .Select(x => new ContestDetails
                {
                    CountingCircleId = x.Key,
                    VotingCardsBallotBox = x.Single().VotingCards
                        .Where(z => z.DomainOfInfluenceType == vote.DomainOfInfluence.Type && z.Valid && z.Channel == VotingChannel.BallotBox)
                        .Sum(z => z.CountOfReceivedVotingCards ?? 0),
                    VotingCardsPaper = x.Single().VotingCards
                        .Where(z => z.DomainOfInfluenceType == vote.DomainOfInfluence.Type && z.Valid && z.Channel == VotingChannel.Paper)
                        .Sum(z => z.CountOfReceivedVotingCards ?? 0),
                    VotingCardsByMail = x.Single().VotingCards
                        .Where(z => z.DomainOfInfluenceType == vote.DomainOfInfluence.Type && z.Valid && z.Channel == VotingChannel.ByMail)
                        .Sum(z => z.CountOfReceivedVotingCards ?? 0),
                    VotingCardsByMailNotValid = x.Single().VotingCards
                        .Where(z => z.DomainOfInfluenceType == vote.DomainOfInfluence.Type && !z.Valid && z.Channel == VotingChannel.ByMail)
                        .Sum(z => z.CountOfReceivedVotingCards ?? 0),
                    VotingCardsEVoting = x.Single().VotingCards
                        .Where(z => z.DomainOfInfluenceType == vote.DomainOfInfluence.Type && z.Valid && z.Channel == VotingChannel.EVoting)
                        .Sum(z => z.CountOfReceivedVotingCards ?? 0),
                    TotalCountOfVoters = x.Single().TotalCountOfVoters,
                    SwissAbroadCountOfVoters = x.Single().CountOfVotersInformationSubTotals
                        .Where(y => y.VoterType == VoterType.SwissAbroad)
                        .Sum(y => y.CountOfVoters.GetValueOrDefault()),
                    SwissMalesCountOfVoters = x.Single().CountOfVotersInformationSubTotals
                        .Where(y => y.VoterType == VoterType.Swiss && y.Sex == SexType.Male)
                        .Sum(y => y.CountOfVoters.GetValueOrDefault()),
                    SwissFemalesCountOfVoters = x.Single().CountOfVotersInformationSubTotals
                        .Where(y => y.VoterType == VoterType.Swiss && y.Sex == SexType.Female)
                        .Sum(y => y.CountOfVoters.GetValueOrDefault()),
                    SwissAbroadMalesCountOfVoters = x.Single().CountOfVotersInformationSubTotals
                        .Where(y => y.VoterType == VoterType.SwissAbroad && y.Sex == SexType.Male)
                        .Sum(y => y.CountOfVoters.GetValueOrDefault()),
                    SwissAbroadFemalesCountOfVoters = x.Single().CountOfVotersInformationSubTotals
                        .Where(y => y.VoterType == VoterType.SwissAbroad && y.Sex == SexType.Female)
                        .Sum(y => y.CountOfVoters.GetValueOrDefault()),
                })
                .ToDictionaryAsync(x => x.CountingCircleId, x => x, ct);

            var resultStatesByCountingCircleId = await _repo.Query()
                .Where(x => x.Id == vote.Id)
                .SelectMany(x => x.Results)
                .Include(x => x.CountingCircle)
                .ToDictionaryAsync(x => x.CountingCircle.BasisCountingCircleId, x => x.State, ct);

            // Currently the export is only used for vote results with one ballot result.
            var ballotResultByCountingCircleId = await _repo.Query()
                .AsSplitQuery()
                .Where(x => x.Id == vote.Id)
                .SelectMany(x => x.Results)
                .Include(x => x.CountingCircle)
                .Include(x => x.Results).ThenInclude(x => x.QuestionResults).ThenInclude(y => y.Question)
                .Include(x => x.Results).ThenInclude(x => x.TieBreakQuestionResults).ThenInclude(y => y.Question)
                .ToDictionaryAsync(x => x.CountingCircle.BasisCountingCircleId, x => x.Results.FirstOrDefault(), ct);

            var dataByVote = await _repo.Query()
                .Where(x => x.Id == vote.Id)
                .SelectMany(x => x.Results)
                .Select(x => new Data
                {
                    CountingCircleCode = x.CountingCircle.Code,
                    CountingCircleSortNumber = x.CountingCircle.SortNumber,
                    CountingCircleBfs = x.CountingCircle.Bfs,
                    CountingCircleName = x.CountingCircle.Name,
                    CountingCircleId = x.CountingCircle.BasisCountingCircleId,
                })
                .OrderBy(x => x.CountingCircleSortNumber)
                .AsAsyncEnumerable()
                .Select(x =>
                {
                    AttachPoliticalBusinessData(vote, x);
                    AttachContestDetails(countOfVotersByCountingCircleId, x);
                    AttachBallotResultData(ballotResultByCountingCircleId, x);
                    CleanResultValues(resultStatesByCountingCircleId, x);
                    return x;
                })
                .ToListAsync(ct);

            data.AddRange(dataByVote);
        }

        return _templateService.RenderToDynamicCsv(
            ctx,
            data.ToAsyncEnumerable(),
            WabstiCDateUtil.BuildDateForFilename(contest.Date));
    }

    private int GetQuestionResultValueByNumber(IReadOnlyDictionary<int, BallotQuestionResult> questionResultByNumber, int number, Func<BallotQuestionResult, int> propertySelector)
    {
        questionResultByNumber.TryGetValue(number, out var ballotQuestionResult);
        return ballotQuestionResult == null ? 0 : propertySelector(ballotQuestionResult);
    }

    private int GetTieBreakQuestionResultValueByNumber(IReadOnlyDictionary<int, TieBreakQuestionResult> tieBreakQuestionResultByNumber, int number, Func<TieBreakQuestionResult, int> propertySelector)
    {
        tieBreakQuestionResultByNumber.TryGetValue(number, out var tieBreakQuestionResult);
        return tieBreakQuestionResult == null ? 0 : propertySelector(tieBreakQuestionResult);
    }

    private void CleanResultValues(IReadOnlyDictionary<Guid, CountingCircleResultState> statesByCcId, Data data)
    {
        var state = statesByCcId.GetValueOrDefault(data.CountingCircleId);
        if (!StatesToCleanResult.Contains(state))
        {
            return;
        }

        data.VoterParticipation = null;
        data.AccountedBallots = null;
        data.EmptyBallots = null;
        data.InvalidBallots = null;
        data.ReceivedBallots = null;
        data.Question1CountYes = null;
        data.Question1CountNo = null;
        data.Question1CountUnspecified = null;
        data.Question1CountTotal = null;
        data.Question2CountYes = null;
        data.Question2CountNo = null;
        data.Question2CountUnspecified = null;
        data.Question2CountTotal = null;
        data.TieBreakCountQ1 = null;
        data.TieBreakCountQ2 = null;
        data.TieBreakCountUnspecified = null;
        data.TieBreakCountTotal = null;
    }

    private void AttachContestDetails(IReadOnlyDictionary<Guid, ContestDetails> detailsByCcId, Data data)
    {
        var details = detailsByCcId.GetValueOrDefault(data.CountingCircleId);
        data.CountOfVoters = details?.TotalCountOfVoters ?? 0;
        data.CountOfVotersSwissAbroad = details?.SwissAbroadCountOfVoters ?? 0;
        data.CountOfVotersSwissMales = details?.SwissMalesCountOfVoters ?? 0;
        data.CountOfVotersSwissFemales = details?.SwissFemalesCountOfVoters ?? 0;
        data.CountOfVotersSwissAbroadMales = details?.SwissAbroadMalesCountOfVoters ?? 0;
        data.CountOfVotersSwissAbroadFemales = details?.SwissAbroadFemalesCountOfVoters ?? 0;
        data.VotingCardsBallotBox = details?.VotingCardsBallotBox ?? 0;
        data.VotingCardsPaper = details?.VotingCardsPaper ?? 0;
        data.VotingCardsByMail = details?.VotingCardsByMail ?? 0;
        data.VotingCardsByMailNotValid = details?.VotingCardsByMailNotValid ?? 0;
        data.VotingCardsEVoting = details?.VotingCardsEVoting ?? 0;
    }

    private void AttachPoliticalBusinessData(Vote vote, Data row)
    {
        row.ContestDate = vote.Contest.Date;
        row.PoliticalBusinessNumber = vote.PoliticalBusinessNumber;
        row.DomainOfInfluenceType = vote.DomainOfInfluence.Type.ToString().ToUpperInvariant();
        row.DomainOfInfluenceTypeNumber = GetDomainOfInfluenceTypeNumber(vote.DomainOfInfluence.Type);
    }

    private void AttachBallotResultData(IReadOnlyDictionary<Guid, BallotResult?> ballotResultByCcId, Data data)
    {
        var ballotResult = ballotResultByCcId.GetValueOrDefault(data.CountingCircleId);
        if (ballotResult == null)
        {
            return;
        }

        data.VoterParticipation = ballotResult.CountOfVoters.VoterParticipation;
        data.AccountedBallots = ballotResult.CountOfVoters.TotalAccountedBallots;
        data.EmptyBallots = ballotResult.CountOfVoters.TotalBlankBallots;
        data.InvalidBallots = ballotResult.CountOfVoters.TotalInvalidBallots;
        data.ReceivedBallots = ballotResult.CountOfVoters.TotalReceivedBallots;

        var questionResultByNumber = ballotResult.QuestionResults.ToDictionary(x => x.Question.Number);
        var tieBreakQuestionResultByNumber = ballotResult.TieBreakQuestionResults.ToDictionary(x => x.Question.Number);

        data.Question1CountYes = GetQuestionResultValueByNumber(questionResultByNumber, 1, y => y.TotalCountOfAnswerYes);
        data.Question1CountNo = GetQuestionResultValueByNumber(questionResultByNumber, 1, y => y.TotalCountOfAnswerNo);
        data.Question1CountUnspecified = GetQuestionResultValueByNumber(questionResultByNumber, 1, y => y.TotalCountOfAnswerUnspecified);
        data.Question1CountTotal = GetQuestionResultValueByNumber(questionResultByNumber, 1, y => y.CountOfAnswerTotal);
        data.Question2CountYes = GetQuestionResultValueByNumber(questionResultByNumber, 2, y => y.TotalCountOfAnswerYes);
        data.Question2CountNo = GetQuestionResultValueByNumber(questionResultByNumber, 2, y => y.TotalCountOfAnswerNo);
        data.Question2CountUnspecified = GetQuestionResultValueByNumber(questionResultByNumber, 2, y => y.TotalCountOfAnswerUnspecified);
        data.Question2CountTotal = GetQuestionResultValueByNumber(questionResultByNumber, 2, y => y.CountOfAnswerTotal);
        data.TieBreakCountQ1 = GetTieBreakQuestionResultValueByNumber(tieBreakQuestionResultByNumber, 1, y => y.TotalCountOfAnswerQ1);
        data.TieBreakCountQ2 = GetTieBreakQuestionResultValueByNumber(tieBreakQuestionResultByNumber, 1, y => y.TotalCountOfAnswerQ2);
        data.TieBreakCountUnspecified = GetTieBreakQuestionResultValueByNumber(tieBreakQuestionResultByNumber, 1, y => y.TotalCountOfAnswerUnspecified);
        data.TieBreakCountTotal = GetTieBreakQuestionResultValueByNumber(tieBreakQuestionResultByNumber, 1, y => y.CountOfAnswerTotal);
    }

    private int GetDomainOfInfluenceTypeNumber(DomainOfInfluenceType type) => type switch
    {
        DomainOfInfluenceType.Ch => 1,
        DomainOfInfluenceType.Ct or DomainOfInfluenceType.Bz => 2,
        _ => 3,
    };

    private class ContestDetails
    {
        public Guid CountingCircleId { get; set; }

        public int TotalCountOfVoters { get; set; }

        public int SwissAbroadCountOfVoters { get; set; }

        public int SwissMalesCountOfVoters { get; set; }

        public int SwissFemalesCountOfVoters { get; set; }

        public int SwissAbroadMalesCountOfVoters { get; set; }

        public int SwissAbroadFemalesCountOfVoters { get; set; }

        public int VotingCardsBallotBox { get; set; }

        public int VotingCardsPaper { get; set; }

        public int VotingCardsByMail { get; set; }

        public int VotingCardsByMailNotValid { get; set; }

        public int VotingCardsEVoting { get; set; }
    }

    private class Data
    {
        [Name("Datum")]
        [TypeConverter(typeof(WabstiCDateConverter))]
        public DateTime ContestDate { get; set; }

        [Name("Vorlage-Nr.")]
        public string? PoliticalBusinessNumber { get; set; }

        [Name("GeschäftsEbene")]
        public string? DomainOfInfluenceType { get; set; }

        [Name("Gemeinde")]
        public string? CountingCircleName { get; set; }

        [Name("SortPolitisch")]
        public int CountingCircleSortNumber { get; set; }

        [Name("BfS-Nr.")]
        public string? CountingCircleBfs { get; set; }

        [Name("Stimmberechtigte")]
        public int? CountOfVoters { get; set; }

        [Name("davon Auslandschweizer")]
        public int? CountOfVotersSwissAbroad { get; set; }

        [Name("eingelegte SZ")]
        public int? ReceivedBallots { get; set; }

        [Name("leere SZ")]
        public int? EmptyBallots { get; set; }

        [Name("ungültige SZ")]
        public int? InvalidBallots { get; set; }

        [Name("gültige SZ")]
        public int? AccountedBallots { get; set; }

        [Name("Ja")]
        public int? Question1CountYes { get; set; }

        [Name("Nein")]
        public int? Question1CountNo { get; set; }

        [Name("InitOAntw")]
        public int? Question1CountUnspecified { get; set; }

        [Name("InitTotal")]
        public int? Question1CountTotal { get; set; }

        [Name("GegenvJa")]
        public int? Question2CountYes { get; set; }

        [Name("GegenvNein")]
        public int? Question2CountNo { get; set; }

        [Name("GegenvOAntw")]
        public int? Question2CountUnspecified { get; set; }

        [Name("GegenvTotal")]
        public int? Question2CountTotal { get; set; }

        [Name("StichfrJa")]
        public int? TieBreakCountQ1 { get; set; }

        [Name("StichfrNein")]
        public int? TieBreakCountQ2 { get; set; }

        [Name("StichfrOAntw")]
        public int? TieBreakCountUnspecified { get; set; }

        [Name("StichfrTotal")]
        public int? TieBreakCountTotal { get; set; }

        [Name("StimmBet")]
        [TypeConverter(typeof(WabstiCPercentDecimalConverter))]
        public decimal? VoterParticipation { get; set; }

        [Name("UmschlaegeOhneAusweis")]
        public int UmschlaegeOhneAusweis => 0;

        [Name("StimmEingelegtUngueltig")]
        public int StimmEingelegtUngueltig => 0;

        [Name("StimmEingelegtGueltig")]
        public int StimmEingelegtGueltig => 0;

        [Name("GeSubNr")]
        public int? GeSubNr => null;

        [Name("GeEbene")]
        public int DomainOfInfluenceTypeNumber { get; set; }

        [Name("StimmberInlandschweizerM")]
        public int? CountOfVotersSwissMales { get; set; }

        [Name("StimmberInlandschweizerW")]
        public int? CountOfVotersSwissFemales { get; set; }

        [Name("StimmberAuslandschweizerM")]
        public int? CountOfVotersSwissAbroadMales { get; set; }

        [Name("StimmberAuslandschweizerW")]
        public int? CountOfVotersSwissAbroadFemales { get; set; }

        [Name("StimmausweiseUrne")]
        public int? VotingCardsBallotBox { get; set; }

        [Name("StimmausweiseVorzeitig")]
        public int VotingCardsPaper { get; set; }

        [Name("StimmausweiseBrieflich")]
        public int VotingCardsByMail { get; set; }

        [Name("StimmausweiseBrieflichUngueltig")]
        public int VotingCardsByMailNotValid { get; set; }

        [Name("StimmausweiseEVoting")]
        public int VotingCardsEVoting { get; set; }

        [Name("CodeZEinheiten")]
        public string? CountingCircleCode { get; set; }

        [Ignore]
        public Guid CountingCircleId { get; set; }
    }
}
