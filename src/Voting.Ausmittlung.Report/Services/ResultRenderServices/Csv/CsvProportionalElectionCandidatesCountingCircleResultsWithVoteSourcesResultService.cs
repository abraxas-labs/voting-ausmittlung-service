// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.Converter;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv.WabstiC.Data;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Csv;

public class CsvProportionalElectionCandidatesCountingCircleResultsWithVoteSourcesResultService : IRendererService
{
    private const string FileNameParamReplacement = "_";
    private static readonly Regex _validFileNameParam = new("[. ]+", RegexOptions.Compiled, TimeSpan.FromMicroseconds(100));

    private readonly TemplateService _templateService;
    private readonly IDbRepository<DataContext, ProportionalElectionList> _listRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionCandidateResult> _repo;
    private readonly IDbRepository<DataContext, ProportionalElection> _electionRepo;
    private readonly IClock _clock;

    public CsvProportionalElectionCandidatesCountingCircleResultsWithVoteSourcesResultService(
        TemplateService templateService,
        IDbRepository<DataContext, ProportionalElectionCandidateResult> repo,
        IDbRepository<DataContext, ProportionalElectionList> listRepo,
        IClock clock,
        IDbRepository<DataContext, ProportionalElection> electionRepo)
    {
        _templateService = templateService;
        _repo = repo;
        _listRepo = listRepo;
        _clock = clock;
        _electionRepo = electionRepo;
    }

    public async Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        var lists = await _listRepo.Query()
            .Include(x => x.Translations)
            .Where(c => c.ProportionalElectionId == ctx.PoliticalBusinessId)
            .OrderBy(x => x.Position)
            .ToListAsync(ct);

        var records = _repo.Query()
            .AsSplitQuery()
            .Where(c => c.ListResult.Result.ProportionalElectionId == ctx.PoliticalBusinessId)
            .OrderBy(c => c.ListResult.Result.CountingCircle.Code)
            .ThenBy(c => c.ListResult.Result.CountingCircle.Name)
            .ThenBy(c => c.ListResult.List.Position)
            .ThenBy(c => c.Candidate.Position)
            .Select(c => new Data
            {
                ElectionTranslations = c.ListResult.Result.ProportionalElection.Translations,
                CountingCircleCode = c.ListResult.Result.CountingCircle.Code,
                CountingCircleName = c.ListResult.Result.CountingCircle.Name,
                CountingCircleBfs = c.ListResult.Result.CountingCircle.Bfs,
                FirstName = c.Candidate.FirstName,
                LastName = c.Candidate.LastName,
                Locality = c.Candidate.Locality,
                CandidateNumber = c.Candidate.Number,
                CandidateNumberIncludingList = $"{c.ListResult.List.OrderNumber}.{c.Candidate.Number}",
                ListNumber = c.ListResult.List.OrderNumber,
                ListTranslations = c.ListResult.List.Translations,
                UnmodifiedListsCount = c.ListResult.UnmodifiedListsCount,
                ModifiedListsCount = c.ListResult.ModifiedListsCount,
                CandidateVotesOnOtherLists = c.CountOfVotesOnOtherLists,
                CandidateUnmodifiedVoteCount = c.UnmodifiedListVotesCount,
                CandidateModifiedVoteCount = c.ModifiedListVotesCount,
                CandidateTotalVoteCount = c.VoteCount,
                ListBlankRowsCount = c.ListResult.BlankRowsCount,
                ListTotalVoteCount = c.ListResult.TotalVoteCount,
                TotalCountOfModifiedLists = c.ListResult.Result.TotalCountOfModifiedLists,
                TotalCountOfListsWithoutParty = c.ListResult.Result.TotalCountOfListsWithoutParty,
                VoteSources = c.VoteSources.ToList(),
                SubmissionDoneTimestamp = c.ListResult.Result.SubmissionDoneTimestamp,
                AuditedTentativelyTimestamp = c.ListResult.Result.AuditedTentativelyTimestamp,
                ElectionId = c.ListResult.Result.ProportionalElectionId,
                BasisCountingCircleId = c.ListResult.Result.CountingCircle.BasisCountingCircleId,
            })
            .AsAsyncEnumerable()
            .Select(row =>
            {
                SetVoteSources(lists, row);
                row.ReportGeneratedTimestamp = now;
                return row;
            });

        return _templateService.RenderToDynamicCsv(
            ctx,
            records,
            await LoadDomainOfInfluenceShortName(ctx));
    }

    private void SetVoteSources(
        IReadOnlyCollection<ProportionalElectionList> lists,
        Data record)
    {
        var voteSources = new SortedDictionary<string, int?>();

        var voteSourcesVoteCounts = record.VoteSources!
            .ToDictionary(x => x.ListId ?? Guid.Empty, x => x.VoteCount as int?);

        foreach (var list in lists)
        {
            AddVoteSource(voteSources, list, voteSourcesVoteCounts);
        }

        AddVoteSource(voteSources, null, voteSourcesVoteCounts);
        record.AllVoteSources = voteSources;
    }

    private void AddVoteSource(
        IDictionary<string, int?> voteSources,
        ProportionalElectionList? list,
        IReadOnlyDictionary<Guid, int?> voteSourceVoteCounts)
    {
        var description = list != null
            ? $"{list.OrderNumber}.{list.ShortDescription}"
            : WabstiCConstants.ListWithoutPartyNumberAndDescription;

        voteSourceVoteCounts.TryGetValue(list?.Id ?? Guid.Empty, out var voteCount);
        voteSources.Add(description, voteCount);
    }

    private async Task<string> LoadDomainOfInfluenceShortName(ReportRenderContext ctx)
    {
        var domainOfInfluenceShortName = await _electionRepo.Query()
            .Where(pe => pe.Id == ctx.PoliticalBusinessId)
            .Select(pe => pe.DomainOfInfluence.ShortName)
            .FirstOrDefaultAsync() ?? throw new EntityNotFoundException(nameof(ProportionalElection), ctx.PoliticalBusinessId);

        return _validFileNameParam.Replace(domainOfInfluenceShortName, FileNameParamReplacement);
    }

    private class Data
    {
        [Ignore]
        public ICollection<ProportionalElectionTranslation> ElectionTranslations { get; set; } = new HashSet<ProportionalElectionTranslation>();

        [Name("Geschaeft")]
        public string ElectionShortDescription => ElectionTranslations.GetTranslated(x => x.ShortDescription);

        [Name("Einheit_Code")]
        public string CountingCircleCode { get; set; } = string.Empty;

        [Name("Einheit_Name")]
        public string CountingCircleName { get; set; } = string.Empty;

        [Name("Einheit_Bfs")]
        public string CountingCircleBfs { get; set; } = string.Empty;

        [Name("Kand_Nachname")]
        public string LastName { get; set; } = string.Empty;

        [Name("Kand_Vorname")]
        public string FirstName { get; set; } = string.Empty;

        [Name("Kand_Ort")]
        public string Locality { get; set; } = string.Empty;

        [Name("Kand_ID")]
        public string CandidateNumber { get; set; } = string.Empty;

        [Name("List_KandID")]
        public string CandidateNumberIncludingList { get; set; } = string.Empty;

        [Name("Liste_ID")]
        public string ListNumber { get; set; } = string.Empty;

        [Ignore]
        public ICollection<ProportionalElectionListTranslation> ListTranslations { get; set; } = new HashSet<ProportionalElectionListTranslation>();

        [Name("Liste_Code")]
        public string ListShortDescription => ListTranslations.GetTranslated(x => x.ShortDescription);

        [Name("Liste_Bez")]
        public string ListDescription => ListTranslations.GetTranslated(x => x.Description);

        [Name("Liste_WZUnveraendert")]
        public int UnmodifiedListsCount { get; set; }

        [Name("Liste_WZVeraendert")]
        public int ModifiedListsCount { get; set; }

        [Name("Kand_StimmenPanaschiert")]
        public int CandidateVotesOnOtherLists { get; set; }

        [Name("Kand_StimmenUnveraendert")]
        public int CandidateUnmodifiedVoteCount { get; set; }

        [Name("Kand_StimmenVeraendert")]
        public int CandidateModifiedVoteCount { get; set; }

        [Name("Kand_StimmenTotal")]
        public int CandidateTotalVoteCount { get; set; }

        [Name("Liste_Zusatzstimmen")]
        public int ListBlankRowsCount { get; set; }

        [Name("Liste_Parteistimmen")]
        public int ListTotalVoteCount { get; set; }

        [Ignore]
        public List<ProportionalElectionCandidateVoteSourceResult>? VoteSources { get; set; }

        public IDictionary? AllVoteSources { get; set; }

        [Name("TotalWZVeraendert")]
        public int TotalCountOfModifiedLists { get; set; }

        [Name("TotalWZAmtlLeer")]
        public int TotalCountOfListsWithoutParty { get; set; }

        [Name("SperrungVerantwortlich")]
        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime? SubmissionDoneTimestamp { get; set; }

        [Name("SperrungVonOben")]
        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime? AuditedTentativelyTimestamp { get; set; }

        [Name("Zeitpunkt")]
        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime? ReportGeneratedTimestamp { get; set; }

        [Name("LfNr_Geschaefte")]
        public Guid ElectionId { get; set; }

        [Name("LfNr_EinheitenPara")]
        public Guid BasisCountingCircleId { get; set; }
    }
}
