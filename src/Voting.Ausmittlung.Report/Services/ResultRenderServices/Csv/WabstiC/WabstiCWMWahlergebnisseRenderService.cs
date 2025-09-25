// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections;
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
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Helper;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC;

public class WabstiCWMWahlergebnisseRenderService : IRendererService
{
    private const string ColKandIdPrefix = "KandID_";
    private const string ColKandNamePrefix = "KandName_";
    private const string ColKandVoranmePrefix = "KandVorname_";
    private const string ColKandStimmenPrefix = "Stimmen_";
    private const string ColKandResultArtPrefix = "KandResultArt_";

    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, MajorityElection> _repo;

    public WabstiCWMWahlergebnisseRenderService(TemplateService templateService, IDbRepository<DataContext, MajorityElection> repo)
    {
        _templateService = templateService;
        _repo = repo;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var election = await _repo.Query()
            .AsSingleQuery()
            .Where(x => x.Id == ctx.PoliticalBusinessId)
            .Include(x => x.Contest)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.MajorityElectionUnionEntries)
            .Include(x => x.Translations)
            .Include(x => x.EndResult!)
            .FirstAsync(ct);

        var countOfVotersByCountingCircleId = await _repo.Query()
            .SelectMany(x => x.Contest.CountingCircleDetails)
            .Where(x => x.ContestId == election.ContestId)
            .GroupBy(x => x.CountingCircle.BasisCountingCircleId)
            .Select(x => new ContestDetails
            {
                CountingCircleId = x.Key,
                VotingCards = x.Single().VotingCards
                    .Where(z => z.DomainOfInfluenceType == election.DomainOfInfluence.Type && z.Valid)
                    .Sum(z => z.CountOfReceivedVotingCards),
                TotalCountOfVoters = x.Single().CountOfVotersInformationSubTotals
                    .Where(z => z.DomainOfInfluenceType == election.DomainOfInfluence.Type)
                    .Sum(z => z.CountOfVoters),
                SwissAbroadCountOfVoters = x.Single().CountOfVotersInformationSubTotals
                    .Where(y => y.VoterType == VoterType.SwissAbroad && y.DomainOfInfluenceType == election.DomainOfInfluence.Type)
                    .Sum(y => y.CountOfVoters),
            })
            .ToDictionaryAsync(x => x.CountingCircleId, x => x, ct);

        var candidateStatesByCandidateId = await _repo.Query()
            .Where(x => x.Id == ctx.PoliticalBusinessId)
            .SelectMany(x => x.EndResult!.CandidateEndResults)
            .ToDictionaryAsync(x => x.CandidateId, x => x.State, ct);

        var data = _repo.Query()
            .Where(x => x.Id == ctx.PoliticalBusinessId)
            .SelectMany(x => x.Results)
            .Select(x => new Data
            {
                CountingCircleCode = x.CountingCircle.Code,
                CountingCircleSortNumber = x.CountingCircle.SortNumber,
                CountingCircleBfs = x.CountingCircle.Bfs,
                CountingCircleName = x.CountingCircle.Name,
                CountingCircleId = x.CountingCircle.BasisCountingCircleId,
                VoterParticipation = x.CountOfVoters.VoterParticipation,
                AccountedBallots = x.CountOfVoters.TotalAccountedBallots,
                EmptyBallots = x.CountOfVoters.TotalBlankBallots,
                InvalidBallots = x.CountOfVoters.TotalInvalidBallots,
                ReceivedBallots = x.CountOfVoters.TotalReceivedBallots,
                TotalVoteCount = x.TotalVoteCount,
                PlausibilisedTimestamp = x.PlausibilisedTimestamp,
                AuditedTentativelyTimestamp = x.AuditedTentativelyTimestamp,
                SubmissionDoneTimestamp = x.SubmissionDoneTimestamp,
                IndividualVoteCount = x.IndividualVoteCount,
                InvalidVoteCount = x.InvalidVoteCount,
                EmptyVoteCount = x.EmptyVoteCount,
                CandidateResults = x.CandidateResults
                    .OrderBy(c => c.Candidate.Number)
                    .Select(c => new CandidateResultData
                    {
                        CandidateId = c.CandidateId,
                        Number = c.Candidate.Number,
                        LastName = c.Candidate.PoliticalLastName,
                        FirstName = c.Candidate.PoliticalFirstName,
                        VoteCount = c.VoteCount,
                    }).ToList(),
                ResultState = x.State,
            })
            .OrderBy(x => x.CountingCircleSortNumber)
            .AsAsyncEnumerable()
            .Select(x =>
            {
                AttachPoliticalBusinessData(election, x);
                AttachContestDetails(countOfVotersByCountingCircleId, x);
                AttachCandidateResults(election.EndResult!.Calculation, candidateStatesByCandidateId, x);
                x.ResetDataIfSubmissionNotDone();
                return x;
            });

        return _templateService.RenderToDynamicCsv(
            ctx,
            data,
            election.ShortDescription,
            WabstiCDateUtil.BuildDateForFilename(election.Contest.Date));
    }

    private void AttachContestDetails(IReadOnlyDictionary<Guid, ContestDetails> detailsByCcId, Data data)
    {
        var details = detailsByCcId.GetValueOrDefault(data.CountingCircleId);
        data.CountOfVoters = details?.TotalCountOfVoters;
        data.CountOfVotersSwissAbroad = details?.SwissAbroadCountOfVoters;
        data.VotingCards = details?.VotingCards;
    }

    private void AttachPoliticalBusinessData(MajorityElection election, Data row)
    {
        row.ContestDate = election.Contest.Date;
        row.PoliticalBusinessNumber = election.PoliticalBusinessNumber;
        row.DomainOfInfluenceType = election.DomainOfInfluence.Type;
        row.PolticalBusinessOwnerDomainOfInfluenceId = election.DomainOfInfluenceId;
        row.CountOfSecondaryElections = election.MajorityElectionUnionEntries.Count;
        row.PoliticalBusinessTranslations = election.Translations;
        row.NumberOfMandates = election.NumberOfMandates;
        row.AbsoluteMajority = election.EndResult?.Calculation.AbsoluteMajority;
        row.PoliticalBusinessDomainOfInfluenceName = election.DomainOfInfluence.Name;
        row.IndividualCandidatesDisabled = election.IndividualCandidatesDisabled;
    }

    private void AttachCandidateResults(
        MajorityElectionEndResultCalculation calculation,
        IReadOnlyDictionary<Guid, MajorityElectionCandidateEndResultState> candidateStatesByCandidateId,
        Data row)
    {
        var submissionDone = row.ResultState.IsSubmissionDone();
        var data = new Dictionary<string, object?>();
        var i = 1;
        foreach (var candidateResult in row.CandidateResults)
        {
            var voteCount = !submissionDone ? (int?)null : candidateResult.VoteCount;
            var state = ConvertCandidateResultStateToWabstiC(calculation, candidateStatesByCandidateId.GetValueOrDefault(candidateResult.CandidateId));
            data.Add(ColKandIdPrefix + i, candidateResult.Number);
            data.Add(ColKandNamePrefix + i, candidateResult.LastName);
            data.Add(ColKandVoranmePrefix + i, candidateResult.FirstName);
            data.Add(ColKandStimmenPrefix + i, voteCount);
            data.Add(ColKandResultArtPrefix + i, state);
            i++;
        }

        if (!row.IndividualCandidatesDisabled)
        {
            AddFakeCandidateResult(
                i++,
                data,
                WabstiCConstants.IndividualMajorityCandidateNumber,
                WabstiCConstants.IndividualMajorityCandidateLastName,
                row.IndividualVoteCount,
                submissionDone);
        }

        AddFakeCandidateResult(
            i++,
            data,
            WabstiCConstants.EmptyMajorityCandidateNumber,
            WabstiCConstants.EmptyMajorityCandidateLastName,
            row.EmptyVoteCount,
            submissionDone);

        AddFakeCandidateResult(
            i,
            data,
            WabstiCConstants.InvalidMajorityCandidateNumber,
            WabstiCConstants.InvalidMajorityCandidateLastName,
            row.InvalidVoteCount,
            submissionDone);

        row.CandidateResultsDict = data;
    }

    private void AddFakeCandidateResult(int i, Dictionary<string, object?> data, string number, string lastName, int voteCount, bool submissionDone)
    {
        data.Add(ColKandIdPrefix + i, number);
        data.Add(ColKandNamePrefix + i, lastName);
        data.Add(ColKandVoranmePrefix + i, string.Empty);
        data.Add(ColKandStimmenPrefix + i, !submissionDone ? null : voteCount);
        data.Add(ColKandResultArtPrefix + i, 0);
    }

    private int ConvertCandidateResultStateToWabstiC(MajorityElectionEndResultCalculation calc, MajorityElectionCandidateEndResultState state)
    {
        if (calc.AbsoluteMajority is null)
        {
            return state switch
            {
                MajorityElectionCandidateEndResultState.Elected => 6,
                MajorityElectionCandidateEndResultState.NotElected => 7,
                MajorityElectionCandidateEndResultState.NotEligible => 8,
                _ => 0,
            };
        }

        return state switch
        {
            MajorityElectionCandidateEndResultState.AbsoluteMajorityAndElected => 2,
            MajorityElectionCandidateEndResultState.AbsoluteMajorityAndNotElected => 3,
            MajorityElectionCandidateEndResultState.NoAbsoluteMajorityAndNotElectedButRankOk => 4,
            MajorityElectionCandidateEndResultState.NotElected => 5,
            MajorityElectionCandidateEndResultState.NotEligible => 8,
            _ => 0,
        };
    }

    private class ContestDetails
    {
        public Guid CountingCircleId { get; set; }

        public int? TotalCountOfVoters { get; set; }

        public int? SwissAbroadCountOfVoters { get; set; }

        public int? VotingCards { get; set; }
    }

    private class CandidateResultData
    {
        public Guid CandidateId { get; set; }

        public string Number { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public int VoteCount { get; set; }
    }

    private class Data : IWabstiCPoliticalResultData
    {
        [Name("GroupHGN1")]
        public int GroupHgn1 => 1;

        [Name("GroupPage")]
        public int GroupPage => 1;

        [Name("Wahlsonntag")]
        [TypeConverter(typeof(WabstiCDateConverter))]
        public DateTime ContestDate { get; set; }

        [Name("GeschBezUmgang")]
        public string? PoliticalBusinessShortDescription => PoliticalBusinessTranslations?.GetTranslated(x => x.ShortDescription);

        [Ignore]
        public ICollection<MajorityElectionTranslation>? PoliticalBusinessTranslations { get; set; }

        [Name("GeNrSort")]
        public string? PoliticalBusinessNumber { get; set; }

        [Name("GeschArt")]
        [TypeConverter(typeof(WabstiCDomainOfIncluenceTypeConverter))]
        public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

        [Name("GeschTyp")]
        [TypeConverter(typeof(WabstiCMajoritySecondaryElectionsCountConverter))]
        public int CountOfSecondaryElections { get; set; }

        [Name("AnzMandate")]
        public int NumberOfMandates { get; set; }

        [Name("AbsolutesMehr")]
        public int? AbsoluteMajority { get; set; }

        [Name("Einheit")]
        public string? CountingCircleCode { get; set; }

        [Name("SortPolitisch")]
        public int CountingCircleSortNumber { get; set; }

        [Name("BFS")]
        public string? CountingCircleBfs { get; set; }

        [Name("SortBfs")]
        public string? CountingCircleBfs2 => CountingCircleBfs;

        [Name("BezirkGruppe")]
        public string? BezirkGruppe { get; set; }

        [Name("WahlkreisTopBez")]
        public string? PoliticalBusinessDomainOfInfluenceName { get; set; }

        [Name("EinheitBez")]
        public string? CountingCircleName { get; set; }

        [Name("StimmBer")]
        public int? CountOfVoters { get; set; }

        [Name("StimmBerAusl")]
        public int? CountOfVotersSwissAbroad { get; set; }

        [Name("StimmBetProzent")]
        [TypeConverter(typeof(WabstiCPercentDecimalConverter))]
        public decimal? VoterParticipation { get; set; }

        [Name("StimmAusweise")]
        public int? VotingCards { get; set; }

        [Name("UmschlaegeOhneAusweis")]
        public int? UmschlaegeOhneAusweis => null;

        [Name("WzAbgegeben")]
        public int? ReceivedBallots { get; set; }

        [Name("WzEingUngueltig")]
        public int? InvalidBallotsDeprecated { get; set; } = 0;

        [Name("WzEingGueltig")]
        public int? ValidBallotsDeprecated { get; set; } = 0;

        [Name("WzLeer")]
        public int? EmptyBallots { get; set; }

        [Name("WzUngueltig")]
        public int? InvalidBallots { get; set; }

        [Name("WzUngestempelt")]
        public int? UnaccountedBallotsDeprecated { get; set; }

        [Name("WzNichtbeachtet")]
        public int? UnaccountedBallots => EmptyBallots + InvalidBallots;

        [Name("WzGueltig")]
        public int? AccountedBallots { get; set; }

        [Name("MassgebStimTotal")]
        public int? TotalVoteCount { get; set; }

        [Name("ZeitSperrungVerantw")]
        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime? SubmissionDoneTimestamp { get; set; }

        [Name("ZeitSperrungVonOben")]
        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime? AuditedTentativelyTimestamp { get; set; }

        [Name("ZeitpunktDrucken")]
        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime? PlausibilisedTimestamp { get; set; }

        [Name("Parent")]
        public Guid PolticalBusinessOwnerDomainOfInfluenceId { get; set; }

        [Name("LfNr")]
        public Guid CountingCircleId { get; set; }

        [Name("WahlkreisToop")]
        public Guid PolticalBusinessOwnerDomainOfInfluenceId2 => PolticalBusinessOwnerDomainOfInfluenceId;

        [Name("IstOberbehörde")]
        public bool IsParent => false;

        [Name("IsUserLevelOne")]
        public bool IsUserLevelOne => true;

        [Name("SortIntern")]
        public int CountingCircleSortNumber2 => CountingCircleSortNumber;

        [Name("SortIntern2")]
        public int? CountingCircleSortDeprecated { get; set; }

        [Name("MaxRekursiv")]
        public int? MaxRekursiv { get; set; }

        [Name("Rekursiv")]
        public int? Rekursiv { get; set; }

        [Ignore]
        public int IndividualVoteCount { get; set; }

        [Ignore]
        public int EmptyVoteCount { get; set; }

        [Ignore]
        public int InvalidVoteCount { get; set; }

        [Ignore]
        public bool IndividualCandidatesDisabled { get; set; }

        public IDictionary? CandidateResultsDict { get; set; }

        [Ignore]
        public IReadOnlyCollection<CandidateResultData> CandidateResults { get; set; } = Array.Empty<CandidateResultData>();

        [Ignore]
        public CountingCircleResultState ResultState { get; set; }

        public void ResetDataIfSubmissionNotDone()
        {
            if (ResultState.IsSubmissionDone())
            {
                return;
            }

            VoterParticipation = null;
            AccountedBallots = null;
            EmptyBallots = null;
            InvalidBallots = null;
            ReceivedBallots = null;
            TotalVoteCount = null;
            VoterParticipation = null;
            InvalidBallotsDeprecated = null;
            ValidBallotsDeprecated = null;
            CountOfVoters = null;
            CountOfVotersSwissAbroad = null;
            VotingCards = null;
        }
    }
}
