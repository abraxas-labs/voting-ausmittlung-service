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
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv;

public class CsvVoteEVotingDetailsResultService : IRendererService
{
    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, VoteResult> _voteResultRepo;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _ccDetailsRepo;

    public CsvVoteEVotingDetailsResultService(
        TemplateService templateService,
        IDbRepository<DataContext, VoteResult> voteResultRepo,
        IDbRepository<DataContext, ContestCountingCircleDetails> ccDetailsRepo)
    {
        _templateService = templateService;
        _voteResultRepo = voteResultRepo;
        _ccDetailsRepo = ccDetailsRepo;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var ccDetailsByCountingCircleId = await _ccDetailsRepo.Query()
            .AsSplitQuery()
            .Include(x => x.VotingCards)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .Where(x => x.ContestId == ctx.ContestId && x.EVoting)
            .ToDictionaryAsync(x => x.CountingCircleId, ct);
        var countingCircleIds = ccDetailsByCountingCircleId.Keys.ToList();

        var results = await _voteResultRepo.Query()
            .AsSplitQuery()
            .Where(x => x.Vote.ContestId == ctx.ContestId
                && ctx.PoliticalBusinessIds.Contains(x.VoteId)
                && countingCircleIds.Contains(x.CountingCircleId))
            .SelectMany(x => x.Results)
            .OrderBy(x => x.Ballot.Vote.PoliticalBusinessNumber)
            .ThenBy(x => x.VoteResult.CountingCircle.Code)
            .ThenBy(x => x.VoteResult.CountingCircle.Name)
            .ThenBy(x => x.Ballot.Position)
            .Select(x => new Data
            {
                DomainOfInfluenceType = x.VoteResult.Vote.DomainOfInfluence.Type,
                DomainOfInfluenceSortNumber = x.VoteResult.Vote.DomainOfInfluence.SortNumber,
                VoteTranslations = x.VoteResult.Vote.Translations,
                CountingCircleId = x.VoteResult.CountingCircle.Id,
                CountingCircleBfs = x.VoteResult.CountingCircle.Bfs,
                CountingCircleCode = x.VoteResult.CountingCircle.Code,
                SortNumber = x.VoteResult.CountingCircle.SortNumber,
                VoterParticipation = x.CountOfVoters.VoterParticipation,
                TotalReceivedBallots = x.CountOfVoters.TotalReceivedBallots,
                ConventionalAndECountingReceivedBallots = x.CountOfVoters.ConventionalSubTotal.ReceivedBallots ?? 0
                    + x.CountOfVoters.ECountingSubTotal.ReceivedBallots,
                EVotingReceivedBallots = x.CountOfVoters.EVotingSubTotal.ReceivedBallots,
                TotalInvalidBallots = x.CountOfVoters.TotalInvalidBallots,
                ConventionalAndECountingInvalidBallots = x.CountOfVoters.ConventionalSubTotal.InvalidBallots ?? 0
                    + x.CountOfVoters.ECountingSubTotal.InvalidBallots,
                EVotingInvalidBallots = x.CountOfVoters.EVotingSubTotal.InvalidBallots,
                TotalAccountedBallots = x.CountOfVoters.TotalAccountedBallots,
                ConventionalAndECountingAccountedBallots = x.CountOfVoters.ConventionalSubTotal.AccountedBallots ?? 0
                    + x.CountOfVoters.ECountingSubTotal.AccountedBallots,
                EVotingAccountedBallots = x.CountOfVoters.EVotingSubTotal.AccountedBallots,
                TotalBlankBallots = x.CountOfVoters.TotalBlankBallots,
                ConventionalAndECountingBlankBallots = x.CountOfVoters.ConventionalSubTotal.BlankBallots ?? 0
                    + x.CountOfVoters.ECountingSubTotal.BlankBallots,
                EVotingBlankBallots = x.CountOfVoters.EVotingSubTotal.BlankBallots,
                QuestionResults = x.QuestionResults.OrderBy(qr => qr.Question.Number).ToList(),
                TieBreakQuestionResults = x.TieBreakQuestionResults.OrderBy(qr => qr.Question.Number).ToList(),
                PoliticalBusinessId = x.VoteResult.VoteId,
                SubmissionDoneTimestamp = x.VoteResult.SubmissionDoneTimestamp,
                TotalSentEVotingVotingCards = x.VoteResult.TotalSentEVotingVotingCards,
                ResultState = x.VoteResult.State,
            })
            .ToListAsync(ct);

        foreach (var result in results)
        {
            if (result.ResultState.IsSubmissionDone())
            {
                AttachContestDetails(result, ccDetailsByCountingCircleId);
            }
            else
            {
                ResetData(result);
            }
        }

        return _templateService.RenderToCsv(
            ctx,
            results);
    }

    private void ResetData(Data record)
    {
        record.CountOfVotersMen = 0;
        record.CountOfVotersWomen = 0;
        record.TotalCountOfVoters = 0;
        record.VotingCardsBallotBox = 0;
        record.VotingCardsPaper = 0;
        record.VotingCardsByMail = 0;
        record.VotingCardsByMailNotValid = 0;
        record.VotingCardsEVoting = 0;
        record.VoterParticipation = 0;
        record.TotalReceivedBallots = 0;
        record.ConventionalAndECountingReceivedBallots = 0;
        record.EVotingReceivedBallots = 0;
        record.TotalInvalidBallots = 0;
        record.ConventionalAndECountingInvalidBallots = 0;
        record.EVotingInvalidBallots = 0;
        record.TotalBlankBallots = 0;
        record.ConventionalAndECountingBlankBallots = 0;
        record.EVotingBlankBallots = 0;
        record.TotalAccountedBallots = 0;
        record.ConventionalAndECountingAccountedBallots = 0;
        record.EVotingAccountedBallots = 0;
        record.TotalCountOfAnswerYesQ1 = 0;
        record.ConventionalAndECountingCountOfAnswerYesQ1 = 0;
        record.EVotingCountOfAnswerYesQ1 = 0;
        record.TotalCountOfAnswerNoQ1 = 0;
        record.ConventionalAndECountingCountOfAnswerNoQ1 = 0;
        record.EVotingCountOfAnswerNoQ1 = 0;
        record.TotalCountOfAnswerUnspecifiedQ1 = 0;
        record.ConventionalAndECountingCountOfAnswerUnspecifiedQ1 = 0;
        record.EVotingCountOfAnswerUnspecifiedQ1 = 0;
        record.TotalCountOfAnswerYesQ2 = null;
        record.ConventionalAndECountingCountOfAnswerYesQ2 = null;
        record.EVotingCountOfAnswerYesQ2 = null;
        record.TotalCountOfAnswerNoQ2 = null;
        record.ConventionalAndECountingCountOfAnswerNoQ2 = null;
        record.EVotingCountOfAnswerNoQ2 = null;
        record.TotalCountOfAnswerUnspecifiedQ2 = null;
        record.ConventionalAndECountingCountOfAnswerUnspecifiedQ2 = null;
        record.EVotingCountOfAnswerUnspecifiedQ2 = null;
        record.TotalCountOfAnswerYesQ3 = null;
        record.ConventionalAndECountingCountOfAnswerYesQ3 = null;
        record.EVotingCountOfAnswerYesQ3 = null;
        record.TotalCountOfAnswerNoQ3 = null;
        record.ConventionalAndECountingCountOfAnswerNoQ3 = null;
        record.EVotingCountOfAnswerNoQ3 = null;
        record.TotalCountOfAnswerUnspecifiedQ3 = null;
        record.ConventionalAndECountingCountOfAnswerUnspecifiedQ3 = null;
        record.EVotingCountOfAnswerUnspecifiedQ3 = null;
        record.TotalCountOfAnswerYesTBQ1 = null;
        record.ConventionalAndECountingCountOfAnswerYesTBQ1 = null;
        record.EVotingCountOfAnswerYesTBQ1 = null;
        record.TotalCountOfAnswerNoTBQ1 = null;
        record.ConventionalAndECountingCountOfAnswerNoTBQ1 = null;
        record.EVotingCountOfAnswerNoTBQ1 = null;
        record.TotalCountOfAnswerUnspecifiedTBQ1 = null;
        record.ConventionalAndECountingCountOfAnswerUnspecifiedTBQ1 = null;
        record.EVotingCountOfAnswerUnspecifiedTBQ1 = null;
        record.TotalCountOfAnswerYesTBQ2 = null;
        record.ConventionalAndECountingCountOfAnswerYesTBQ2 = null;
        record.EVotingCountOfAnswerYesTBQ2 = null;
        record.TotalCountOfAnswerNoTBQ2 = null;
        record.ConventionalAndECountingCountOfAnswerNoTBQ2 = null;
        record.EVotingCountOfAnswerNoTBQ2 = null;
        record.TotalCountOfAnswerUnspecifiedTBQ2 = null;
        record.ConventionalAndECountingCountOfAnswerUnspecifiedTBQ2 = null;
        record.EVotingCountOfAnswerUnspecifiedTBQ2 = null;
        record.TotalCountOfAnswerYesTBQ3 = null;
        record.ConventionalAndECountingCountOfAnswerYesTBQ3 = null;
        record.EVotingCountOfAnswerYesTBQ3 = null;
        record.TotalCountOfAnswerNoTBQ3 = null;
        record.ConventionalAndECountingCountOfAnswerNoTBQ3 = null;
        record.EVotingCountOfAnswerNoTBQ3 = null;
        record.TotalCountOfAnswerUnspecifiedTBQ3 = null;
        record.ConventionalAndECountingCountOfAnswerUnspecifiedTBQ3 = null;
        record.EVotingCountOfAnswerUnspecifiedTBQ3 = null;
        record.TotalSentEVotingVotingCards = null;
    }

    private void AttachContestDetails(Data data, Dictionary<Guid, ContestCountingCircleDetails> ccDetailsByCountingCircleId)
    {
        if (!ccDetailsByCountingCircleId.TryGetValue(data.CountingCircleId, out var contestDetail))
        {
            return;
        }

        data.TotalCountOfVoters = contestDetail.CountOfVotersInformationSubTotals
            .Where(x => x.DomainOfInfluenceType == data.DomainOfInfluenceType)
            .Sum(x => x.CountOfVoters.GetValueOrDefault());
        data.CountOfVotersMen = contestDetail.CountOfVotersInformationSubTotals
            .Where(x => x.DomainOfInfluenceType == data.DomainOfInfluenceType && x.Sex == SexType.Male)
            .Sum(x => x.CountOfVoters.GetValueOrDefault());
        data.CountOfVotersWomen = contestDetail.CountOfVotersInformationSubTotals
            .Where(x => x.DomainOfInfluenceType == data.DomainOfInfluenceType && x.Sex == SexType.Female)
            .Sum(x => x.CountOfVoters.GetValueOrDefault());

        var vcByValidityAndChannel = contestDetail.VotingCards
            .Where(x => x.DomainOfInfluenceType == data.DomainOfInfluenceType)
            .GroupBy(x => (x.Valid, x.Channel))
            .ToDictionary(x => x.Key, x => x.Sum(y => y.CountOfReceivedVotingCards.GetValueOrDefault()));
        data.VotingCardsPaper = vcByValidityAndChannel.GetValueOrDefault((true, VotingChannel.Paper));
        data.VotingCardsBallotBox = vcByValidityAndChannel.GetValueOrDefault((true, VotingChannel.BallotBox));
        data.VotingCardsByMail = vcByValidityAndChannel.GetValueOrDefault((true, VotingChannel.ByMail));
        data.VotingCardsByMailNotValid = vcByValidityAndChannel.GetValueOrDefault((false, VotingChannel.ByMail));
        data.VotingCardsEVoting = vcByValidityAndChannel.GetValueOrDefault((true, VotingChannel.EVoting));
    }

    private class Data
    {
        [Name("Art")]
        [TypeConverter(typeof(WabstiCUpperSnakeCaseConverter))]
        public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

        [Name("SortWahlkreis")]
        public int DomainOfInfluenceSortNumber { get; set; }

        [Ignore]
        public ICollection<VoteTranslation> VoteTranslations { get; set; } = new HashSet<VoteTranslation>();

        [Name("Kurzbezeichnung")]
        public string VoteShortDescription => VoteTranslations.GetTranslated(x => x.ShortDescription);

        [Name("BfsNrGemeinde")]
        public string CountingCircleBfs { get; set; } = string.Empty;

        [Name("EinheitCode")]
        public string CountingCircleCode { get; set; } = string.Empty;

        [Ignore]
        public Guid CountingCircleId { get; set; }

        [Name("SortGemeinde")]
        public int SortNumber { get; set; }

        [Name("StimmberechtigteMaenner")]
        public int CountOfVotersMen { get; set; }

        [Name("StimmberechtigteFrauen")]
        public int CountOfVotersWomen { get; set; }

        [Name("Stimmberechtigte")]
        public int TotalCountOfVoters { get; set; }

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

        [Name("StmAbgegebenTotal")]
        public int TotalReceivedBallots { get; set; }

        [Name("StmAbgegebenKonventionell")]
        public int ConventionalAndECountingReceivedBallots { get; set; }

        [Name("StmAbgegebenEVoting")]
        public int EVotingReceivedBallots { get; set; }

        [Name("StmUngueltigTotal")]
        public int TotalInvalidBallots { get; set; }

        [Name("StmUngueltigKonventionell")]
        public int ConventionalAndECountingInvalidBallots { get; set; }

        [Name("StmUngueltigEVoting")]
        public int EVotingInvalidBallots { get; set; }

        [Name("StmLeerTotal")]
        public int TotalBlankBallots { get; set; }

        [Name("StmLeerKonventionell")]
        public int ConventionalAndECountingBlankBallots { get; set; }

        [Name("StmLeerEVoting")]
        public int EVotingBlankBallots { get; set; }

        [Name("StmGueltigTotal")]
        public int TotalAccountedBallots { get; set; }

        [Name("StmGueltigKonventionell")]
        public int ConventionalAndECountingAccountedBallots { get; set; }

        [Name("StmGueltigEVoting")]
        public int EVotingAccountedBallots { get; set; }

        [Name("StmHGJaTotal")]
        public int TotalCountOfAnswerYesQ1 { get; set; }

        [Name("StmHGJaKonventionell")]
        public int ConventionalAndECountingCountOfAnswerYesQ1 { get; set; }

        [Name("StmHGJaEVoting")]
        public int EVotingCountOfAnswerYesQ1 { get; set; }

        [Name("StmHGNeinTotal")]
        public int TotalCountOfAnswerNoQ1 { get; set; }

        [Name("StmHGNeinKonventionell")]
        public int ConventionalAndECountingCountOfAnswerNoQ1 { get; set; }

        [Name("StmHGNeinEVoting")]
        public int EVotingCountOfAnswerNoQ1 { get; set; }

        [Name("StmHGohneAwTotal")]
        public int TotalCountOfAnswerUnspecifiedQ1 { get; set; }

        [Name("StmHGohneAwKonventionell")]
        public int ConventionalAndECountingCountOfAnswerUnspecifiedQ1 { get; set; }

        [Name("StmHGohneAwEVoting")]
        public int EVotingCountOfAnswerUnspecifiedQ1 { get; set; }

        [Name("StmN1JaTotal")]
        public int? TotalCountOfAnswerYesQ2 { get; set; }

        [Name("StmN1JaKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerYesQ2 { get; set; }

        [Name("StmN1JaEVoting")]
        public int? EVotingCountOfAnswerYesQ2 { get; set; }

        [Name("StmN1NeinTotal")]
        public int? TotalCountOfAnswerNoQ2 { get; set; }

        [Name("StmN1NeinKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerNoQ2 { get; set; }

        [Name("StmN1NeinEVoting")]
        public int? EVotingCountOfAnswerNoQ2 { get; set; }

        [Name("StmN1ohneAwTotal")]
        public int? TotalCountOfAnswerUnspecifiedQ2 { get; set; }

        [Name("StmN1ohneAwKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerUnspecifiedQ2 { get; set; }

        [Name("StmN1ohneAwEVoting")]
        public int? EVotingCountOfAnswerUnspecifiedQ2 { get; set; }

        [Name("StmN11JaTotal")]
        public int? TotalCountOfAnswerYesQ3 { get; set; }

        [Name("StmN11JaKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerYesQ3 { get; set; }

        [Name("StmN11JaEVoting")]
        public int? EVotingCountOfAnswerYesQ3 { get; set; }

        [Name("StmN11NeinTotal")]
        public int? TotalCountOfAnswerNoQ3 { get; set; }

        [Name("StmN11NeinKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerNoQ3 { get; set; }

        [Name("StmN11NeinEVoting")]
        public int? EVotingCountOfAnswerNoQ3 { get; set; }

        [Name("StmN11ohneAwTotal")]
        public int? TotalCountOfAnswerUnspecifiedQ3 { get; set; }

        [Name("StmN11ohneAwKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerUnspecifiedQ3 { get; set; }

        [Name("StmN11ohneAwEVoting")]
        public int? EVotingCountOfAnswerUnspecifiedQ3 { get; set; }

        [Name("StmN2JaTotal")]
        public int? TotalCountOfAnswerYesTBQ1 { get; set; }

        [Name("StmN2JaKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerYesTBQ1 { get; set; }

        [Name("StmN2JaEVoting")]
        public int? EVotingCountOfAnswerYesTBQ1 { get; set; }

        [Name("StmN2NeinTotal")]
        public int? TotalCountOfAnswerNoTBQ1 { get; set; }

        [Name("StmN2NeinKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerNoTBQ1 { get; set; }

        [Name("StmN2NeinEVoting")]
        public int? EVotingCountOfAnswerNoTBQ1 { get; set; }

        [Name("StmN2ohneAwTotal")]
        public int? TotalCountOfAnswerUnspecifiedTBQ1 { get; set; }

        [Name("StmN2ohneAwKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerUnspecifiedTBQ1 { get; set; }

        [Name("StmN2ohneAwEVoting")]
        public int? EVotingCountOfAnswerUnspecifiedTBQ1 { get; set; }

        [Name("StmN21JaTotal")]
        public int? TotalCountOfAnswerYesTBQ2 { get; set; }

        [Name("StmN21JaKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerYesTBQ2 { get; set; }

        [Name("StmN21JaEVoting")]
        public int? EVotingCountOfAnswerYesTBQ2 { get; set; }

        [Name("StmN21NeinTotal")]
        public int? TotalCountOfAnswerNoTBQ2 { get; set; }

        [Name("StmN21NeinKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerNoTBQ2 { get; set; }

        [Name("StmN21NeinEVoting")]
        public int? EVotingCountOfAnswerNoTBQ2 { get; set; }

        [Name("StmN21ohneAwTotal")]
        public int? TotalCountOfAnswerUnspecifiedTBQ2 { get; set; }

        [Name("StmN21ohneAwKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerUnspecifiedTBQ2 { get; set; }

        [Name("StmN21ohneAwEVoting")]
        public int? EVotingCountOfAnswerUnspecifiedTBQ2 { get; set; }

        [Name("StmN22JaTotal")]
        public int? TotalCountOfAnswerYesTBQ3 { get; set; }

        [Name("StmN22JaKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerYesTBQ3 { get; set; }

        [Name("StmN22JaEVoting")]
        public int? EVotingCountOfAnswerYesTBQ3 { get; set; }

        [Name("StmN22NeinTotal")]
        public int? TotalCountOfAnswerNoTBQ3 { get; set; }

        [Name("StmN22NeinKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerNoTBQ3 { get; set; }

        [Name("StmN22NeinEVoting")]
        public int? EVotingCountOfAnswerNoTBQ3 { get; set; }

        [Name("StmN22ohneAwTotal")]
        public int? TotalCountOfAnswerUnspecifiedTBQ3 { get; set; }

        [Name("StmN22ohneAwKonventionell")]
        public int? ConventionalAndECountingCountOfAnswerUnspecifiedTBQ3 { get; set; }

        [Name("StmN22ohneAwEVoting")]
        public int? EVotingCountOfAnswerUnspecifiedTBQ3 { get; set; }

        [Name("TotalVersendeteEVotingSRA")]
        public int? TotalSentEVotingVotingCards { get; set; }

        [Name("FreigabeGde")]
        [TypeConverter(typeof(WabstiCTimeConverter))]
        public DateTime? SubmissionDoneTimestamp { get; set; }

        [Name("GeLfNr")]
        public Guid PoliticalBusinessId { get; set; }

        [Ignore]
        public CountingCircleResultState ResultState { get; set; }

        public IEnumerable<BallotQuestionResult> QuestionResults
        {
            set
            {
                using var enumerator = value.GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    return;
                }

                TotalCountOfAnswerYesQ1 = enumerator.Current.TotalCountOfAnswerYes;
                ConventionalAndECountingCountOfAnswerYesQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerYes.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerYes;
                EVotingCountOfAnswerYesQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerYes;
                TotalCountOfAnswerNoQ1 = enumerator.Current.TotalCountOfAnswerNo;
                ConventionalAndECountingCountOfAnswerNoQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerNo.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerNo;
                EVotingCountOfAnswerNoQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerNo;
                TotalCountOfAnswerUnspecifiedQ1 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalAndECountingCountOfAnswerUnspecifiedQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerUnspecified;
                EVotingCountOfAnswerUnspecifiedQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerUnspecified;

                if (!enumerator.MoveNext())
                {
                    return;
                }

                TotalCountOfAnswerYesQ2 = enumerator.Current.TotalCountOfAnswerYes;
                ConventionalAndECountingCountOfAnswerYesQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerYes.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerYes;
                EVotingCountOfAnswerYesQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerYes;
                TotalCountOfAnswerNoQ2 = enumerator.Current.TotalCountOfAnswerNo;
                ConventionalAndECountingCountOfAnswerNoQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerNo.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerNo;
                EVotingCountOfAnswerNoQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerNo;
                TotalCountOfAnswerUnspecifiedQ2 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalAndECountingCountOfAnswerUnspecifiedQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerUnspecified;
                EVotingCountOfAnswerUnspecifiedQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerUnspecified;

                if (!enumerator.MoveNext())
                {
                    return;
                }

                TotalCountOfAnswerYesQ3 = enumerator.Current.TotalCountOfAnswerYes;
                ConventionalAndECountingCountOfAnswerYesQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerYes.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerYes;
                EVotingCountOfAnswerYesQ3 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerYes;
                TotalCountOfAnswerNoQ3 = enumerator.Current.TotalCountOfAnswerNo;
                ConventionalAndECountingCountOfAnswerNoQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerNo.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerNo;
                EVotingCountOfAnswerNoQ3 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerNo;
                TotalCountOfAnswerUnspecifiedQ3 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalAndECountingCountOfAnswerUnspecifiedQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerUnspecified;
                EVotingCountOfAnswerUnspecifiedQ3 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerUnspecified;
            }
        }

        public IEnumerable<TieBreakQuestionResult> TieBreakQuestionResults
        {
            set
            {
                using var enumerator = value.GetEnumerator();
                if (!enumerator.MoveNext())
                {
                    return;
                }

                TotalCountOfAnswerYesTBQ1 = enumerator.Current.TotalCountOfAnswerQ1;
                ConventionalAndECountingCountOfAnswerYesTBQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ1.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerQ1;
                EVotingCountOfAnswerYesTBQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ1;
                TotalCountOfAnswerNoTBQ1 = enumerator.Current.TotalCountOfAnswerQ2;
                ConventionalAndECountingCountOfAnswerNoTBQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ2.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerQ2;
                EVotingCountOfAnswerNoTBQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ2;
                TotalCountOfAnswerUnspecifiedTBQ1 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalAndECountingCountOfAnswerUnspecifiedTBQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerUnspecified;
                EVotingCountOfAnswerUnspecifiedTBQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerUnspecified;

                if (!enumerator.MoveNext())
                {
                    return;
                }

                TotalCountOfAnswerYesTBQ2 = enumerator.Current.TotalCountOfAnswerQ1;
                ConventionalAndECountingCountOfAnswerYesTBQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ1.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerQ1;
                EVotingCountOfAnswerYesTBQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ1;
                TotalCountOfAnswerNoTBQ2 = enumerator.Current.TotalCountOfAnswerQ2;
                ConventionalAndECountingCountOfAnswerNoTBQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ2.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerQ2;
                EVotingCountOfAnswerNoTBQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ2;
                TotalCountOfAnswerUnspecifiedTBQ2 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalAndECountingCountOfAnswerUnspecifiedTBQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerUnspecified;
                EVotingCountOfAnswerUnspecifiedTBQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerUnspecified;

                if (!enumerator.MoveNext())
                {
                    return;
                }

                TotalCountOfAnswerYesTBQ3 = enumerator.Current.TotalCountOfAnswerQ1;
                ConventionalAndECountingCountOfAnswerYesTBQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ1.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerQ1;
                EVotingCountOfAnswerYesTBQ3 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ1;
                TotalCountOfAnswerNoTBQ3 = enumerator.Current.TotalCountOfAnswerQ2;
                ConventionalAndECountingCountOfAnswerNoTBQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ2.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerQ2;
                EVotingCountOfAnswerNoTBQ3 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ2;
                TotalCountOfAnswerUnspecifiedTBQ3 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalAndECountingCountOfAnswerUnspecifiedTBQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault()
                    + enumerator.Current.ECountingSubTotal.TotalCountOfAnswerUnspecified;
                EVotingCountOfAnswerUnspecifiedTBQ3 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerUnspecified;
            }
        }
    }
}
