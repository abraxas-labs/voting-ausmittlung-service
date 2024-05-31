// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Core.Services.Write;

public class DoubleProportionalResultWriter
{
    private readonly DoubleProportionalResultReader _dpResultReader;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ContestService _contestService;
    private readonly DoubleProportionalResultRepo _dpResultRepo;
    private readonly ILogger<DoubleProportionalResultWriter> _logger;
    private readonly IMapper _mapper;

    public DoubleProportionalResultWriter(
        DoubleProportionalResultReader dpResultReader,
        IAggregateRepository aggregateRepository,
        ContestService contestService,
        DoubleProportionalResultRepo dpResultRepo,
        ILogger<DoubleProportionalResultWriter> logger,
        IMapper mapper)
    {
        _dpResultReader = dpResultReader;
        _aggregateRepository = aggregateRepository;
        _contestService = contestService;
        _dpResultRepo = dpResultRepo;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task UpdateUnionSuperApportionmentLotDecision(
        Guid proportionalElectionUnionId,
        int lotNumber)
    {
        var availableLotDecisions = await _dpResultReader.GetUnionDoubleProportionalSuperApportionmentAvailableLotDecisions(proportionalElectionUnionId);
        var selectedLotDecision = GetLotDecision(availableLotDecisions, lotNumber);

        var contest = await _dpResultRepo.Query()
                          .Where(x => x.ProportionalElectionUnionId == proportionalElectionUnionId)
                          .Include(x => x.ProportionalElectionUnion!.Contest)
                          .Select(x => x.ProportionalElectionUnion!.Contest)
                          .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(proportionalElectionUnionId);

        _contestService.EnsureNotLocked(contest);
        var dpResultId = AusmittlungUuidV5.BuildDoubleProportionalResult(proportionalElectionUnionId, null, contest.TestingPhaseEnded);

        var aggregate = await _aggregateRepository.GetOrCreateById<ProportionalElectionUnionDoubleProportionalResultAggregate>(dpResultId);
        aggregate.UpdateSuperApportionmentLotDecision(
            proportionalElectionUnionId,
            _mapper.Map<Domain.DoubleProportionalResultSuperApportionmentLotDecision>(selectedLotDecision),
            contest.Id,
            contest.TestingPhaseEnded);

        await _aggregateRepository.Save(aggregate);
        _logger.LogInformation(
            "Updated super apportionment lot decisions for proportional election union dp result {DpResultId}",
            dpResultId);
    }

    public async Task UpdateElectionSuperApportionmentLotDecision(Guid proportionalElectionId, int lotNumber)
    {
        var availableLotDecisions = await _dpResultReader.GetElectionDoubleProportionalSuperApportionmentAvailableLotDecisions(proportionalElectionId);
        var selectedLotDecision = GetLotDecision(availableLotDecisions, lotNumber);

        var contest = await _dpResultRepo.Query()
            .Where(x => x.ProportionalElectionId == proportionalElectionId)
            .Include(x => x.ProportionalElection!.Contest)
            .Select(x => x.ProportionalElection!.Contest)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(proportionalElectionId);

        _contestService.EnsureNotLocked(contest);
        var dpResultId = AusmittlungUuidV5.BuildDoubleProportionalResult(null, proportionalElectionId, contest.TestingPhaseEnded);

        var aggregate = await _aggregateRepository.GetOrCreateById<ProportionalElectionDoubleProportionalResultAggregate>(dpResultId);
        aggregate.UpdateSuperApportionmentLotDecision(
            proportionalElectionId,
            _mapper.Map<Domain.DoubleProportionalResultSuperApportionmentLotDecision>(selectedLotDecision),
            contest.Id,
            contest.TestingPhaseEnded);

        await _aggregateRepository.Save(aggregate);
        _logger.LogInformation(
            "Updated super apportionment lot decisions for proportional election dp result {DpResultId}",
            dpResultId);
    }

    public async Task UpdateUnionSubApportionmentLotDecision(
        Guid proportionalElectionUnionId,
        int lotNumber)
    {
        var availableLotDecisions = await _dpResultReader.GetUnionDoubleProportionalSubApportionmentAvailableLotDecisions(proportionalElectionUnionId);
        var selectedLotDecision = GetLotDecision(availableLotDecisions, lotNumber);

        var contest = await _dpResultRepo.Query()
                          .Where(x => x.ProportionalElectionUnionId == proportionalElectionUnionId)
                          .Include(x => x.ProportionalElectionUnion!.Contest)
                          .Select(x => x.ProportionalElectionUnion!.Contest)
                          .FirstOrDefaultAsync()
                      ?? throw new EntityNotFoundException(proportionalElectionUnionId);

        _contestService.EnsureNotLocked(contest);
        var dpResultId = AusmittlungUuidV5.BuildDoubleProportionalResult(proportionalElectionUnionId, null, contest.TestingPhaseEnded);

        var aggregate = await _aggregateRepository.GetOrCreateById<ProportionalElectionUnionDoubleProportionalResultAggregate>(dpResultId);
        aggregate.UpdateSubApportionmentLotDecision(
            proportionalElectionUnionId,
            _mapper.Map<Domain.DoubleProportionalResultSubApportionmentLotDecision>(selectedLotDecision),
            contest.Id,
            contest.TestingPhaseEnded);

        await _aggregateRepository.Save(aggregate);
        _logger.LogInformation(
            "Updated sub apportionment lot decisions for proportional election union dp result {DpResultId}",
            dpResultId);
    }

    private TLotDecision GetLotDecision<TLotDecision>(IReadOnlyCollection<TLotDecision> lotDecisions, int lotNumber)
    {
        if (lotDecisions.Count == 0)
        {
            throw new ValidationException("No lots available");
        }

        return lotDecisions.ElementAtOrDefault(lotNumber - 1)
            ?? throw new ValidationException($"Lot with number {lotNumber} does not exist");
    }
}
