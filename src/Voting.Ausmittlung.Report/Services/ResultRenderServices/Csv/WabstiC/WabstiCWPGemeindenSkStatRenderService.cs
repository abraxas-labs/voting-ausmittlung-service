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
using Voting.Ausmittlung.Report.Extensions;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Lib.Database.Repositories;
using DomainOfInfluenceType = Voting.Ausmittlung.Data.Models.DomainOfInfluenceType;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

// we use german names here since the entire wabstiC domain is in german and there are no eCH definitions.
public class WabstiCWPGemeindenSkStatRenderService : WabstiCWPBaseRenderService
{
    private readonly WabstiCContestDetailsAttacher _contestDetailsAttacher;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _contestDetailsRepo;

    public WabstiCWPGemeindenSkStatRenderService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElection> repo,
        WabstiCContestDetailsAttacher contestDetailsAttacher,
        IDbRepository<DataContext, ContestCountingCircleDetails> contestDetailsRepo)
        : base(templateService, repo)
    {
        _contestDetailsAttacher = contestDetailsAttacher;
        _contestDetailsRepo = contestDetailsRepo;
    }

    public override async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var results = await Repo.Query()
            .AsSplitQuery()
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
                SortNumber = x.CountingCircle.SortNumber,
                TotalCountOfVoters = x.TotalCountOfVoters,
                TotalCountOfVotersMen = x.TotalCountOfVoters,
                ElectionUnionIds = x.ProportionalElection.ProportionalElectionUnionEntries
                    .Select(y => y.ProportionalElectionUnionId)
                    .OrderBy(y => y)
                    .ToList(),
                VoterParticipation = x.CountOfVoters.VoterParticipation,
                SubmissionDoneTimestamp = x.SubmissionDoneTimestamp,
                AuditedTentativelyTimestamp = x.AuditedTentativelyTimestamp,
                TotalReceivedBallots = x.CountOfVoters.TotalReceivedBallots,
                CountOfAccountedBallots = x.CountOfVoters.TotalAccountedBallots,
                CountOfBlankBallots = x.CountOfVoters.TotalBlankBallots,
                CountOfInvalidBallots = x.CountOfVoters.TotalInvalidBallots,
                TotalCountOfModifiedLists = x.TotalCountOfModifiedLists,
                TotalCountOfUnmodifiedLists = x.TotalCountOfUnmodifiedLists,
                TotalCountOfListsWithParty = x.TotalCountOfListsWithParty,
                TotalCountOfListsWithoutParty = x.TotalCountOfListsWithoutParty,
                TotalCountOfBlankRowsOnListsWithoutParty = x.TotalCountOfBlankRowsOnListsWithoutParty,
                ResultState = x.State,
            })
            .ToListAsync(ct);

        await _contestDetailsAttacher.AttachContestDetails(ctx.ContestId, results, ct);
        await AttachCountOfVoters(ctx.ContestId, results, ct);

        foreach (var result in results)
        {
            result.ResetDataIfSubmissionNotDone();
        }

        return await RenderToCsv(
            ctx,
            results);
    }

    private async Task AttachCountOfVoters(Guid contestId, IEnumerable<Data> data, CancellationToken ct = default)
    {
        var contestDetailsByCountingCircleId = await _contestDetailsRepo.Query()
            .AsSplitQuery()
            .Where(x => x.ContestId == contestId)
            .Include(x => x.CountOfVotersInformationSubTotals)
            .ToDictionaryAsync(x => x.CountingCircleId, ct);

        foreach (var entry in data)
        {
            if (!contestDetailsByCountingCircleId.TryGetValue(entry.CountingCircleId, out var contestDetails))
            {
                continue;
            }

            entry.TotalCountOfVotersMen = contestDetails.CountOfVotersInformationSubTotals
                .Where(x => x.Sex == SexType.Male && entry.DomainOfInfluenceType == x.DomainOfInfluenceType)
                .SumNullable(x => x.CountOfVoters);

            entry.TotalCountOfVotersWomen = contestDetails.CountOfVotersInformationSubTotals
                .Where(x => x.Sex == SexType.Female && entry.DomainOfInfluenceType == x.DomainOfInfluenceType)
                .SumNullable(x => x.CountOfVoters);
        }
    }

    private class Data : IWabstiCContestDetails, IWabstiCPoliticalResultData
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
        public int? TotalCountOfVoters { get; set; }

        [Name("StimmberechtigteMaenner")]
        public int? TotalCountOfVotersMen { get; set; }

        [Name("StimmberechtigteFrauen")]
        public int? TotalCountOfVotersWomen { get; set; }

        [Name("StimmberechtigteAusl")]
        public int? CountOfVotersTotalSwissAbroad { get; set; }

        [Name("StiAusweiseUrne")]
        public int? VotingCardsBallotBox { get; set; }

        [Name("StiAusweiseVorzeitig")]
        public int? VotingCardsPaper { get; set; }

        [Name("StiAusweiseBriefGueltig")]
        public int? VotingCardsByMail { get; set; }

        [Name("StiAusweiseBriefNiUz")]
        public int? VotingCardsByMailNotValid { get; set; }

        [Name("StiAusweiseEVoting")]
        public int? VotingCardsEVoting { get; set; }

        [Name("Stimmbeteiligung")]
        [TypeConverter(typeof(WabstiCPercentageConverter))]
        public decimal? VoterParticipation { get; set; }

        [Name("StmAbgegeben")]
        public int? TotalReceivedBallots { get; set; }

        [Name("StmUngueltig")]
        public int? CountOfInvalidBallots { get; set; }

        [Name("StmLeer")]
        public int? CountOfBlankBallots { get; set; }

        [Name("StmLeerAufWZAmtlLeer")]
        public int? TotalCountOfBlankRowsOnListsWithoutParty { get; set; }

        [Name("StmGueltig")]
        public int? CountOfAccountedBallots { get; set; }

        [Name("AnzWZListe")]
        public int? TotalCountOfListsWithParty { get; set; }

        [Name("AnzWZAmtlLeer")]
        public int? TotalCountOfListsWithoutParty { get; set; }

        [Name("AnzWZUnveraendert")]
        public int? TotalCountOfUnmodifiedLists { get; set; }

        [Name("AnzWZVeraendert")]
        public int? TotalCountOfModifiedLists { get; set; }

        [Name("FreigabeGde")]
        [TypeConverter(typeof(WabstiCTimeConverter))]
        public DateTime? SubmissionDoneTimestamp { get; set; }

        [Name("Sperrung")]
        [TypeConverter(typeof(WabstiCTimeConverter))]
        public DateTime? AuditedTentativelyTimestamp { get; set; }

        [Name("GeLfNr")]
        public Guid PoliticalBusinessId { get; set; }

        [Name("GeVerbNr")]
        public string ElectionUnionIdStrs => string.Join(", ", ElectionUnionIds ?? Array.Empty<Guid>());

        public IEnumerable<Guid>? ElectionUnionIds { get; set; }

        [Ignore]
        public CountingCircleResultState ResultState { get; set; }

        public void ResetDataIfSubmissionNotDone()
        {
            if (ResultState.IsSubmissionDone())
            {
                return;
            }

            TotalCountOfVoters = null;
            TotalCountOfVotersMen = null;
            TotalCountOfVotersWomen = null;
            CountOfVotersTotalSwissAbroad = null;
            VotingCardsBallotBox = null;
            VotingCardsPaper = null;
            VotingCardsByMail = null;
            VotingCardsByMailNotValid = null;
            VotingCardsEVoting = null;
            VoterParticipation = null;
            TotalReceivedBallots = null;
            CountOfInvalidBallots = null;
            CountOfBlankBallots = null;
            TotalCountOfBlankRowsOnListsWithoutParty = null;
            CountOfAccountedBallots = null;
            TotalCountOfListsWithParty = null;
            TotalCountOfListsWithoutParty = null;
            TotalCountOfUnmodifiedLists = null;
            TotalCountOfModifiedLists = null;
        }
    }
}
