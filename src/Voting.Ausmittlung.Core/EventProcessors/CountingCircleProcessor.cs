// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class CountingCircleProcessor :
    IEventProcessor<CountingCircleCreated>,
    IEventProcessor<CountingCircleDeleted>,
    IEventProcessor<CountingCircleUpdated>,
    IEventProcessor<CountingCirclesMergerActivated>
{
    private readonly IDbRepository<DataContext, CountingCircle> _repo;
    private readonly DomainOfInfluenceRepo _doiRepo;
    private readonly DomainOfInfluencePermissionBuilder _permissionBuilder;
    private readonly CountingCircleResultsInitializer _ccResultsInitializer;
    private readonly ContestRepo _contestRepo;
    private readonly IMapper _mapper;

    public CountingCircleProcessor(
        IDbRepository<DataContext, CountingCircle> repo,
        DomainOfInfluenceRepo doiRepo,
        CountingCircleResultsInitializer ccResultsInitializer,
        IMapper mapper,
        DomainOfInfluencePermissionBuilder permissionBuilder,
        ContestRepo contestRepo)
    {
        _repo = repo;
        _doiRepo = doiRepo;
        _ccResultsInitializer = ccResultsInitializer;
        _mapper = mapper;
        _permissionBuilder = permissionBuilder;
        _contestRepo = contestRepo;
    }

    public async Task Process(CountingCircleCreated eventData)
    {
        var countingCircle = _mapper.Map<CountingCircle>(eventData.CountingCircle);
        await _repo.Create(countingCircle);

        var contestInTestingPhase = await _contestRepo.GetContestsInTestingPhase();
        foreach (var contest in contestInTestingPhase)
        {
            var snapshotCountingCircle = _mapper.Map<CountingCircle>(eventData.CountingCircle);
            snapshotCountingCircle.SnapshotForContest(contest.Id);
            await _repo.Create(snapshotCountingCircle);
        }

        await _permissionBuilder.RebuildPermissionTree();
    }

    public async Task Process(CountingCircleUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.CountingCircle.Id);
        var existing = await _repo.Query()
                           .Include(x => x.ContactPersonDuringEvent)
                           .Include(x => x.ContactPersonAfterEvent)
                           .Include(x => x.ResponsibleAuthority)
                           .FirstOrDefaultAsync(x => x.Id == id)
                       ?? throw new EntityNotFoundException(id);

        var countingCircle = _mapper.Map<CountingCircle>(eventData.CountingCircle);
        SetExistingRelationIds(countingCircle, existing);
        await _repo.Update(countingCircle);

        var snapshots = await _repo.Query()
            .Include(x => x.ContactPersonDuringEvent)
            .Include(x => x.ContactPersonAfterEvent)
            .Include(x => x.ResponsibleAuthority)
            .Where(cc => cc.BasisCountingCircleId == id)
            .WhereContestIsInTestingPhase()
            .ToListAsync();

        var snapshotCountingCirclesToUpdate = new List<CountingCircle>();
        foreach (var snapshot in snapshots)
        {
            var snapshotCountingCircle = _mapper.Map<CountingCircle>(eventData.CountingCircle);
            snapshotCountingCircle.SnapshotContestId = snapshot.SnapshotContestId!.Value;
            snapshotCountingCircle.Id = snapshot.Id;
            SetExistingRelationIds(snapshotCountingCircle, snapshot);
            snapshotCountingCirclesToUpdate.Add(snapshotCountingCircle);
        }

        await _repo.UpdateRange(snapshotCountingCirclesToUpdate);

        await _permissionBuilder.RebuildPermissionTree();
    }

    public async Task Process(CountingCirclesMergerActivated eventData)
    {
        var countingCircle = _mapper.Map<CountingCircle>(eventData.Merger.NewCountingCircle);
        var copyFromCcId = GuidParser.Parse(eventData.Merger.CopyFromCountingCircleId);
        var ccIdsToDelete = eventData.Merger.MergedCountingCircleIds.Select(GuidParser.Parse).ToList();

        var copyFromCc = await _repo.Query()
            .Include(cc => cc.DomainOfInfluences)
            .FirstOrDefaultAsync(cc => cc.Id == copyFromCcId)
            ?? throw new EntityNotFoundException(copyFromCcId);

        var copyFromDoiCcs = copyFromCc.DomainOfInfluences.Select(doiCc => new DomainOfInfluenceCountingCircle
        {
            Inherited = doiCc.Inherited,
            DomainOfInfluenceId = doiCc.DomainOfInfluenceId,
        }).ToList();

        // Update basis (non-snapshot) counting circles
        countingCircle.DomainOfInfluences = copyFromDoiCcs;
        await _repo.Create(countingCircle);
        await _repo.DeleteRangeByKey(ccIdsToDelete);

        // Get the snapshotted counting circles including DOI assignment, which we have to copy
        var snapshotCopyFromCcs = await _repo.Query()
            .AsSplitQuery()
            .Include(cc => cc.DomainOfInfluences)
            .Where(cc => cc.BasisCountingCircleId == copyFromCcId)
            .WhereContestIsInTestingPhase()
            .ToListAsync();

        // Update the snapshotted counting circles.
        var countingCircleSnapshotsToCreate = new List<CountingCircle>();
        foreach (var snapshotCopyFromCc in snapshotCopyFromCcs)
        {
            var snapshotCountingCircle = _mapper.Map<CountingCircle>(eventData.Merger.NewCountingCircle);

            // copy the assigned domain of influences
            snapshotCountingCircle.DomainOfInfluences = snapshotCopyFromCc.DomainOfInfluences.Select(doiCc => new DomainOfInfluenceCountingCircle
            {
                Inherited = doiCc.Inherited,
                DomainOfInfluenceId = doiCc.DomainOfInfluenceId,
            }).ToList();

            snapshotCountingCircle.SnapshotForContest(snapshotCopyFromCc.SnapshotContestId!.Value);
            countingCircleSnapshotsToCreate.Add(snapshotCountingCircle);
        }

        await _repo.CreateRange(countingCircleSnapshotsToCreate);

        // Remove old snapshotted counting circles
        var snapshotCcIdsToDelete = await _repo.Query()
            .WhereContestIsInTestingPhase()
            .Where(cc => ccIdsToDelete.Contains(cc.BasisCountingCircleId))
            .Select(cc => cc.Id)
            .ToListAsync();
        await _repo.DeleteRangeByKey(snapshotCcIdsToDelete);

        // Domain of influence IDs with changed domain of influence counting circles to initialize political business counting circle results.
        var updatedDoiIds = snapshotCopyFromCcs
            .SelectMany(cc => cc.DomainOfInfluences)
            .Select(doiCc => doiCc.DomainOfInfluenceId)
            .Distinct()
            .ToList();
        await _ccResultsInitializer.InitializeResults(updatedDoiIds);

        await _permissionBuilder.RebuildPermissionTree();
    }

    public async Task Process(CountingCircleDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.CountingCircleId);
        if (!await _repo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await _repo.DeleteByKey(id);

        var snapshotIdsToDelete = await _repo.Query()
            .Where(cc => cc.BasisCountingCircleId == id)
            .WhereContestIsInTestingPhase()
            .Select(cc => cc.Id)
            .ToListAsync();

        await _repo.DeleteRangeByKey(snapshotIdsToDelete);
        await _permissionBuilder.RebuildPermissionTree();
    }

    private void SetExistingRelationIds(CountingCircle countingCircle, CountingCircle existing)
    {
        countingCircle.ResponsibleAuthority.Id = existing.ResponsibleAuthority.Id;
        countingCircle.ContactPersonDuringEvent.Id = existing.ContactPersonDuringEvent.Id;
        if (countingCircle.ContactPersonAfterEvent != null)
        {
            countingCircle.ContactPersonAfterEvent.Id = existing.ContactPersonAfterEvent?.Id ?? Guid.Empty;
        }
    }
}
