// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ResultReader
{
    private readonly ContestReader _contestReader;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, CountingCircle> _countingCircleRepo;
    private readonly ContestCountingCircleDetailsRepo _contestCountingCircleDetailsRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly MessageConsumerHub<ResultStateChanged> _resultStateChangeConsumer;
    private readonly PermissionService _permissionService;
    private readonly ILogger<ResultReader> _logger;

    public ResultReader(
        ContestReader contestReader,
        IDbRepository<DataContext, Contest> contestRepo,
        IDbRepository<DataContext, CountingCircle> countingCircleRepo,
        ContestCountingCircleDetailsRepo contestCountingCircleDetailsRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        MessageConsumerHub<ResultStateChanged> resultStateChangeConsumer,
        PermissionService permissionService,
        ILogger<ResultReader> logger)
    {
        _contestReader = contestReader;
        _contestRepo = contestRepo;
        _countingCircleRepo = countingCircleRepo;
        _contestCountingCircleDetailsRepo = contestCountingCircleDetailsRepo;
        _permissionService = permissionService;
        _logger = logger;
        _simpleResultRepo = simpleResultRepo;
        _resultStateChangeConsumer = resultStateChangeConsumer;
    }

    public async Task ListenToResultStateChanges(
        Guid contestId,
        Func<ResultStateChanged, Task> listener,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listening to result state changes for contest with id {ContestId}", contestId);

        _permissionService.EnsureAnyRole();

        _logger.LogDebug("Listening permission is assured.");

        var ownedPoliticalBusinessIds = await _contestReader.GetAccessiblePoliticalBusinessIds(contestId);
        if (ownedPoliticalBusinessIds.Count == 0)
        {
            throw new EntityNotFoundException("no political businesses found to watch");
        }

        await _resultStateChangeConsumer.Listen(
            e => ownedPoliticalBusinessIds.Contains(e.PoliticalBusinessId),
            listener,
            cancellationToken);
    }

    public async Task<ResultOverview> GetResultOverview(Guid contestId)
    {
        _permissionService.EnsureMonitoringElectionAdmin();

        var tenantId = _permissionService.TenantId;
        var contest = await _contestRepo.Query()
                   .AsSplitQuery()
                   .Include(x => x.Translations)
                   .Include(x => x.DomainOfInfluence)
                   .Include(x => x.SimplePoliticalBusinesses
                       .Where(pb => pb.Active && pb.DomainOfInfluence.SecureConnectId == tenantId && pb.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection)
                       .OrderBy(pb => pb.PoliticalBusinessNumber))
                   .ThenInclude(pb => pb.Translations)
                   .Include(x => x.SimplePoliticalBusinesses)
                   .ThenInclude(pb => pb.SimpleResults.OrderBy(x => x.CountingCircle!.Name))
                   .ThenInclude(r => r.CountingCircle)
                   .Include(x => x.SimplePoliticalBusinesses)
                   .ThenInclude(x => x.DomainOfInfluence)
                   .FirstOrDefaultAsync(c => c.Id == contestId)
               ?? throw new EntityNotFoundException(contestId);

        if (contest.SimplePoliticalBusinesses.Count == 0)
        {
            throw new EntityNotFoundException(nameof(Contest), contestId);
        }

        var countingCircles = contest.SimplePoliticalBusinesses
            .SelectMany(pb => pb.SimpleResults)
            .GroupBy(cc => cc.CountingCircleId)
            .ToDictionary(
                x => x.First().CountingCircle!,
                x => x.ToList());

        return new ResultOverview(contest, countingCircles);
    }

    public async Task<ResultList> GetList(Guid contestId, Guid basisCountingCircleId)
    {
        _permissionService.EnsureAnyRole();
        var tenantId = _permissionService.TenantId;
        await _permissionService.EnsureCanReadBasisCountingCircle(basisCountingCircleId, contestId);

        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .Include(c => c.DomainOfInfluence)
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Id == contestId)
            ?? throw new EntityNotFoundException(contestId);

        var countingCircle = await _countingCircleRepo.Query()
                .Include(x => x.ResponsibleAuthority)
                .Include(x => x.ContactPersonDuringEvent)
                .Include(x => x.ContactPersonAfterEvent)
                .FirstOrDefaultAsync(x => x.BasisCountingCircleId == basisCountingCircleId && x.SnapshotContestId == contestId)
            ?? throw new EntityNotFoundException(basisCountingCircleId);

        var contestCcDetailsId = AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, basisCountingCircleId, contest.TestingPhaseEnded);
        var details = await _contestCountingCircleDetailsRepo.GetWithRelatedEntities(contestCcDetailsId)
            ?? throw new EntityNotFoundException(nameof(ContestCountingCircleDetails), contestCcDetailsId);
        details.OrderVotingCardsAndSubTotals();

        var isPoliticalDoiType = contest.DomainOfInfluence.Type.IsPolitical();
        var results = await _simpleResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.PoliticalBusiness!.Translations)
            .Include(x => x.PoliticalBusiness!.DomainOfInfluence)
            .Where(x =>
                x.CountingCircleId == countingCircle.Id
                && x.PoliticalBusiness!.ContestId == contestId
                && x.PoliticalBusiness.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection)
            .OrderBy(x => isPoliticalDoiType ? x.PoliticalBusiness!.DomainOfInfluence.Type : 0)
            .ThenBy(x => x.PoliticalBusiness!.PoliticalBusinessNumber)
            .ToListAsync();

        var currentTenantIsResponsible = countingCircle.ResponsibleAuthority.SecureConnectId == tenantId || (contest.DomainOfInfluence.SecureConnectId == tenantId && !contest.TestingPhaseEnded);

        return new ResultList(
            contest,
            countingCircle,
            details,
            results,
            currentTenantIsResponsible,
            countingCircle.ContestCountingCircleContactPersonId,
            countingCircle.MustUpdateContactPersons);
    }

    public async Task<IEnumerable<CountingCircleResultComment>> GetComments(Guid resultId)
    {
        _permissionService.EnsureAnyRole();

        var result = await _simpleResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle)
            .Include(x => x.Comments!.OrderByDescending(c => c.CreatedAt))
            .FirstOrDefaultAsync(x => x.Id == resultId)
            ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), resultId);

        await _permissionService.EnsureCanReadCountingCircle(result.CountingCircleId, result.CountingCircle!.SnapshotContestId!.Value);
        return result.Comments!;
    }
}
