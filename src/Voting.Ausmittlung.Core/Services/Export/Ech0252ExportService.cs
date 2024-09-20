// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Services.Export.Models;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Ech.Converters;
using Voting.Ausmittlung.Ech.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Ech;
using Voting.Lib.Iam.Store;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Core.Services.Export;

public class Ech0252ExportService
{
    private const int MaxDaysRange = 365 * 10;
    private const string Ech0252ExportFileName = "eCH-0252_{0}_{1}_{2}";

    private static readonly (string, ExportFileFormat) _exportData = ("eCH-0252-api", ExportFileFormat.Xml);

    private readonly Ech0252Serializer _ech0252Serializer;
    private readonly EchSerializer _echSerializer;
    private readonly ContestRepo _contestRepo;
    private readonly DomainOfInfluenceRepo _domainOfInfluenceRepo;
    private readonly IClock _clock;
    private readonly IAuth _auth;
    private readonly SimplePoliticalBusinessRepo _simplePoliticalBusinessRepo;
    private readonly ExportRateLimitService _rateLimitService;
    private readonly IDbRepository<DataContext, CantonSettings> _cantonSettingsRepo;

    public Ech0252ExportService(
        Ech0252Serializer ech0252Serializer,
        EchSerializer echSerializer,
        ContestRepo contestRepo,
        IClock clock,
        IAuth auth,
        SimplePoliticalBusinessRepo simplePoliticalBusinessRepo,
        ExportRateLimitService rateLimitService,
        IDbRepository<DataContext, CantonSettings> cantonSettingsRepo,
        DomainOfInfluenceRepo domainOfInfluenceRepo)
    {
        _ech0252Serializer = ech0252Serializer;
        _echSerializer = echSerializer;
        _contestRepo = contestRepo;
        _clock = clock;
        _auth = auth;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
        _rateLimitService = rateLimitService;
        _cantonSettingsRepo = cantonSettingsRepo;
        _domainOfInfluenceRepo = domainOfInfluenceRepo;
    }

    public async IAsyncEnumerable<FileModel> GenerateExports(
        Ech0252FilterModel filter,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var contestsEnumerable = await LoadContests(filter, ct);
        var doisByContestId = await LoadDomainOfInfluences(filter);

        await foreach (var contest in contestsEnumerable)
        {
            ct.ThrowIfCancellationRequested();

            if (PrepareContestDataAndCheckIsEmpty(contest))
            {
                continue;
            }

            var ctx = new Ech0252MappingContext(doisByContestId[contest.Id]);

            if (contest.Votes.Count > 0)
            {
                var voteDelivery = _ech0252Serializer.ToVoteDelivery(
                    contest,
                    ctx,
                    filter.CountingCircleResultStates.Count > 0 ? filter.CountingCircleResultStates : null);

                yield return RenderToXml(contest, voteDelivery, "vote-result-delivery");
            }

            if (contest.ProportionalElections.Count > 0)
            {
                var electionDelivery = filter.InformationOnly
                    ? _ech0252Serializer.ToProportionalElectionInformationDelivery(contest, ctx)
                    : _ech0252Serializer.ToProportionalElectionResultDelivery(contest);

                yield return RenderToXml(contest, electionDelivery, filter.InformationOnly ? "proportional-election-info-delivery" : "proportional-election-result-delivery");
            }

            if (contest.MajorityElections.Count > 0)
            {
                var electionDelivery = filter.InformationOnly
                    ? _ech0252Serializer.ToMajorityElectionInformationDelivery(contest, ctx)
                    : _ech0252Serializer.ToMajorityElectionResultDelivery(contest);
                yield return RenderToXml(contest, electionDelivery, filter.InformationOnly ? "majority-election-info-delivery" : "majority-election-result-delivery");
            }
        }
    }

    public Ech0252FilterModel BuildAndValidateFilter(Ech0252FilterRequest filterRequest)
    {
        var contestDateFilter = GetContestDateFilter(filterRequest);
        return new()
        {
            ContestDateFrom = contestDateFilter.From,
            ContestDateTo = contestDateFilter.To,
            PoliticalBusinessIds = filterRequest.VotingIdentifications ?? new(),
            PoliticalBusinessTypes = filterRequest.PoliticalBusinessTypes ?? new(),
            CountingCircleResultStates = filterRequest.CountingStates ?? new(),
            InformationOnly = filterRequest.InformationOnly,
        };
    }

    public async Task EnsureCanExport()
    {
        await _rateLimitService.CheckAndLog(new[] { _exportData });
    }

    internal async Task<IAsyncEnumerable<Contest>> LoadContests(Ech0252FilterModel filter, CancellationToken ct = default)
    {
        var tenantId = _auth.Tenant.Id;

        var accessibleCantons = await _cantonSettingsRepo.Query()
            .Where(c => c.SecureConnectId == tenantId)
            .Select(c => c.Canton)
            .ToListAsync(ct);

        var query = BuildQuery(
            filter.ContestDateFrom,
            filter.ContestDateTo,
            filter.PoliticalBusinessIds,
            filter.PoliticalBusinessTypes,
            filter.CountingCircleResultStates,
            filter.InformationOnly,
            accessibleCantons);

        return query.ToAsyncEnumerable();
    }

    internal bool PrepareContestDataAndCheckIsEmpty(Contest contest)
    {
        contest.Votes = contest.Votes.Where(v => v.Results.Count != 0).ToList();

        foreach (var vote in contest.Votes)
        {
            var voteResultById = vote.Results.ToDictionary(v => v.Id);

            foreach (var ballot in vote.Ballots)
            {
                var ballotResultById = ballot.Results.ToDictionary(r => r.Id);

                foreach (var ballotResult in ballot.Results)
                {
                    ballotResult.VoteResult = voteResultById.GetValueOrDefault(ballotResult.VoteResultId)!;
                }

                foreach (var question in ballot.BallotQuestions)
                {
                    foreach (var questionResult in question.Results)
                    {
                        questionResult.BallotResult = ballotResultById.GetValueOrDefault(questionResult.BallotResultId)!;
                    }
                }

                foreach (var tieBreakQuestion in ballot.TieBreakQuestions)
                {
                    foreach (var tieBreakQuestionResult in tieBreakQuestion.Results)
                    {
                        tieBreakQuestionResult.BallotResult = ballotResultById.GetValueOrDefault(tieBreakQuestionResult.BallotResultId)!;
                    }
                }
            }
        }

        return !contest.PoliticalBusinesses.Any() || !contest.PoliticalBusinesses.SelectMany(pb => pb.CountingCircleResults).Any();
    }

    private FileModel RenderToXml(
        Contest contest,
        Ech0252_2_0.Delivery data,
        string type)
    {
        var fileName = FileNameBuilder.GenerateFileName(
            Ech0252ExportFileName,
            ExportFileFormat.Xml,
            new List<string>
            {
                type,
                contest.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                contest.Id.ToString(),
            });

        return new FileModel(null, fileName, ExportFileFormat.Xml, data.DeliveryHeader.MessageId, (w, _) =>
        {
            _echSerializer.WriteXml(w, data);
            return Task.CompletedTask;
        });
    }

    private IQueryable<Contest> BuildQuery(
        DateTime from,
        DateTime? to,
        List<Guid> politicalBusinessIds,
        List<PoliticalBusinessType> politicalBusinessTypes,
        List<CountingCircleResultState> countingCircleResultStates,
        bool informationOnly,
        List<DomainOfInfluenceCanton> accessibleCantons)
    {
        var query = _contestRepo.Query()
            .AsSplitQuery()
            .AsNoTrackingWithIdentityResolution()
            .IgnoreQueryFilters() // eCH exports need all languages, do not filter them
            .Include(c => c.DomainOfInfluence)
            .OrderByDescending(c => c.Date)
            .Where(c => accessibleCantons.Contains(c.DomainOfInfluence.Canton));

        query = to == null
            ? query.Where(c => c.Date == from)
            : query.Where(c => c.Date >= from && c.Date <= to.Value);

        var noPbIdFilter = politicalBusinessIds.Count == 0;
        var noCcResultStateFilter = countingCircleResultStates.Count == 0;
        var noPbTypesFiter = politicalBusinessTypes.Count == 0;

        if (noPbTypesFiter || politicalBusinessTypes.Contains(PoliticalBusinessType.Vote))
        {
            query = query
                .Include(x => x.Votes.Where(v => noPbIdFilter || politicalBusinessIds.Contains(v.Id)))
                    .ThenInclude(x => x.Results) // we filter per counting circle result state in the ech mapper, since the counting circle is required.
                    .ThenInclude(x => x.CountingCircle.DomainOfInfluences)
                    .ThenInclude(x => x.DomainOfInfluence)
                .Include(x => x.Votes)
                    .ThenInclude(x => x.Translations)
                .Include(x => x.Votes)
                    .ThenInclude(x => x.Ballots)
                    .ThenInclude(x => x.Translations)
                .Include(x => x.Votes)
                    .ThenInclude(x => x.Ballots)
                    .ThenInclude(x => x.BallotQuestions)
                    .ThenInclude(x => x.Translations)
                .Include(x => x.Votes)
                    .ThenInclude(x => x.Ballots)
                    .ThenInclude(x => x.TieBreakQuestions)
                    .ThenInclude(x => x.Translations)
                .Include(x => x.Votes)
                    .ThenInclude(x => x.DomainOfInfluence)
                .Include(x => x.Votes)
                    .ThenInclude(x => x.Ballots)
                    .ThenInclude(x => x.Results)
                    .ThenInclude(x => x.CountOfVoters)
                .Include(x => x.Votes)
                    .ThenInclude(x => x.Ballots)
                    .ThenInclude(x => x.BallotQuestions)
                    .ThenInclude(x => x.Results)
                .Include(x => x.Votes)
                    .ThenInclude(x => x.Ballots)
                    .ThenInclude(x => x.TieBreakQuestions)
                    .ThenInclude(x => x.Results);
        }

        if (noPbTypesFiter || politicalBusinessTypes.Contains(PoliticalBusinessType.ProportionalElection))
        {
            if (!informationOnly)
            {
                query = query
                    .Include(x => x.ProportionalElections.Where(v => noPbIdFilter || politicalBusinessIds.Contains(v.Id)))
                        .ThenInclude(x => x.Results.Where(r => noCcResultStateFilter || countingCircleResultStates.Contains(r.State)))
                        .ThenInclude(x => x.CountingCircle)
                    .Include(x => x.ProportionalElections)
                        .ThenInclude(x => x.DomainOfInfluence)
                    .Include(x => x.ProportionalElections)
                        .ThenInclude(x => x.Results)
                        .ThenInclude(x => x.CountOfVoters)
                    .Include(x => x.ProportionalElections)
                        .ThenInclude(x => x.Results)
                        .ThenInclude(x => x.ListResults)
                        .ThenInclude(x => x.CandidateResults)
                    .Include(x => x.ProportionalElections)
                        .ThenInclude(x => x.EndResult!)
                        .ThenInclude(x => x.ListEndResults)
                        .ThenInclude(x => x.CandidateEndResults);
            }
            else
            {
                query = query
                    .Include(x => x.ProportionalElections.Where(v => noPbIdFilter || politicalBusinessIds.Contains(v.Id)))
                        .ThenInclude(x => x.Results)
                        .ThenInclude(x => x.CountingCircle)
                    .Include(x => x.ProportionalElections)
                        .ThenInclude(x => x.Translations)
                    .Include(x => x.ProportionalElections)
                        .ThenInclude(x => x.DomainOfInfluence)
                    .Include(x => x.ProportionalElections)
                        .ThenInclude(x => x.ProportionalElectionLists)
                        .ThenInclude(x => x.Translations)
                    .Include(x => x.ProportionalElections)
                        .ThenInclude(x => x.ProportionalElectionLists)
                        .ThenInclude(x => x.ProportionalElectionCandidates)
                        .ThenInclude(x => x.Translations)
                    .Include(x => x.ProportionalElections)
                        .ThenInclude(x => x.ProportionalElectionLists)
                        .ThenInclude(x => x.ProportionalElectionCandidates)
                        .ThenInclude(x => x.Party!)
                        .ThenInclude(x => x.Translations)
                    .Include(x => x.ProportionalElectionUnions)
                        .ThenInclude(x => x.ProportionalElectionUnionEntries)
                        .ThenInclude(x => x.ProportionalElection);
            }
        }

        if (noPbTypesFiter || politicalBusinessTypes.Contains(PoliticalBusinessType.MajorityElection))
        {
            if (!informationOnly)
            {
                query = query
                    .Include(x => x.MajorityElections.Where(v => noPbIdFilter || politicalBusinessIds.Contains(v.Id)))
                        .ThenInclude(x => x.Results.Where(r => noCcResultStateFilter || countingCircleResultStates.Contains(r.State)))
                        .ThenInclude(x => x.CountingCircle)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.DomainOfInfluence)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.Results)
                        .ThenInclude(x => x.CountOfVoters)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.Results)
                        .ThenInclude(x => x.CandidateResults)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.EndResult!)
                        .ThenInclude(x => x.Calculation)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.EndResult!)
                        .ThenInclude(x => x.CandidateEndResults)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.SecondaryMajorityElections)
                        .ThenInclude(x => x.EndResult!)
                        .ThenInclude(x => x.CandidateEndResults)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.SecondaryMajorityElections)
                        .ThenInclude(x => x.EndResult!)
                        .ThenInclude(x => x.PrimaryMajorityElectionEndResult)
                        .ThenInclude(x => x.Calculation)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.SecondaryMajorityElections)
                        .ThenInclude(x => x.Results)
                        .ThenInclude(x => x.CandidateResults)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.SecondaryMajorityElections)
                        .ThenInclude(x => x.Results)
                        .ThenInclude(x => x.PrimaryResult)
                        .ThenInclude(x => x.CountingCircle);
            }
            else
            {
                query = query
                    .Include(x => x.MajorityElections.Where(v => noPbIdFilter || politicalBusinessIds.Contains(v.Id)))
                        .ThenInclude(x => x.Results)
                        .ThenInclude(x => x.CountingCircle)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.DomainOfInfluence)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.Translations)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.MajorityElectionCandidates)
                        .ThenInclude(x => x.Translations)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.SecondaryMajorityElections)
                        .ThenInclude(x => x.Translations)
                    .Include(x => x.MajorityElections)
                        .ThenInclude(x => x.SecondaryMajorityElections)
                        .ThenInclude(x => x.Candidates)
                        .ThenInclude(x => x.Translations)
                    .Include(x => x.MajorityElectionUnions)
                        .ThenInclude(x => x.MajorityElectionUnionEntries);
            }
        }

        return query.Where(c => c.Votes.Any() || c.ProportionalElections.Any() || c.MajorityElections.Any());
    }

    private async Task<Dictionary<Guid, List<DomainOfInfluence>>> LoadDomainOfInfluences(Ech0252FilterModel filter)
    {
        var query = _domainOfInfluenceRepo.Query();

        // Must match with the filter of the contest query
        query = filter.ContestDateTo == null
            ? query.Where(d => d.SnapshotContest!.Date == filter.ContestDateFrom)
            : query.Where(d => d.SnapshotContest!.Date >= filter.ContestDateFrom && d.SnapshotContest!.Date <= filter.ContestDateTo.Value);

        return await query
            .GroupBy(d => d.SnapshotContestId!.Value)
            .ToDictionaryAsync(x => x.Key, x => x.ToList());
    }

    private (DateTime From, DateTime? To) GetContestDateFilter(Ech0252FilterRequest filterRequest)
    {
        var hasContestDateFilter = filterRequest.PollingDate != null;
        var hasContestDateRangeFilter = filterRequest.PollingDateFrom != null && filterRequest.PollingDateTo != null;
        var hasContestSinceDaysFilter = filterRequest.PollingSinceDays != null;
        var sumOfFilter = Convert.ToInt32(hasContestDateFilter) + Convert.ToInt32(hasContestDateRangeFilter) + Convert.ToInt32(hasContestSinceDaysFilter);

        if (sumOfFilter == 0)
        {
            throw new ValidationException("Contest date or date range is required");
        }

        if (sumOfFilter > 1)
        {
            throw new ValidationException("Only one date filter is allowed and required. Choose one between date, date range and since days.");
        }

        if (hasContestDateFilter)
        {
            return (filterRequest.PollingDate!.Value, null);
        }

        if (hasContestSinceDaysFilter)
        {
            var contestSinceDays = filterRequest.PollingSinceDays!.Value;

            if (contestSinceDays < 0 || contestSinceDays > MaxDaysRange)
            {
                throw new ValidationException($"Since days can maximaly be {MaxDaysRange} days");
            }

            var nowDate = _clock.UtcNow.Date;

            return (nowDate.Subtract(TimeSpan.FromDays(contestSinceDays)), nowDate);
        }

        if (hasContestDateRangeFilter)
        {
            var contestDateFrom = filterRequest.PollingDateFrom!.Value;
            var contestDateTo = filterRequest.PollingDateTo!.Value;

            var contestDateDaysDiff = (contestDateTo - contestDateFrom).TotalDays;

            if (contestDateFrom > contestDateTo)
            {
                throw new ValidationException("From date must be smaller or equal to date");
            }

            if (contestDateDaysDiff > MaxDaysRange)
            {
                throw new ValidationException($"Date range can maximaly be {MaxDaysRange} days");
            }

            return (contestDateFrom, contestDateTo);
        }

        throw new ArgumentException("Contest date or date range is required");
    }
}
