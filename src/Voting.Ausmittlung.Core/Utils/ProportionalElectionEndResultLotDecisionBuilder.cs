// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionEndResultLotDecisionBuilder
{
    private readonly ProportionalElectionListEndResultRepo _listEndResultRepo;
    private readonly ProportionalElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly DataContext _dataContext;

    public ProportionalElectionEndResultLotDecisionBuilder(
        ProportionalElectionListEndResultRepo listEndResultRepo,
        ProportionalElectionCandidateEndResultBuilder candidateEndResultBuilder,
        DataContext dataContext)
    {
        _listEndResultRepo = listEndResultRepo;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _dataContext = dataContext;
    }

    internal async Task Recalculate(
        Guid proportionalElectionListId,
        IEnumerable<ElectionEndResultLotDecision> lotDecisions)
    {
        var listEndResult = await _listEndResultRepo.GetByListIdAsTracked(proportionalElectionListId)
                            ?? throw new EntityNotFoundException(proportionalElectionListId);

        _candidateEndResultBuilder.UpdateCandidateEndResultRanksByLotDecisions(listEndResult, lotDecisions);
        _candidateEndResultBuilder.RecalculateCandidateEndResultStates(listEndResult);

        listEndResult.ElectionEndResult.Finalized = false;

        await _dataContext.SaveChangesAsync();
    }
}
