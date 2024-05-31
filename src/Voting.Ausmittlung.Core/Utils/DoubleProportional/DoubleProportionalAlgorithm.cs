// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.BiproportionalApportionment;
using Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils.DoubleProportional.Models;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Core.Utils.DoubleProportional;

public class DoubleProportionalAlgorithm
{
    private readonly ILogger<DoubleProportionalAlgorithm> _logger;

    public DoubleProportionalAlgorithm(ILogger<DoubleProportionalAlgorithm> logger)
    {
        _logger = logger;
    }

    public void BuildResultForUnion(ProportionalElectionUnion union)
    {
        var dpResult = BuildInitialDoubleProportionalResultForUnion(union);
        union.DoubleProportionalResult = dpResult;

        if (!dpResult.MandateAlgorithm.IsUnionDoubleProportional())
        {
            return;
        }

        CalculateQuorum(dpResult);
        CalculateSuperApportionment(dpResult);

        if (dpResult.SuperApportionmentState != DoubleProportionalResultApportionmentState.Completed)
        {
            return;
        }

        CalculateSubApportionment(dpResult);
    }

    public void BuildResultForElection(ProportionalElection election)
    {
        var dpResult = BuildInitialDoubleProportionalResultForElection(election);
        election.DoubleProportionalResult = dpResult;

        if (!dpResult.MandateAlgorithm.IsNonUnionDoubleProportional())
        {
            return;
        }

        CalculateQuorum(dpResult);
        CalculateSuperApportionment(dpResult);
    }

    public void SetSuperApportionmentLotDecision(DoubleProportionalResult dpResult, DoubleProportionalResultSuperApportionmentLotDecision lotDecision)
    {
        var lotDecisionNumberOfMandatesByListOrUnionListId = lotDecision.Columns.ToDictionary(c => c.UnionListId ?? c.ListId!.Value, c => c.NumberOfMandates);

        foreach (var column in dpResult.Columns.Where(c => c.SuperApportionmentLotDecisionRequired))
        {
            var listOrUnionListId = column.UnionListId ?? column.ListId!.Value;
            if (!lotDecisionNumberOfMandatesByListOrUnionListId.TryGetValue(listOrUnionListId, out var lotDecisionNumberOfMandates))
            {
                _logger.LogError("Lot decision column for {ListOrUnionListId}", listOrUnionListId);
                continue;
            }

            column.SuperApportionmentNumberOfMandatesFromLotDecision = lotDecisionNumberOfMandates - column.SuperApportionmentNumberOfMandatesExclLotDecision;
            column.SubApportionmentNumberOfMandates = 0;

            foreach (var cell in column.Cells)
            {
                cell.SubApportionmentNumberOfMandatesExclLotDecision = 0;
                cell.SubApportionmentNumberOfMandatesFromLotDecision = 0;
            }
        }

        dpResult.SuperApportionmentNumberOfMandates = dpResult.Columns.Sum(c => c.SuperApportionmentNumberOfMandates);
        dpResult.SuperApportionmentState = dpResult.NumberOfMandates == dpResult.SuperApportionmentNumberOfMandates
            ? DoubleProportionalResultApportionmentState.Completed
            : DoubleProportionalResultApportionmentState.Error;

        if (dpResult.ProportionalElectionId != null || dpResult.SuperApportionmentState != DoubleProportionalResultApportionmentState.Completed)
        {
            return;
        }

        CalculateSubApportionment(dpResult);
    }

    public void SetSubApportionmentLotDecision(DoubleProportionalResult dpResult, DoubleProportionalResultSubApportionmentLotDecision lotDecision)
    {
        var lotDecisionNumberOfMandateByListId = lotDecision.Columns.SelectMany(co => co.Cells).ToDictionary(c => c.ListId, c => c.NumberOfMandates);

        foreach (var column in dpResult.Columns.Where(c => c.SubApportionmentInitialNegativeTies > 0))
        {
            foreach (var cell in column.Cells)
            {
                if (!lotDecisionNumberOfMandateByListId.TryGetValue(cell.ListId, out var lotDecisionNumberOfMandates))
                {
                    _logger.LogError("Lot decision cell for list {ListId} not found", cell.ListId);
                    continue;
                }

                cell.SubApportionmentNumberOfMandatesFromLotDecision = lotDecisionNumberOfMandates - cell.SubApportionmentNumberOfMandatesExclLotDecision;
            }

            column.SubApportionmentNumberOfMandates = column.Cells.Sum(ce => ce.SubApportionmentNumberOfMandates);
        }

        foreach (var row in dpResult.Rows)
        {
            row.SubApportionmentNumberOfMandates = row.Cells.Sum(ce => ce.SubApportionmentNumberOfMandates);
        }

        dpResult.SubApportionmentNumberOfMandates = dpResult.Columns.Sum(c => c.SubApportionmentNumberOfMandates);
        dpResult.SubApportionmentState = dpResult.NumberOfMandates == dpResult.SubApportionmentNumberOfMandates
            ? DoubleProportionalResultApportionmentState.Completed
            : DoubleProportionalResultApportionmentState.Error;
    }

    /// <summary>
    /// Step 1 of the double proportional method.
    /// </summary>
    /// <param name="dpResult">The double proportional result.</param>
    private void CalculateQuorum(DoubleProportionalResult dpResult)
    {
        var proportionalElectionQuorumMultiplier = GetProportionalElectionQuorumMultiplier(dpResult.MandateAlgorithm);
        var cantonalQuorumMultiplier = GetCantonalQuorumMultiplier(dpResult.MandateAlgorithm);

        // Calculate the election quorum, and check whether any list fulfills the quorum.
        foreach (var row in dpResult.Rows)
        {
            row.Quorum = (int)Math.Ceiling(row.VoteCount * proportionalElectionQuorumMultiplier);

            foreach (var cell in row.Cells)
            {
                cell.ProportionalElectionQuorumReached = row.Quorum <= cell.VoteCount;
            }
        }

        // Calculate the cantonal quorum
        dpResult.CantonalQuorum = (int)Math.Ceiling(dpResult.VoteCount * cantonalQuorumMultiplier);

        // Check whether any list group fulfills the quorums.
        foreach (var column in dpResult.Columns)
        {
            column.CantonalQuorumReached = dpResult.CantonalQuorum <= column.VoteCount;
            column.AnyRequiredQuorumReached = (cantonalQuorumMultiplier > 0 && column.CantonalQuorumReached)
                || column.Cells.Any(ledp => ledp.ProportionalElectionQuorumReached);
        }
    }

    /// <summary>
    /// Step 2 of the double proportional calculation ("Oberzuteilung").
    /// </summary>
    /// <param name="dpResult">The result.</param>
    private void CalculateSuperApportionment(DoubleProportionalResult dpResult)
    {
        // Only columns (union lists) which have passed the "Quorum"
        var columnsWithQuorumReached = dpResult.Columns
            .Where(x => x.AnyRequiredQuorumReached)
            .ToList();

        if (columnsWithQuorumReached.Count < 2)
        {
            _logger.LogError("Double proportional super apportionment does not work if only 0 or 1 list passed the quorum");
            return;
        }

        // Calculate "Wählerzahl" on each list, union list and the whole election union.
        foreach (var cell in columnsWithQuorumReached.SelectMany(x => x.Cells).ToList())
        {
            cell.VoterNumber = (decimal)cell.VoteCount / cell.Row.NumberOfMandates;
        }

        foreach (var column in columnsWithQuorumReached)
        {
            column.VoterNumber = column.Cells.Sum(x => x.VoterNumber);
        }

        dpResult.VoterNumber = columnsWithQuorumReached.Sum(x => x.VoterNumber);

        foreach (var row in dpResult.Rows)
        {
            row.VoterNumber = row.Cells.Sum(c => c.VoterNumber);
        }

        var saintLagueAlgorithm = new SaintLagueAlgorithm();
        SaintLagueAlgorithmResult? saintLagueResult = null;

        try
        {
            saintLagueResult = saintLagueAlgorithm.Calculate(
                columnsWithQuorumReached.Select(c => c.VoterNumber).ToArray(),
                dpResult.NumberOfMandates);
        }
        catch (DoubleProportionalAlgorithmException ex)
        {
            _logger.LogError(ex, "Could not calculate the sub apportionment");
        }

        if (saintLagueResult == null || saintLagueResult.ElectionKey == 0)
        {
            dpResult.SuperApportionmentState = DoubleProportionalResultApportionmentState.Error;
            return;
        }

        for (var x = 0; x < saintLagueResult.Quotients.Length; x++)
        {
            var column = columnsWithQuorumReached[x];
            column.SuperApportionmentQuotient = saintLagueResult.Quotients[x];
            column.SuperApportionmentNumberOfMandatesExclLotDecision = saintLagueResult.Apportionment[x];

            var tieState = saintLagueResult.TieStates[x];
            if (tieState == TieState.Unique)
            {
                continue;
            }

            column.SuperApportionmentLotDecisionRequired = true;

            if (tieState == TieState.Negative)
            {
                // The negative tie state indicates that the number of mandates was rounded up because it was exactly an integer + 0.5,
                // but it could not update divisors without breaking the overall super apportionment.
                column.SuperApportionmentNumberOfMandatesExclLotDecision--;
            }
        }

        dpResult.ElectionKey = saintLagueResult.ElectionKey;
        dpResult.SuperApportionmentNumberOfMandatesForLotDecision = saintLagueResult.CountOfMissingNumberOfMandates;
        dpResult.HasSuperApportionmentRequiredLotDecision = saintLagueResult.HasTies;

        dpResult.SuperApportionmentNumberOfMandates = columnsWithQuorumReached.Sum(c => c.SuperApportionmentNumberOfMandatesExclLotDecision);
        dpResult.SuperApportionmentState = !saintLagueResult.HasTies
            ? DoubleProportionalResultApportionmentState.Completed
            : DoubleProportionalResultApportionmentState.HasOpenLotDecision;
    }

    private void CalculateSubApportionment(DoubleProportionalResult dpResult)
    {
        // Only union lists which have received seats.
        var columnsWithSeats = dpResult.Columns
            .Where(x => x.SuperApportionmentNumberOfMandates > 0)
            .ToList();

        // Create a "Weight" matrix (= dimension of the double proportional result matrix) for a biproportional apportionment method.
        var rows = dpResult.Rows.ToList();
        var weights = new Weight[rows.Count][];

        for (var i = 0; i < rows.Count; i++)
        {
            weights[i] = new Weight[columnsWithSeats.Count];
            var cells = rows[i].Cells.ToList();

            for (var j = 0; j < columnsWithSeats.Count; j++)
            {
                var column = columnsWithSeats[j];
                var cell = column.Cells.FirstOrDefault(cells.Contains);
                weights[i][j] = new Weight
                {
                    VoteCount = cell?.VoteCount ?? 0,
                };
            }
        }

        // Calculate the sub apportionment with a biproportional apportionment method.
        var bipropAlgo = new TieAndTransferApportionmentMethod();
        var bipropData = new BiproportionalApportionmentData(
            weights,
            rows.Select(x => x.NumberOfMandates).ToArray(),
            columnsWithSeats.Select(x => x.SuperApportionmentNumberOfMandates).ToArray());

        BiproportionalApportionmentResult? bipropResult = null;

        try
        {
            bipropResult = bipropAlgo.Calculate(bipropData);
        }
        catch (BiproportionalApportionmentException ex)
        {
            _logger.LogError(ex, "Could not calculate the sub apportionment");
        }

        if (bipropResult == null)
        {
            dpResult.SubApportionmentState = DoubleProportionalResultApportionmentState.Error;
            return;
        }

        // Set the sub apportionment number of mandates on each list, and the election and union list dividers.
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Divisor = bipropResult.RowDivisors[i];
            var listDpResults = rows[i].Cells.ToList();

            for (var j = 0; j < columnsWithSeats.Count; j++)
            {
                var column = columnsWithSeats[j];
                var cell = column.Cells.FirstOrDefault(listDpResults.Contains);

                if (cell == null)
                {
                    continue;
                }

                cell.SubApportionmentNumberOfMandatesExclLotDecision = bipropResult.Apportionment[i][j];

                var tieState = bipropResult.Ties[i][j];
                if (tieState == TieState.Unique)
                {
                    continue;
                }

                cell.SubApportionmentLotDecisionRequired = true;

                if (tieState == TieState.Negative)
                {
                    // the negative tie state indicates that the number of mandates was rounded up because it was very very close to a integer + 0.5,
                    // but it could not update divisors without breaking the overall sub apportionment (cyclic path in the BfsRowColumnGraph).
                    cell.SubApportionmentNumberOfMandatesExclLotDecision--;
                    column.SubApportionmentInitialNegativeTies++;
                }
            }

            rows[i].SubApportionmentNumberOfMandates = listDpResults.Sum(x => x.SubApportionmentNumberOfMandates);
        }

        for (var j = 0; j < columnsWithSeats.Count; j++)
        {
            columnsWithSeats[j].Divisor = bipropResult.ColumnDivisors[j];
            columnsWithSeats[j].SubApportionmentNumberOfMandates = columnsWithSeats[j].Cells.Sum(x => x.SubApportionmentNumberOfMandates);
        }

        dpResult.HasSubApportionmentRequiredLotDecision = bipropResult.HasTies;

        dpResult.SubApportionmentNumberOfMandates = dpResult.Columns.Sum(x => x.SubApportionmentNumberOfMandates);
        dpResult.SubApportionmentState = !bipropResult.HasTies
            ? DoubleProportionalResultApportionmentState.Completed
            : DoubleProportionalResultApportionmentState.HasOpenLotDecision;
    }

    private decimal GetProportionalElectionQuorumMultiplier(ProportionalElectionMandateAlgorithm mandateAlgorithm)
    {
        return mandateAlgorithm switch
        {
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum => 0.05M,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum => 0.05M,
            ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum => 0,
            _ => throw new ArgumentException($"Invalid mandate algorithm {mandateAlgorithm}"),
        };
    }

    private decimal GetCantonalQuorumMultiplier(ProportionalElectionMandateAlgorithm mandateAlgorithm)
    {
        return mandateAlgorithm switch
        {
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum => 0.03M,
            ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum => 0,
            ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum => 0,
            _ => throw new ArgumentException($"Invalid mandate algorithm {mandateAlgorithm}"),
        };
    }

    private DoubleProportionalResult BuildInitialDoubleProportionalResultForUnion(ProportionalElectionUnion union)
    {
        var mandateAlgorithm = union.ProportionalElectionUnionEntries.Select(e => e.ProportionalElection).FirstOrDefault()?.MandateAlgorithm
            ?? ProportionalElectionMandateAlgorithm.Unspecified;

        EnsureValidMandateAlgorithm(mandateAlgorithm);

        // The order is relevant (especially for the sub apportionment, the end result of the sub apportionment does not change but it could result in non-determistic dividers)
        union.ProportionalElectionUnionLists = union.ProportionalElectionUnionLists.OrderBy(x => x.OrderNumber).ToList();
        foreach (var unionList in union.ProportionalElectionUnionLists)
        {
            unionList.ProportionalElectionUnionListEntries = unionList.ProportionalElectionUnionListEntries
                .OrderBy(x => x.ProportionalElectionList.ProportionalElection.PoliticalBusinessNumber)
                .ToList();
        }

        union.ProportionalElectionUnionEntries = union.ProportionalElectionUnionEntries
            .OrderBy(x => x.ProportionalElection.PoliticalBusinessNumber)
            .ToList();

        var electionEndResults = union.ProportionalElectionUnionEntries
            .Select(e => e.ProportionalElection.EndResult!)
            .ToList();

        foreach (var electionEndResult in electionEndResults)
        {
            electionEndResult.ListEndResults = electionEndResult.ListEndResults.OrderBy(x => x.List.OrderNumber).ToList();
        }

        // Build the initial double proportional result
        var unionDpResult = new DoubleProportionalResult
        {
            Id = AusmittlungUuidV5.BuildDoubleProportionalResult(union.Id, null, union.Contest.TestingPhaseEnded),
            ProportionalElectionUnionId = union.Id,
            MandateAlgorithm = union.ProportionalElectionUnionEntries.Select(e => e.ProportionalElection).FirstOrDefault()!.MandateAlgorithm,
        };

        foreach (var electionEndResult in electionEndResults)
        {
            var electionDpResult = new DoubleProportionalResultRow
            {
                Result = unionDpResult,
                ProportionalElectionId = electionEndResult.ProportionalElectionId,
                VoteCount = electionEndResult.ListEndResults.Sum(x => x.TotalVoteCount),
                NumberOfMandates = electionEndResult.ProportionalElection.NumberOfMandates,
            };

            electionDpResult.Cells = electionEndResult!.ListEndResults
                .OrderBy(listEndResult => listEndResult.List.OrderNumber)
                .Select(listEndResult => new DoubleProportionalResultCell
                {
                    VoteCount = listEndResult.TotalVoteCount,
                    Row = electionDpResult,
                    ListId = listEndResult.ListId,
                })
                .ToList();

            unionDpResult.Rows.Add(electionDpResult);
        }

        foreach (var unionList in union.ProportionalElectionUnionLists)
        {
            var listIds = unionList
                .ProportionalElectionUnionListEntries
                .Select(x => x.ProportionalElectionListId)
                .ToList();

            var listDpResults = unionDpResult.Rows.SelectMany(x => x.Cells)
                .Where(x => listIds.Contains(x.ListId))
                .ToList();

            unionList.DoubleProportionalResultColumn = new DoubleProportionalResultColumn
            {
                UnionListId = unionList.Id,
                Result = unionDpResult,
                VoteCount = listDpResults.Sum(x => x.VoteCount),
                Cells = listDpResults,
            };
        }

        unionDpResult.Columns = union.ProportionalElectionUnionLists.Select(x => x.DoubleProportionalResultColumn!).ToList();
        unionDpResult.VoteCount = unionDpResult.Rows.Sum(x => x.VoteCount);
        unionDpResult.NumberOfMandates = unionDpResult.Rows.Sum(x => x.NumberOfMandates);

        var totalVoteCountByUnionLists = unionDpResult.Columns.Sum(x => x.VoteCount);
        if (unionDpResult.VoteCount != totalVoteCountByUnionLists)
        {
            throw new ValidationException($"Different vote counts: By elections {unionDpResult.VoteCount}, by union lists {totalVoteCountByUnionLists}");
        }

        return unionDpResult;
    }

    private DoubleProportionalResult BuildInitialDoubleProportionalResultForElection(ProportionalElection election)
    {
        EnsureValidMandateAlgorithm(election.MandateAlgorithm);

        var electionEndResult = election.EndResult!;
        electionEndResult.ListEndResults = electionEndResult.ListEndResults.OrderBy(x => x.List.OrderNumber).ToList();

        var dpResult = new DoubleProportionalResult
        {
            Id = AusmittlungUuidV5.BuildDoubleProportionalResult(null, election.Id, election.Contest.TestingPhaseEnded),
            ProportionalElectionId = election.Id,
            NumberOfMandates = election.NumberOfMandates,
            MandateAlgorithm = election.MandateAlgorithm,
            SubApportionmentState = DoubleProportionalResultApportionmentState.Unspecified,
        };

        var row = new DoubleProportionalResultRow
        {
            ProportionalElectionId = election.Id,
            NumberOfMandates = election.NumberOfMandates,
            VoteCount = electionEndResult.ListEndResults.Sum(x => x.TotalVoteCount),
        };

        dpResult.Rows.Add(row);

        foreach (var listEndresult in electionEndResult.ListEndResults)
        {
            var column = new DoubleProportionalResultColumn
            {
                ListId = listEndresult.ListId,
                Result = dpResult,
                VoteCount = listEndresult.TotalVoteCount,
            };

            var cell = new DoubleProportionalResultCell
            {
                ListId = column.ListId.Value,
                Row = row,
                Column = column,
                VoteCount = column.VoteCount,
            };

            column.Cells.Add(cell);
            dpResult.Columns.Add(column);
        }

        row.Cells = dpResult.Columns.SelectMany(c => c.Cells).ToList();
        dpResult.VoteCount = dpResult.Rows.Sum(x => x.VoteCount);
        return dpResult;
    }

    private void EnsureValidMandateAlgorithm(ProportionalElectionMandateAlgorithm mandateAlgorithm)
    {
        if (!mandateAlgorithm.IsDoubleProportional())
        {
            throw new ArgumentException($"Cannot build a double proportional result with mandate algorithm {mandateAlgorithm}");
        }
    }
}
