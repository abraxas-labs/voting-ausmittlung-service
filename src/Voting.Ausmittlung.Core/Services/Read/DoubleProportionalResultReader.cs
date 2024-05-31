// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class DoubleProportionalResultReader
{
    private readonly DoubleProportionalResultRepo _dpResultRepo;
    private readonly PermissionService _permissionService;

    public DoubleProportionalResultReader(DoubleProportionalResultRepo dpResultRepo, PermissionService permissionService)
    {
        _dpResultRepo = dpResultRepo;
        _permissionService = permissionService;
    }

    public async Task<DoubleProportionalResult> GetUnionDoubleProportionalResult(Guid unionId)
    {
        return await _dpResultRepo.GetUnionDoubleProportionalResult(unionId, _permissionService.TenantId)
            ?? throw new EntityNotFoundException(nameof(DoubleProportionalResult), unionId);
    }

    public async Task<DoubleProportionalResult> GetElectionDoubleProportionalResult(Guid proportionalElectionId)
    {
        return await _dpResultRepo.GetElectionDoubleProportionalResult(proportionalElectionId, _permissionService.TenantId)
            ?? throw new EntityNotFoundException(nameof(DoubleProportionalResult), proportionalElectionId);
    }

    public async Task<List<DoubleProportionalResultSuperApportionmentLotDecision>> GetUnionDoubleProportionalSuperApportionmentAvailableLotDecisions(Guid unionId)
    {
        var dpResult = await GetUnionDoubleProportionalResult(unionId);
        return DoubleProportionalAlgorithmLotDecisionsBuilder.BuildSuperApportionmentLotDecisions(dpResult);
    }

    public async Task<List<DoubleProportionalResultSuperApportionmentLotDecision>> GetElectionDoubleProportionalSuperApportionmentAvailableLotDecisions(Guid proportionalElectionId)
    {
        var dpResult = await GetElectionDoubleProportionalResult(proportionalElectionId);
        return DoubleProportionalAlgorithmLotDecisionsBuilder.BuildSuperApportionmentLotDecisions(dpResult);
    }

    public async Task<List<DoubleProportionalResultSubApportionmentLotDecision>> GetUnionDoubleProportionalSubApportionmentAvailableLotDecisions(Guid unionId)
    {
        var dpResult = await GetUnionDoubleProportionalResult(unionId);
        return DoubleProportionalAlgorithmLotDecisionsBuilder.BuildSubApportionmentLotDecisions(dpResult);
    }
}
