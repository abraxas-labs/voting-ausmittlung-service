// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class ContestCountingCircleContactPersonWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly PermissionService _permissionService;
    private readonly IDbRepository<DataContext, DataModels.CountingCircle> _countingCircleRepo;
    private readonly ContestService _contestService;

    public ContestCountingCircleContactPersonWriter(
        IAggregateFactory aggregateFactory,
        IAggregateRepository aggregateRepository,
        PermissionService permissionService,
        IDbRepository<DataContext, DataModels.CountingCircle> countingCircleRepo,
        ContestService contestService)
    {
        _aggregateFactory = aggregateFactory;
        _aggregateRepository = aggregateRepository;
        _permissionService = permissionService;
        _countingCircleRepo = countingCircleRepo;
        _contestService = contestService;
    }

    public async Task<Guid> Create(
        Guid contestId,
        Guid countingCircleId,
        ContactPerson contactPersonDuringEvent,
        bool contactPersonSameDuringEventAsAfter,
        ContactPerson contactPersonAfterEvent)
    {
        _permissionService.EnsureErfassungElectionAdmin();
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(countingCircleId, contestId);
        await _contestService.EnsureNotLocked(contestId);

        if (await _countingCircleRepo.Query()
            .AnyAsync(x => x.BasisCountingCircleId == countingCircleId && x.SnapshotContestId == contestId && x.ContestCountingCircleContactPersonId.HasValue))
        {
            throw new ValidationException($"A contest counting circle contact person exists already for {contestId}/{countingCircleId}");
        }

        var aggregate = _aggregateFactory.New<ContestCountingCircleContactPersonAggregate>();
        aggregate.Create(contestId, countingCircleId, contactPersonDuringEvent, contactPersonSameDuringEventAsAfter, contactPersonAfterEvent);
        await _aggregateRepository.Save(aggregate);
        return aggregate.Id;
    }

    public async Task Update(
        Guid id,
        ContactPerson contactPersonDuringEvent,
        bool contactPersonSameDuringEventAsAfter,
        ContactPerson contactPersonAfterEvent)
    {
        _permissionService.EnsureErfassungElectionAdmin();

        var aggregate = await _aggregateRepository.GetById<ContestCountingCircleContactPersonAggregate>(id);
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(aggregate.CountingCircleId, aggregate.ContestId);
        await _contestService.EnsureNotLocked(aggregate.ContestId);

        aggregate.Update(contactPersonDuringEvent, contactPersonSameDuringEventAsAfter, contactPersonAfterEvent);
        await _aggregateRepository.Save(aggregate);
    }
}
