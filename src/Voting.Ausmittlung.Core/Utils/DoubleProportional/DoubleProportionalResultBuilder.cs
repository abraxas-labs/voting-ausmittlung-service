// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils.DoubleProportional;

public class DoubleProportionalResultBuilder
{
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _unionRepo;
    private readonly DoubleProportionalResultRepo _dpResultRepo;
    private readonly DataContext _dataContext;
    private readonly ProportionalElectionRepo _electionRepo;
    private readonly ProportionalElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly DoubleProportionalAlgorithm _dpAlgorithm;
    private readonly ILogger<DoubleProportionalResultBuilder> _logger;
    private readonly SimplePoliticalBusinessRepo _simplePoliticalBusinessRepo;

    public DoubleProportionalResultBuilder(
        DoubleProportionalResultRepo dpResultRepo,
        IDbRepository<DataContext, ProportionalElectionUnion> unionRepo,
        DataContext dataContext,
        ProportionalElectionRepo electionRepo,
        ProportionalElectionCandidateEndResultBuilder candidateEndResultBuilder,
        DoubleProportionalAlgorithm dpAlgorithm,
        ILogger<DoubleProportionalResultBuilder> logger,
        SimplePoliticalBusinessRepo simplePoliticalBusinessRepo)
    {
        _dpResultRepo = dpResultRepo;
        _dataContext = dataContext;
        _electionRepo = electionRepo;
        _unionRepo = unionRepo;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _dpAlgorithm = dpAlgorithm;
        _logger = logger;
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
    }

    internal async Task BuildForUnion(Guid unionId)
    {
        await ResetForUnion(unionId);

        var union = await LoadUnionAsTracking(unionId);

        var electionIds = union.ProportionalElectionUnionEntries
            .Select(e => e.ProportionalElectionId)
            .ToList();

        var simplePbById = await _simplePoliticalBusinessRepo
            .Query()
            .AsSplitQuery()
            .AsTracking()
            .Where(x => electionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x);

        _dpAlgorithm.BuildResultForUnion(union);
        var dpResult = union.DoubleProportionalResult!;

        // explicit create necessary, otherwise it will output an error because ef tries to save the child first, while the parent (fk) isnt created yet.
        await _dpResultRepo.Create(dpResult);

        foreach (var electionEndResult in union.ProportionalElectionUnionEntries.Select(e => e.ProportionalElection.EndResult!))
        {
            var electionDpResult = dpResult.Rows.FirstOrDefault(x => x.ProportionalElectionId == electionEndResult.ProportionalElectionId)
                ?? throw new InvalidOperationException("Dp result does not exist to a proportional election end result");

            UpdateUnionElectionEndResult(electionEndResult, electionDpResult, dpResult.AllNumberOfMandatesDistributed);

            var implicitFinalize = union.Contest.CantonDefaults.EndResultFinalizeDisabled;
            electionEndResult.Finalized = implicitFinalize;
            simplePbById[electionEndResult.ProportionalElectionId].EndResultFinalized = implicitFinalize;
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task BuildForElection(Guid electionId)
    {
        await ResetForElection(electionId);

        var election = await LoadElectionAsTracking(electionId);

        _dpAlgorithm.BuildResultForElection(election);
        var dpResult = election.DoubleProportionalResult!;

        // explicit create necessary, otherwise it will output an error because ef tries to save the child first, while the parent (fk) isnt created yet.
        await _dpResultRepo.Create(dpResult);

        UpdateElectionEndResult(election.EndResult!, dpResult);
        await _dataContext.SaveChangesAsync();
    }

    internal async Task SetSuperApportionmentLotDecisionForUnion(Guid unionId, DoubleProportionalResultSuperApportionmentLotDecision lotDecision)
    {
        var dpResult = await _dpResultRepo.GetUnionDoubleProportionalResultAsTracking(unionId)
            ?? throw new EntityNotFoundException(unionId);

        _dpAlgorithm.SetSuperApportionmentLotDecision(dpResult, lotDecision);

        var union = await LoadUnionAsTracking(unionId);
        foreach (var electionEndResult in union.ProportionalElectionUnionEntries.Select(e => e.ProportionalElection.EndResult!))
        {
            var electionDpResult = dpResult.Rows.FirstOrDefault(x => x.ProportionalElectionId == electionEndResult.ProportionalElectionId)
                ?? throw new InvalidOperationException("Dp result does not exist to a proportional election end result");

            UpdateUnionElectionEndResult(electionEndResult, electionDpResult, dpResult.AllNumberOfMandatesDistributed);
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task SetSuperApportionmentLotDecisionForElection(Guid electionId, DoubleProportionalResultSuperApportionmentLotDecision lotDecision)
    {
        var dpResult = await _dpResultRepo.GetElectionDoubleProportionalResultAsTracking(electionId);

        // Is possible historically, because audited tentatively triggered the calculation before using the ImplicitMandateDistributionDisabled flag.
        if (dpResult == null)
        {
            _logger.LogWarning("Cannot calculate super apportionment for election {ElectionId}, because the dp result is missing", electionId);
            return;
        }

        _dpAlgorithm.SetSuperApportionmentLotDecision(dpResult, lotDecision);

        var election = await LoadElectionAsTracking(electionId);
        UpdateElectionEndResult(election.EndResult!, dpResult);

        await _dataContext.SaveChangesAsync();
    }

    internal async Task SetSubApportionmentLotDecisionForUnion(Guid unionId, DoubleProportionalResultSubApportionmentLotDecision lotDecision)
    {
        var dpResult = await _dpResultRepo.GetUnionDoubleProportionalResultAsTracking(unionId)
            ?? throw new EntityNotFoundException(unionId);

        _dpAlgorithm.SetSubApportionmentLotDecision(dpResult, lotDecision);

        var union = await LoadUnionAsTracking(unionId);
        foreach (var electionEndResult in union.ProportionalElectionUnionEntries.Select(e => e.ProportionalElection.EndResult!))
        {
            var electionDpResult = dpResult.Rows.FirstOrDefault(x => x.ProportionalElectionId == electionEndResult.ProportionalElectionId)
                ?? throw new InvalidOperationException("Dp result does not exist to a proportional election end result");

            UpdateUnionElectionEndResult(electionEndResult, electionDpResult, dpResult.AllNumberOfMandatesDistributed);
        }

        await _dataContext.SaveChangesAsync();
    }

    internal async Task ResetForElection(Guid electionId)
    {
        var election = await _dataContext.ProportionalElections
            .Include(pe => pe.DoubleProportionalResult)
            .Include(pe => pe.ProportionalElectionUnionEntries)
            .WhereIsDoubleProportional()
            .FirstOrDefaultAsync(pe => pe.Id == electionId)
            ?? throw new EntityNotFoundException(electionId);

        if (election.MandateAlgorithm.IsUnionDoubleProportional())
        {
            await ResetForUnion(election.ProportionalElectionUnionEntries.Select(e => e.ProportionalElectionUnionId).ToList());
        }

        if (election.DoubleProportionalResult != null)
        {
            await _dpResultRepo.DeleteByKey(election.DoubleProportionalResult.Id);
        }
    }

    internal async Task ResetForContest(Guid contestId)
    {
        var elections = await _dataContext.ProportionalElections
            .Include(pe => pe.ProportionalElectionUnionEntries)
            .WhereIsDoubleProportional()
            .Where(pe => pe.ContestId == contestId)
            .ToListAsync();

        var unionIds = elections
            .SelectMany(x => x.ProportionalElectionUnionEntries)
            .Select(e => e.ProportionalElectionUnionId)
            .ToHashSet();

        await ResetForUnion(unionIds);
    }

    internal Task ResetForUnion(Guid unionId)
    {
        return ResetForUnion(new[] { unionId });
    }

    internal async Task DeleteDpResultsForUnion(Guid proportionalElectionUnionId)
    {
        var dpResultIds = await _dpResultRepo.Query()
            .Where(x => x.ProportionalElectionUnionId == proportionalElectionUnionId)
            .Select(x => x.Id)
            .ToListAsync();

        await _dpResultRepo.DeleteRangeByKey(dpResultIds);
    }

    private async Task ResetForUnion(IReadOnlyCollection<Guid> unionIds)
    {
        var unions = await _unionRepo.Query()
            .AsSplitQuery()
            .AsTracking()
            .Include(u => u.DoubleProportionalResult)
            .Include(u => u.EndResult)
            .Include(u => u.ProportionalElectionUnionEntries)
                .ThenInclude(e => e.ProportionalElection)
                    .ThenInclude(pe => pe.EndResult!)
                        .ThenInclude(e => e.ListEndResults)
                            .ThenInclude(e => e.CandidateEndResults)
            .Where(u => unionIds.Contains(u.Id))
            .ToListAsync();

        var electionIds = unions
            .SelectMany(u => u.ProportionalElectionUnionEntries)
            .Select(e => e.ProportionalElectionId)
            .ToHashSet();

        var simplePbs = await _simplePoliticalBusinessRepo
            .Query()
            .AsSplitQuery()
            .AsTracking()
            .Where(x => electionIds.Contains(x.Id))
            .ToListAsync();

        foreach (var union in unions)
        {
            if (union.DoubleProportionalResult != null)
            {
                _dataContext.DoubleProportionalResults.Remove(union.DoubleProportionalResult);
            }

            union.EndResult!.Finalized = false;

            foreach (var electionEndResult in union.ProportionalElectionUnionEntries.Select(e => e.ProportionalElection.EndResult!))
            {
                electionEndResult.Reset();
            }
        }

        foreach (var simplePb in simplePbs)
        {
            simplePb.EndResultFinalized = false;
        }

        await _dataContext.SaveChangesAsync();
    }

    private void UpdateUnionElectionEndResult(ProportionalElectionEndResult electionEndResult, DoubleProportionalResultRow electionDpResultRow, bool allNumberOfMandatesDistributed)
    {
        if (!allNumberOfMandatesDistributed)
        {
            electionEndResult.Reset();
            return;
        }

        electionEndResult.MandateDistributionTriggered = true;

        foreach (var listEndResult in electionEndResult.ListEndResults)
        {
            var dpResultCell = electionDpResultRow.Cells.FirstOrDefault(x => x.ListId == listEndResult.ListId)
                ?? throw new InvalidOperationException("Dp result cell does not exist for a proportional election list end result");

            listEndResult.NumberOfMandates = dpResultCell.SubApportionmentNumberOfMandates;

            _candidateEndResultBuilder.RecalculateCandidateEndResultRanks(listEndResult.CandidateEndResults, true);
            _candidateEndResultBuilder.RecalculateLotDecisionState(listEndResult);
        }

        _candidateEndResultBuilder.RecalculateCandidateEndResultStates(electionEndResult);
    }

    private void UpdateElectionEndResult(ProportionalElectionEndResult electionEndResult, DoubleProportionalResult dpResult)
    {
        if (!dpResult.AllNumberOfMandatesDistributed)
        {
            electionEndResult.Reset();
            return;
        }

        electionEndResult.MandateDistributionTriggered = true;

        foreach (var listEndResult in electionEndResult.ListEndResults)
        {
            var dpResultColumn = dpResult.Columns.FirstOrDefault(x => x.ListId == listEndResult.ListId)
                ?? throw new InvalidOperationException("Dp result column does not exist for a proportional election list end result");

            listEndResult.NumberOfMandates = dpResultColumn.SuperApportionmentNumberOfMandates;

            _candidateEndResultBuilder.RecalculateCandidateEndResultRanks(listEndResult.CandidateEndResults, true);
            _candidateEndResultBuilder.RecalculateLotDecisionState(listEndResult);
        }

        _candidateEndResultBuilder.RecalculateCandidateEndResultStates(electionEndResult);
    }

    private async Task<ProportionalElectionUnion> LoadUnionAsTracking(Guid unionId)
    {
        return await _unionRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.Contest.CantonDefaults)
            .Include(x => x.EndResult)
            .Include(x => x.ProportionalElectionUnionEntries)
                .ThenInclude(x => x.ProportionalElection.EndResult!.ListEndResults)
                    .ThenInclude(x => x.List)
             .Include(x => x.ProportionalElectionUnionEntries)
                .ThenInclude(x => x.ProportionalElection.EndResult!.ListEndResults)
                    .ThenInclude(x => x.CandidateEndResults)
            .Include(x => x.ProportionalElectionUnionLists)
                .ThenInclude(x => x.ProportionalElectionUnionListEntries)
                    .ThenInclude(x => x.ProportionalElectionList.EndResult)
            .Include(x => x.ProportionalElectionUnionLists)
                .ThenInclude(x => x.ProportionalElectionUnionListEntries)
                    .ThenInclude(x => x.ProportionalElectionList.ProportionalElection)
            .FirstOrDefaultAsync(x => x.Id == unionId)
            ?? throw new EntityNotFoundException(nameof(ProportionalElectionUnion), unionId);
    }

    private async Task<ProportionalElection> LoadElectionAsTracking(Guid electionId)
    {
        return await _electionRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.Contest.CantonDefaults)
            .Include(x => x.EndResult!.ListEndResults)
            .ThenInclude(x => x.List)
            .Include(x => x.EndResult!.ListEndResults)
            .ThenInclude(x => x.CandidateEndResults)
            .FirstOrDefaultAsync(x => x.Id == electionId)
            ?? throw new EntityNotFoundException(nameof(ProportionalElection), electionId);
    }
}
