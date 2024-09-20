// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Core.Services.Write;

public class ContestCountingCircleElectorateWriter
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly PermissionService _permissionService;
    private readonly ContestService _contestService;
    private readonly ILogger<ContestCountingCircleElectorateWriter> _logger;

    public ContestCountingCircleElectorateWriter(
        IAggregateRepository aggregateRepository,
        IAggregateFactory aggregateFactory,
        PermissionService permissionService,
        ContestService contestService,
        ILogger<ContestCountingCircleElectorateWriter> logger)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateFactory = aggregateFactory;
        _permissionService = permissionService;
        _contestService = contestService;
        _logger = logger;
    }

    public async Task UpdateElectorates(
        Guid contestId,
        Guid basisCountingCircleId,
        IReadOnlyCollection<ContestCountingCircleElectorate> electorates)
    {
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(basisCountingCircleId, contestId);
        await _contestService.EnsureNotLocked(contestId);
        var id = AusmittlungUuidV5.BuildCountingCircleSnapshot(contestId, basisCountingCircleId);

        var aggregate = await _aggregateRepository.TryGetById<ContestCountingCircleElectoratesAggregate>(id);

        if (aggregate == null)
        {
            aggregate = _aggregateFactory.New<ContestCountingCircleElectoratesAggregate>();
            aggregate.CreateFrom(electorates, contestId, basisCountingCircleId);
            _logger.LogInformation("Creating contest counting circle electorates for {ContestCountingCircleId}", id);
        }
        else
        {
            aggregate.UpdateFrom(electorates, contestId, basisCountingCircleId);
            _logger.LogInformation("Updating contest counting circle electorates for {ContestCountingCircleId}", id);
        }

        await _aggregateRepository.Save(aggregate);
    }
}
