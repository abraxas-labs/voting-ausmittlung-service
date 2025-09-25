// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ResultReader
{
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly ContestCountingCircleDetailsRepo _contestCountingCircleDetailsRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly PermissionService _permissionService;
    private readonly CountingCircleResultsValidationSummariesBuilder _countingCircleResultsValidationResultsBuilder;

    public ResultReader(
        IDbRepository<DataContext, Contest> contestRepo,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        ContestCountingCircleDetailsRepo contestCountingCircleDetailsRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        PermissionService permissionService,
        CountingCircleResultsValidationSummariesBuilder countingCircleResultsValidationResultsBuilder)
    {
        _contestRepo = contestRepo;
        _countingCircleRepo = countingCircleRepo;
        _contestCountingCircleDetailsRepo = contestCountingCircleDetailsRepo;
        _permissionService = permissionService;
        _simpleResultRepo = simpleResultRepo;
        _countingCircleResultsValidationResultsBuilder = countingCircleResultsValidationResultsBuilder;
    }

    public async Task<ResultOverview> GetResultOverview(Guid contestId)
    {
        var tenantId = _permissionService.TenantId;

        var viewablePartialResultsCcIds =
            await _permissionService.GetViewablePartialResultsCountingCircleIds(contestId);

        var contest = await _contestRepo.Query()
                   .AsSplitQuery()
                   .Include(x => x.Translations)
                   .Include(x => x.DomainOfInfluence)
                   .Include(x => x.CantonDefaults)
                   .Include(x => x.SimplePoliticalBusinesses
                       .Where(pb =>
                           pb.Active
                           && pb.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection
                           && (pb.Contest.DomainOfInfluence.SecureConnectId == tenantId
                                || pb.DomainOfInfluence.SecureConnectId == tenantId
                                || pb.SimpleResults.Any(r => viewablePartialResultsCcIds.Contains(r.CountingCircleId))))
                       .OrderBy(pb => pb.PoliticalBusinessNumber))
                   .ThenInclude(pb => pb.Translations)
                   .Include(x => x.SimplePoliticalBusinesses)
                   .ThenInclude(pb => pb.SimpleResults
                       .Where(x => viewablePartialResultsCcIds.Count == 0
                            || viewablePartialResultsCcIds.Contains(x.CountingCircleId)
                            || x.PoliticalBusiness!.DomainOfInfluence.SecureConnectId == tenantId)
                       .OrderBy(x => x.CountingCircle!.Name))
                   .ThenInclude(r => r.CountingCircle!.ContestDetails)
                   .ThenInclude(x => x.VotingCards)
                   .Include(x => x.SimplePoliticalBusinesses)
                   .ThenInclude(x => x.SimpleResults)
                   .ThenInclude(x => x.CountingCircle!.ContestDetails)
                   .ThenInclude(x => x.CountOfVotersInformationSubTotals)
                   .Include(x => x.SimplePoliticalBusinesses)
                   .ThenInclude(pb => pb.SimpleResults)
                   .ThenInclude(r => r.CountingCircle!.ResponsibleAuthority)
                   .Include(x => x.SimplePoliticalBusinesses)
                   .ThenInclude(x => x.DomainOfInfluence)
                   .Include(x => x.ProportionalElectionUnions)
                   .ThenInclude(x => x.ProportionalElectionUnionEntries)
                   .ThenInclude(x => x.ProportionalElection.Translations)
                   .Include(x => x.ProportionalElectionUnions)
                   .ThenInclude(x => x.ProportionalElectionUnionEntries)
                   .ThenInclude(x => x.ProportionalElection.DomainOfInfluence)
                   .Include(x => x.MajorityElectionUnions)
                   .ThenInclude(x => x.MajorityElectionUnionEntries)
                   .ThenInclude(x => x.MajorityElection.Translations)
                   .Include(x => x.MajorityElectionUnions)
                   .ThenInclude(x => x.MajorityElectionUnionEntries)
                   .ThenInclude(x => x.MajorityElection.DomainOfInfluence)
                   .Include(x => x.Votes)
                   .ThenInclude(x => x.Results)
                   .ThenInclude(x => x.Results)
                   .ThenInclude(x => x.QuestionResults.OrderBy(y => y.Question.Number))
                   .ThenInclude(x => x.Question.Translations)
                   .Include(x => x.Votes)
                   .ThenInclude(x => x.Results)
                   .ThenInclude(x => x.Results)
                   .ThenInclude(x => x.TieBreakQuestionResults.OrderBy(y => y.Question.Number))
                   .ThenInclude(x => x.Question.Translations)
                   .Include(x => x.Votes)
                   .ThenInclude(x => x.Results)
                   .ThenInclude(x => x.Results)
                   .ThenInclude(x => x.Ballot)
                   .FirstOrDefaultAsync(c => c.Id == contestId)
               ?? throw new EntityNotFoundException(contestId);

        if (contest.SimplePoliticalBusinesses.Count == 0)
        {
            throw new EntityNotFoundException(nameof(Contest), contestId);
        }

        var voteResultsByVoteId = contest.Votes
            .SelectMany(x => x.Results)
            .GroupBy(x => x.VoteId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var countingCircles = contest.SimplePoliticalBusinesses
            .SelectMany(pb => pb.SimpleResults)
            .Select(r =>
            {
                voteResultsByVoteId.TryGetValue(r.PoliticalBusinessId, out var voteResults);
                return new ResultOverviewCountingCircleResult(r, voteResults == null ? [] : voteResults.Single(x => x.CountingCircleId == r.CountingCircleId).Results.ToList());
            })
            .GroupBy(cc => cc.CountingCircleResult.CountingCircleId)
            .ToDictionary(
                x => x.First().CountingCircleResult.CountingCircle!,
                x => x.ToList());

        var countingCircleDetails = countingCircles.Keys.Select(countingCircle => countingCircle.ContestDetails.FirstOrDefault());
        foreach (var details in countingCircleDetails)
        {
            details?.OrderVotingCardsAndSubTotals();
        }

        var currentTenantIsContestManager = contest.DomainOfInfluence.SecureConnectId == tenantId;
        return new ResultOverview(contest, countingCircles, currentTenantIsContestManager, viewablePartialResultsCcIds.Count > 0);
    }

    public async Task<ResultList> GetList(Guid contestId, Guid basisCountingCircleId)
    {
        var tenantId = _permissionService.TenantId;
        await _permissionService.EnsureCanReadBasisCountingCircle(basisCountingCircleId, contestId);

        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .Include(c => c.DomainOfInfluence)
            .Include(c => c.Translations)
            .Include(c => c.CantonDefaults)
            .FirstOrDefaultAsync(c => c.Id == contestId)
            ?? throw new EntityNotFoundException(contestId);

        var countingCircle = await _countingCircleRepo.Query()
                .AsSplitQuery()
                .Include(x => x.ResponsibleAuthority)
                .Include(x => x.ContactPersonDuringEvent)
                .Include(x => x.ContactPersonAfterEvent)
                .Include(x => x.Electorates.OrderBy(e => e.DomainOfInfluenceTypes[0]))
                .Include(x => x.ContestElectorates.OrderBy(e => e.DomainOfInfluenceTypes[0]))
                .FirstOrDefaultAsync(x => x.BasisCountingCircleId == basisCountingCircleId && x.SnapshotContestId == contestId)
            ?? throw new EntityNotFoundException(basisCountingCircleId);

        var contestCcDetailsId = AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, basisCountingCircleId, contest.TestingPhaseEnded);
        var details = await _contestCountingCircleDetailsRepo.GetWithRelatedEntities(contestCcDetailsId)
            ?? throw new EntityNotFoundException(nameof(ContestCountingCircleDetails), contestCcDetailsId);
        details.OrderVotingCardsAndSubTotals();

        var currentTenantIsResponsible = countingCircle.ResponsibleAuthority.SecureConnectId == tenantId
            || (contest.DomainOfInfluence.SecureConnectId == tenantId && !contest.TestingPhaseEnded);
        var viewablePartialResultsCountingCircleIds = await _permissionService.GetViewablePartialResultsCountingCircleIds(contestId);
        var isPoliticalDoiType = contest.DomainOfInfluence.Type.IsPolitical();

        // results which the current tenant is responsible or contest manager, or is result from an owned political business or result is on a partial result counting circle
        var results = await _simpleResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.PoliticalBusiness!.Translations)
            .Include(x => x.PoliticalBusiness!.DomainOfInfluence)
            .Where(x =>
                x.CountingCircleId == countingCircle.Id
                && x.PoliticalBusiness!.ContestId == contestId
                && x.PoliticalBusiness.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection
                && (countingCircle.ResponsibleAuthority.SecureConnectId == tenantId
                    || contest.DomainOfInfluence.SecureConnectId == tenantId
                    || x.PoliticalBusiness.DomainOfInfluence.SecureConnectId == tenantId
                    || viewablePartialResultsCountingCircleIds.Contains(x.CountingCircleId)))
            .OrderBy(x => isPoliticalDoiType ? x.PoliticalBusiness!.DomainOfInfluence.Type : 0)
            .ThenBy(x => x.PoliticalBusiness!.PoliticalBusinessNumber)
            .ToListAsync();

        var electorateSummary = ContestCountingCircleElectorateSummaryBuilder.Build(
            countingCircle,
            details,
            results.Select(r => r.PoliticalBusiness!.DomainOfInfluence.Type).ToHashSet());

        if (!currentTenantIsResponsible && results.All(r => !r.State.IsSubmissionDone()))
        {
            ResetCountingCircleDetails(details);
        }

        return new ResultList(
            contest,
            countingCircle,
            details,
            electorateSummary,
            results,
            currentTenantIsResponsible,
            countingCircle.ContestCountingCircleContactPersonId,
            countingCircle.MustUpdateContactPersons);
    }

    public async Task<IEnumerable<CountingCircleResultComment>> GetComments(Guid resultId)
    {
        var result = await _simpleResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle)
            .Include(x => x.Comments!.OrderByDescending(c => c.CreatedAt))
            .FirstOrDefaultAsync(x => x.Id == resultId)
            ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), resultId);

        await _permissionService.EnsureCanReadCountingCircle(result.CountingCircleId, result.CountingCircle!.SnapshotContestId!.Value);
        return result.Comments!;
    }

    public async Task<List<ValidationSummary>> GetCountingCircleResultsValidationResults(
        Guid contestId,
        Guid basisCountingCircleId,
        IReadOnlyCollection<Guid> resultIds)
    {
        return await _countingCircleResultsValidationResultsBuilder.BuildValidationSummaries(contestId, basisCountingCircleId, resultIds);
    }

    private void ResetCountingCircleDetails(ContestCountingCircleDetails details)
    {
        details.CountingMachine = CountingMachine.Unspecified;
        details.ResetVotingCardsAndSubTotals();
    }
}
