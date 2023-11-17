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
                ConventionalReceivedBallots = x.CountOfVoters.ConventionalReceivedBallots ?? 0,
                EVotingReceivedBallots = x.CountOfVoters.EVotingReceivedBallots,
                TotalInvalidBallots = x.CountOfVoters.TotalInvalidBallots,
                ConventionalInvalidBallots = x.CountOfVoters.ConventionalInvalidBallots ?? 0,
                EVotingInvalidBallots = x.CountOfVoters.EVotingInvalidBallots,
                TotalAccountedBallots = x.CountOfVoters.TotalAccountedBallots,
                ConventionalAccountedBallots = x.CountOfVoters.ConventionalAccountedBallots ?? 0,
                EVotingAccountedBallots = x.CountOfVoters.EVotingAccountedBallots,
                TotalBlankBallots = x.CountOfVoters.TotalBlankBallots,
                ConventionalBlankBallots = x.CountOfVoters.ConventionalBlankBallots ?? 0,
                EVotingBlankBallots = x.CountOfVoters.EVotingBlankBallots,
                QuestionResults = x.QuestionResults.OrderBy(qr => qr.Question.Number).ToList(),
                TieBreakQuestionResults = x.TieBreakQuestionResults.OrderBy(qr => qr.Question.Number).ToList(),
                PoliticalBusinessId = x.VoteResult.VoteId,
                SubmissionDoneTimestamp = x.VoteResult.SubmissionDoneTimestamp,
            })
            .ToListAsync(ct);

        foreach (var result in results)
        {
            AttachContestDetails(result, ccDetailsByCountingCircleId);
        }

        return _templateService.RenderToCsv(
            ctx,
            results);
    }

    private void AttachContestDetails(Data data, Dictionary<Guid, ContestCountingCircleDetails> ccDetailsByCountingCircleId)
    {
        if (!ccDetailsByCountingCircleId.TryGetValue(data.CountingCircleId, out var contestDetail))
        {
            return;
        }

        data.TotalCountOfVoters = contestDetail.TotalCountOfVoters;
        data.CountOfVotersMen = contestDetail.CountOfVotersInformationSubTotals
            .Where(x => x.Sex == SexType.Male)
            .Sum(x => x.CountOfVoters.GetValueOrDefault());
        data.CountOfVotersWomen = contestDetail.CountOfVotersInformationSubTotals
            .Where(x => x.Sex == SexType.Female)
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
        public int ConventionalReceivedBallots { get; set; }

        [Name("StmAbgegebenEVoting")]
        public int EVotingReceivedBallots { get; set; }

        [Name("StmUngueltigTotal")]
        public int TotalInvalidBallots { get; set; }

        [Name("StmUngueltigKonventionell")]
        public int ConventionalInvalidBallots { get; set; }

        [Name("StmUngueltigEVoting")]
        public int EVotingInvalidBallots { get; set; }

        [Name("StmLeerTotal")]
        public int TotalBlankBallots { get; set; }

        [Name("StmLeerKonventionell")]
        public int ConventionalBlankBallots { get; set; }

        [Name("StmLeerEVoting")]
        public int EVotingBlankBallots { get; set; }

        [Name("StmGueltigTotal")]
        public int TotalAccountedBallots { get; set; }

        [Name("StmGueltigKonventionell")]
        public int ConventionalAccountedBallots { get; set; }

        [Name("StmGueltigEVoting")]
        public int EVotingAccountedBallots { get; set; }

        [Name("StmHGJaTotal")]
        public int TotalCountOfAnswerYesQ1 { get; set; }

        [Name("StmHGJaKonventionell")]
        public int ConventionalCountOfAnswerYesQ1 { get; set; }

        [Name("StmHGJaEVoting")]
        public int EVotingCountOfAnswerYesQ1 { get; set; }

        [Name("StmHGNeinTotal")]
        public int TotalCountOfAnswerNoQ1 { get; set; }

        [Name("StmHGNeinKonventionell")]
        public int ConventionalCountOfAnswerNoQ1 { get; set; }

        [Name("StmHGNeinEVoting")]
        public int EVotingCountOfAnswerNoQ1 { get; set; }

        [Name("StmHGohneAwTotal")]
        public int TotalCountOfAnswerUnspecifiedQ1 { get; set; }

        [Name("StmHGohneAwKonventionell")]
        public int ConventionalCountOfAnswerUnspecifiedQ1 { get; set; }

        [Name("StmHGohneAwEVoting")]
        public int EVotingCountOfAnswerUnspecifiedQ1 { get; set; }

        [Name("StmN1JaTotal")]
        public int? TotalCountOfAnswerYesQ2 { get; set; }

        [Name("StmN1JaKonventionell")]
        public int? ConventionalCountOfAnswerYesQ2 { get; set; }

        [Name("StmN1JaEVoting")]
        public int? EVotingCountOfAnswerYesQ2 { get; set; }

        [Name("StmN1NeinTotal")]
        public int? TotalCountOfAnswerNoQ2 { get; set; }

        [Name("StmN1NeinKonventionell")]
        public int? ConventionalCountOfAnswerNoQ2 { get; set; }

        [Name("StmN1NeinEVoting")]
        public int? EVotingCountOfAnswerNoQ2 { get; set; }

        [Name("StmN1ohneAwTotal")]
        public int? TotalCountOfAnswerUnspecifiedQ2 { get; set; }

        [Name("StmN1ohneAwKonventionell")]
        public int? ConventionalCountOfAnswerUnspecifiedQ2 { get; set; }

        [Name("StmN1ohneAwEVoting")]
        public int? EVotingCountOfAnswerUnspecifiedQ2 { get; set; }

        [Name("StmN11JaTotal")]
        public int? TotalCountOfAnswerYesQ3 { get; set; }

        [Name("StmN11JaKonventionell")]
        public int? ConventionalCountOfAnswerYesQ3 { get; set; }

        [Name("StmN11JaEVoting")]
        public int? EVotingCountOfAnswerYesQ3 { get; set; }

        [Name("StmN11NeinTotal")]
        public int? TotalCountOfAnswerNoQ3 { get; set; }

        [Name("StmN11NeinKonventionell")]
        public int? ConventionalCountOfAnswerNoQ3 { get; set; }

        [Name("StmN11NeinEVoting")]
        public int? EVotingCountOfAnswerNoQ3 { get; set; }

        [Name("StmN11ohneAwTotal")]
        public int? TotalCountOfAnswerUnspecifiedQ3 { get; set; }

        [Name("StmN11ohneAwKonventionell")]
        public int? ConventionalCountOfAnswerUnspecifiedQ3 { get; set; }

        [Name("StmN11ohneAwEVoting")]
        public int? EVotingCountOfAnswerUnspecifiedQ3 { get; set; }

        [Name("StmN2JaTotal")]
        public int? TotalCountOfAnswerYesTBQ1 { get; set; }

        [Name("StmN2JaKonventionell")]
        public int? ConventionalCountOfAnswerYesTBQ1 { get; set; }

        [Name("StmN2JaEVoting")]
        public int? EVotingCountOfAnswerYesTBQ1 { get; set; }

        [Name("StmN2NeinTotal")]
        public int? TotalCountOfAnswerNoTBQ1 { get; set; }

        [Name("StmN2NeinKonventionell")]
        public int? ConventionalCountOfAnswerNoTBQ1 { get; set; }

        [Name("StmN2NeinEVoting")]
        public int? EVotingCountOfAnswerNoTBQ1 { get; set; }

        [Name("StmN2ohneAwTotal")]
        public int? TotalCountOfAnswerUnspecifiedTBQ1 { get; set; }

        [Name("StmN2ohneAwKonventionell")]
        public int? ConventionalCountOfAnswerUnspecifiedTBQ1 { get; set; }

        [Name("StmN2ohneAwEVoting")]
        public int? EVotingCountOfAnswerUnspecifiedTBQ1 { get; set; }

        [Name("StmN21JaTotal")]
        public int? TotalCountOfAnswerYesTBQ2 { get; set; }

        [Name("StmN21JaKonventionell")]
        public int? ConventionalCountOfAnswerYesTBQ2 { get; set; }

        [Name("StmN21JaEVoting")]
        public int? EVotingCountOfAnswerYesTBQ2 { get; set; }

        [Name("StmN21NeinTotal")]
        public int? TotalCountOfAnswerNoTBQ2 { get; set; }

        [Name("StmN21NeinKonventionell")]
        public int? ConventionalCountOfAnswerNoTBQ2 { get; set; }

        [Name("StmN21NeinEVoting")]
        public int? EVotingCountOfAnswerNoTBQ2 { get; set; }

        [Name("StmN21ohneAwTotal")]
        public int? TotalCountOfAnswerUnspecifiedTBQ2 { get; set; }

        [Name("StmN21ohneAwKonventionell")]
        public int? ConventionalCountOfAnswerUnspecifiedTBQ2 { get; set; }

        [Name("StmN21ohneAwEVoting")]
        public int? EVotingCountOfAnswerUnspecifiedTBQ2 { get; set; }

        [Name("StmN22JaTotal")]
        public int? TotalCountOfAnswerYesTBQ3 { get; set; }

        [Name("StmN22JaKonventionell")]
        public int? ConventionalCountOfAnswerYesTBQ3 { get; set; }

        [Name("StmN22JaEVoting")]
        public int? EVotingCountOfAnswerYesTBQ3 { get; set; }

        [Name("StmN22NeinTotal")]
        public int? TotalCountOfAnswerNoTBQ3 { get; set; }

        [Name("StmN22NeinKonventionell")]
        public int? ConventionalCountOfAnswerNoTBQ3 { get; set; }

        [Name("StmN22NeinEVoting")]
        public int? EVotingCountOfAnswerNoTBQ3 { get; set; }

        [Name("StmN22ohneAwTotal")]
        public int? TotalCountOfAnswerUnspecifiedTBQ3 { get; set; }

        [Name("StmN22ohneAwKonventionell")]
        public int? ConventionalCountOfAnswerUnspecifiedTBQ3 { get; set; }

        [Name("StmN22ohneAwEVoting")]
        public int? EVotingCountOfAnswerUnspecifiedTBQ3 { get; set; }

        // We currently do not have this information, but the customers wants to have this field included in the CSV
        [Name("TotalVersendeteEVotingSRA")]
        public int? TotalSentEVotingVotingCards { get; set; }

        [Name("FreigabeGde")]
        [TypeConverter(typeof(WabstiCTimeConverter))]
        public DateTime? SubmissionDoneTimestamp { get; set; }

        [Name("GeLfNr")]
        public Guid PoliticalBusinessId { get; set; }

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
                ConventionalCountOfAnswerYesQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerYes.GetValueOrDefault();
                EVotingCountOfAnswerYesQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerYes;
                TotalCountOfAnswerNoQ1 = enumerator.Current.TotalCountOfAnswerNo;
                ConventionalCountOfAnswerNoQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerNo.GetValueOrDefault();
                EVotingCountOfAnswerNoQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerNo;
                TotalCountOfAnswerUnspecifiedQ1 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalCountOfAnswerUnspecifiedQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault();
                EVotingCountOfAnswerUnspecifiedQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerUnspecified;

                if (!enumerator.MoveNext())
                {
                    return;
                }

                TotalCountOfAnswerYesQ2 = enumerator.Current.TotalCountOfAnswerYes;
                ConventionalCountOfAnswerYesQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerYes.GetValueOrDefault();
                EVotingCountOfAnswerYesQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerYes;
                TotalCountOfAnswerNoQ2 = enumerator.Current.TotalCountOfAnswerNo;
                ConventionalCountOfAnswerNoQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerNo.GetValueOrDefault();
                EVotingCountOfAnswerNoQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerNo;
                TotalCountOfAnswerUnspecifiedQ2 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalCountOfAnswerUnspecifiedQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault();
                EVotingCountOfAnswerUnspecifiedQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerUnspecified;

                if (!enumerator.MoveNext())
                {
                    return;
                }

                TotalCountOfAnswerYesQ3 = enumerator.Current.TotalCountOfAnswerYes;
                ConventionalCountOfAnswerYesQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerYes.GetValueOrDefault();
                EVotingCountOfAnswerYesQ3 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerYes;
                TotalCountOfAnswerNoQ3 = enumerator.Current.TotalCountOfAnswerNo;
                ConventionalCountOfAnswerNoQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerNo.GetValueOrDefault();
                EVotingCountOfAnswerNoQ3 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerNo;
                TotalCountOfAnswerUnspecifiedQ3 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalCountOfAnswerUnspecifiedQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault();
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
                ConventionalCountOfAnswerYesTBQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ1.GetValueOrDefault();
                EVotingCountOfAnswerYesTBQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ1;
                TotalCountOfAnswerNoTBQ1 = enumerator.Current.TotalCountOfAnswerQ2;
                ConventionalCountOfAnswerNoTBQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ2.GetValueOrDefault();
                EVotingCountOfAnswerNoTBQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ2;
                TotalCountOfAnswerUnspecifiedTBQ1 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalCountOfAnswerUnspecifiedTBQ1 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault();
                EVotingCountOfAnswerUnspecifiedTBQ1 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerUnspecified;

                if (!enumerator.MoveNext())
                {
                    return;
                }

                TotalCountOfAnswerYesTBQ2 = enumerator.Current.TotalCountOfAnswerQ1;
                ConventionalCountOfAnswerYesTBQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ1.GetValueOrDefault();
                EVotingCountOfAnswerYesTBQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ1;
                TotalCountOfAnswerNoTBQ2 = enumerator.Current.TotalCountOfAnswerQ2;
                ConventionalCountOfAnswerNoTBQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ2.GetValueOrDefault();
                EVotingCountOfAnswerNoTBQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ2;
                TotalCountOfAnswerUnspecifiedTBQ2 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalCountOfAnswerUnspecifiedTBQ2 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault();
                EVotingCountOfAnswerUnspecifiedTBQ2 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerUnspecified;

                if (!enumerator.MoveNext())
                {
                    return;
                }

                TotalCountOfAnswerYesTBQ3 = enumerator.Current.TotalCountOfAnswerQ1;
                ConventionalCountOfAnswerYesTBQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ1.GetValueOrDefault();
                EVotingCountOfAnswerYesTBQ3 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ1;
                TotalCountOfAnswerNoTBQ3 = enumerator.Current.TotalCountOfAnswerQ2;
                ConventionalCountOfAnswerNoTBQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerQ2.GetValueOrDefault();
                EVotingCountOfAnswerNoTBQ3 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerQ2;
                TotalCountOfAnswerUnspecifiedTBQ3 = enumerator.Current.TotalCountOfAnswerUnspecified;
                ConventionalCountOfAnswerUnspecifiedTBQ3 = enumerator.Current.ConventionalSubTotal.TotalCountOfAnswerUnspecified.GetValueOrDefault();
                EVotingCountOfAnswerUnspecifiedTBQ3 = enumerator.Current.EVotingSubTotal.TotalCountOfAnswerUnspecified;
            }
        }
    }
}
