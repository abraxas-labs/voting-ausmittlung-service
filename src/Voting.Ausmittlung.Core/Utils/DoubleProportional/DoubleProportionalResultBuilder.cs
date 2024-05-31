// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

    public DoubleProportionalResultBuilder(
        DoubleProportionalResultRepo dpResultRepo,
        IDbRepository<DataContext, ProportionalElectionUnion> unionRepo,
        DataContext dataContext,
        ProportionalElectionRepo electionRepo,
        ProportionalElectionCandidateEndResultBuilder candidateEndResultBuilder,
        DoubleProportionalAlgorithm dpAlgorithm)
    {
        _dpResultRepo = dpResultRepo;
        _dataContext = dataContext;
        _electionRepo = electionRepo;
        _unionRepo = unionRepo;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _dpAlgorithm = dpAlgorithm;
    }

    internal async Task BuildForUnion(Guid unionId)
    {
        await ResetForUnion(unionId);

        var union = await LoadUnionAsTracking(unionId);

        _dpAlgorithm.BuildResultForUnion(union);
        var dpResult = union.DoubleProportionalResult!;

        // explicit create necessary, otherwise it will output an error because ef tries to save the child first, while the parent (fk) isnt created yet.
        await _dpResultRepo.Create(dpResult);

        foreach (var electionEndResult in union.ProportionalElectionUnionEntries.Select(e => e.ProportionalElection.EndResult!))
        {
            var electionDpResult = dpResult.Rows.FirstOrDefault(x => x.ProportionalElectionId == electionEndResult.ProportionalElectionId)
                ?? throw new InvalidOperationException("Dp result does not exist to a proportional election end result");

            UpdateUnionElectionEndResult(electionEndResult, electionDpResult, dpResult.AllNumberOfMandatesDistributed);
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
        var dpResult = await _dpResultRepo.GetElectionDoubleProportionalResultAsTracking(electionId)
            ?? throw new EntityNotFoundException(electionId);

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

        await _dataContext.SaveChangesAsync();
    }

    private void UpdateUnionElectionEndResult(ProportionalElectionEndResult electionEndResult, DoubleProportionalResultRow electionDpResultRow, bool allNumberOfMandatesDistributed)
    {
        if (!allNumberOfMandatesDistributed)
        {
            electionEndResult.Reset();
            return;
        }

        foreach (var listEndResult in electionEndResult.ListEndResults)
        {
            var dpResultCell = electionDpResultRow.Cells.FirstOrDefault(x => x.ListId == listEndResult.ListId)
                ?? throw new InvalidOperationException("Dp result cell does not exist for a proportional election list end result");

            listEndResult.NumberOfMandates = dpResultCell.SubApportionmentNumberOfMandates;

            _candidateEndResultBuilder.RecalculateCandidateEndResultRanks(listEndResult.CandidateEndResults, true);
            _candidateEndResultBuilder.RecalculateLotDecisionRequired(listEndResult);
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

        foreach (var listEndResult in electionEndResult.ListEndResults)
        {
            var dpResultColumn = dpResult.Columns.FirstOrDefault(x => x.ListId == listEndResult.ListId)
                ?? throw new InvalidOperationException("Dp result column does not exist for a proportional election list end result");

            listEndResult.NumberOfMandates = dpResultColumn.SuperApportionmentNumberOfMandates;

            _candidateEndResultBuilder.RecalculateCandidateEndResultRanks(listEndResult.CandidateEndResults, true);
            _candidateEndResultBuilder.RecalculateLotDecisionRequired(listEndResult);
        }

        _candidateEndResultBuilder.RecalculateCandidateEndResultStates(electionEndResult);
    }

    private async Task<ProportionalElectionUnion> LoadUnionAsTracking(Guid unionId)
    {
        return await _unionRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.Contest)
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
            .Include(x => x.Contest)
            .Include(x => x.EndResult!.ListEndResults)
            .ThenInclude(x => x.List)
            .Include(x => x.EndResult!.ListEndResults)
            .ThenInclude(x => x.CandidateEndResults)
            .FirstOrDefaultAsync(x => x.Id == electionId)
            ?? throw new EntityNotFoundException(nameof(ProportionalElection), electionId);
    }
}
