﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Write;

public class MajorityElectionEndResultWriter : ElectionEndResultWriter<
    MajorityElectionEndResultAvailableLotDecision,
    DataModels.MajorityElectionCandidateBase,
    MajorityElectionEndResultAggregate,
    DataModels.MajorityElectionEndResult>
{
    private readonly MajorityElectionEndResultReader _endResultReader;
    private readonly IDbRepository<DataContext, DataModels.MajorityElectionEndResult> _endResultRepo;

    public MajorityElectionEndResultWriter(
        ILogger<MajorityElectionEndResultWriter> logger,
        IAggregateFactory aggregateFactory,
        IAggregateRepository aggregateRepository,
        MajorityElectionEndResultReader endResultReader,
        ContestService contestService,
        PermissionService permissionService,
        IDbRepository<DataContext, DataModels.MajorityElectionEndResult> endResultRepo,
        SecondFactorTransactionWriter secondFactorTransactionWriter)
        : base(logger, aggregateRepository, aggregateFactory, contestService, permissionService, secondFactorTransactionWriter)
    {
        _endResultReader = endResultReader;
        _endResultRepo = endResultRepo;
    }

    public async Task UpdateEndResultLotDecisions(
        Guid majorityElectionId,
        IReadOnlyCollection<ElectionEndResultLotDecision> lotDecisions)
    {
        var (contestId, testingPhaseEnded) = await ContestService.EnsureNotLockedByPoliticalBusiness(majorityElectionId);
        var availableLotDecisions = await _endResultReader.GetEndResultAvailableLotDecisions(majorityElectionId);

        ValidateLotDecisions(lotDecisions, availableLotDecisions);

        var endResultId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(majorityElectionId, testingPhaseEnded);
        var endResultAggregate = await AggregateRepository.GetOrCreateById<MajorityElectionEndResultAggregate>(endResultId);
        endResultAggregate.UpdateLotDecisions(
            majorityElectionId,
            lotDecisions,
            contestId,
            testingPhaseEnded);
        await AggregateRepository.Save(endResultAggregate);
        Logger.LogInformation(
            "Updated lot decisions for majority election end result {MajorityElectionEndResultId}",
            endResultId);
    }

    protected override Task<DataModels.MajorityElectionEndResult?> GetEndResult(Guid politicalBusinessId, string tenantId)
    {
        return _endResultRepo.Query()
            .FirstOrDefaultAsync(x =>
                x.MajorityElectionId == politicalBusinessId
                && x.MajorityElection.DomainOfInfluence.SecureConnectId == tenantId);
    }

    private void ValidateLotDecisions(
        IReadOnlyCollection<ElectionEndResultLotDecision> lotDecisions,
        MajorityElectionEndResultAvailableLotDecisions availableLotDecisions)
    {
        EnsureValidCandidates(
            lotDecisions,
            availableLotDecisions.PrimaryAndSecondaryLotDecisions);

        EnsureValidRanksInLotDecisions(
            lotDecisions,
            availableLotDecisions.LotDecisions);

        foreach (var secondaryLotDecisions in availableLotDecisions.SecondaryLotDecisions)
        {
            EnsureValidRanksInLotDecisions(
                lotDecisions,
                secondaryLotDecisions.LotDecisions);
        }
    }
}
