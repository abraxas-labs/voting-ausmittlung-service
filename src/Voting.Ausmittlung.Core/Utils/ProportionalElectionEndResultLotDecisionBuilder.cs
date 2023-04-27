// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionEndResultLotDecisionBuilder
{
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePoliticalBusinessRepo;
    private readonly ProportionalElectionListEndResultRepo _listEndResultRepo;
    private readonly ProportionalElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly DataContext _dataContext;

    public ProportionalElectionEndResultLotDecisionBuilder(
        ProportionalElectionListEndResultRepo listEndResultRepo,
        ProportionalElectionCandidateEndResultBuilder candidateEndResultBuilder,
        DataContext dataContext,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepo)
    {
        _listEndResultRepo = listEndResultRepo;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _dataContext = dataContext;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
    }

    internal async Task Recalculate(
        Guid proportionalElectionListId,
        IEnumerable<ElectionEndResultLotDecision> lotDecisions)
    {
        var listEndResult = await _listEndResultRepo.GetByListIdAsTracked(proportionalElectionListId)
                            ?? throw new EntityNotFoundException(proportionalElectionListId);

        var simpleEndResult = await _simplePoliticalBusinessRepo.Query()
                .AsTracking()
                .FirstOrDefaultAsync(x => x.Id == listEndResult.ElectionEndResult.ProportionalElectionId)
            ?? throw new EntityNotFoundException(nameof(SimplePoliticalBusiness), listEndResult.ElectionEndResult.ProportionalElectionId);

        _candidateEndResultBuilder.UpdateCandidateEndResultRanksByLotDecisions(listEndResult, lotDecisions);

        if (!listEndResult.ElectionEndResult.ManualEndResultRequired)
        {
            _candidateEndResultBuilder.RecalculateCandidateEndResultStates(listEndResult);
        }

        _candidateEndResultBuilder.RecalculateLotDecisionRequired(listEndResult);
        listEndResult.ElectionEndResult.Finalized = false;
        simpleEndResult.EndResultFinalized = false;

        await _dataContext.SaveChangesAsync();
    }
}
