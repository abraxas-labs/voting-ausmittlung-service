// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Rationals;

namespace Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;

/// <summary>
/// The tie and transfer apportionment method with integer arithmetic (Martin Zachariasen, 2006).
/// The algorithm is described in the paper "Algorithic Aspects of Divisor-Based Biproportional Rounding" from Martin Zachariasen (2006)
/// and in the source code of BAZI.
/// </summary>
public class TieAndTransferApportionmentMethod : IBiproportionalApportionmentMethod
{
    private readonly VectorApportionmentMethod _vectorApportionmentMethod = new();

    /// <inheritdoc />
    public BiproportionalApportionmentResult Calculate(BiproportionalApportionmentData apportionmentData)
    {
        EnsureValidInput(apportionmentData);

        var context = new TieAndTransferMethodContext(apportionmentData.WeightMatrix, apportionmentData.RowApportionment, apportionmentData.ColumnApportionment);
        CalculateApportionment(context);
        CalculateTies(context);

        return new BiproportionalApportionmentResult(
            context.ColDivisors,
            context.RowDivisors,
            context.NumberOfTransfers,
            context.NumberOfUpdates,
            context.Ties,
            context.Apportionment);
    }

    private void EnsureValidInput(BiproportionalApportionmentData apportionmentData)
    {
        if (apportionmentData.RowApportionment.Sum() != apportionmentData.ColumnApportionment.Sum())
        {
            throw new BiproportionalApportionmentException("Rows and cols need the same sum of number of mandates");
        }

        if (apportionmentData.WeightMatrix.Length == 0)
        {
            throw new BiproportionalApportionmentException("Weight matrix has no data");
        }

        if (apportionmentData.WeightMatrix.Length != apportionmentData.RowApportionment.Length
            || apportionmentData.WeightMatrix[0].Length != apportionmentData.ColumnApportionment.Length)
        {
            throw new BiproportionalApportionmentException("The dimensions of the weight matrix does not match the row or column apportionment");
        }
    }

    private void CalculateApportionment(TieAndTransferMethodContext ctx)
    {
        for (var i = 0; i < ctx.Rows; i++)
        {
            ctx.RowDivisors[i] = 1;
        }

        CalculateColumnDivisors(ctx);
        TieAndTransfer(ctx);
        CheckForTiesAndUpdateDivisors(ctx);
        ScaleDivisors(ctx);
    }

    /// <summary>
    /// Calculate the initial apportionment, by calculating the column (union list) divisors, so that the number of mandates gets distributed correctly per union list,
    /// to reduce the 2D-problem to a 1D-problem.
    /// </summary>
    /// <param name="ctx">The TT-Method Context.</param>
    private void CalculateColumnDivisors(TieAndTransferMethodContext ctx)
    {
        for (var j = 0; j < ctx.Cols; j++)
        {
            var colWeights = new Weight[ctx.Rows];

            for (var i = 0; i < ctx.Rows; i++)
            {
                colWeights[i] = ctx.Weights[i][j];
            }

            var dmResult = _vectorApportionmentMethod.Calculate(colWeights, ctx.ColumnApportionment[j]);
            ctx.ColDivisors[j] = dmResult.MaxDivisor;

            for (var i = 0; i < ctx.Rows; i++)
            {
                ctx.Apportionment[i][j] = dmResult.Apportionment[i];
            }
        }
    }

    /// <summary>
    /// Tie and transfer, by bringing iteratively the error of the apportionment down to 0.
    /// </summary>
    /// <param name="ctx">The TT-Method Context.</param>
    private void TieAndTransfer(TieAndTransferMethodContext ctx)
    {
        var labeled = new bool[ctx.Rows + ctx.Cols];
        var predecessor = new int[ctx.Rows + ctx.Cols];

        while (true)
        {
            var rowApportionmentError = new int[ctx.Rows];
            var totalError = 0;

            for (var i = 0; i < ctx.Rows + ctx.Cols; i++)
            {
                labeled[i] = false;
                predecessor[i] = -1;
            }

            // Calculate the total error (sum of row errors).
            for (var i = 0; i < ctx.Rows; i++)
            {
                var currentSum = 0;
                for (var j = 0; j < ctx.Cols; j++)
                {
                    currentSum += ctx.Apportionment[i][j];
                }

                rowApportionmentError[i] = currentSum - ctx.RowApportionment[i];
                totalError += Math.Abs(rowApportionmentError[i]);

                // Add to queue if negative error
                if (rowApportionmentError[i] < 0)
                {
                    labeled[i] = true;
                }
            }

            // If the total error is 0, the problem is solved
            if (totalError == 0)
            {
                break;
            }

            var positiveRowLabeled = false;
            var rowColIndex = 0;

            while (true)
            {
                BfsRowColumnGraph(ctx, labeled, predecessor);

                for (var i = 0; i < ctx.Rows; i++)
                {
                    if (labeled[i] && (rowApportionmentError[i] > 0))
                    {
                        positiveRowLabeled = true;
                        rowColIndex = i;
                        break;
                    }
                }

                if (positiveRowLabeled)
                {
                    break;
                }

                var delta = ComputeDelta(ctx, labeled)
                    ?? throw new BiproportionalApportionmentException("Problem is infeasible. Could not find a valid delta");
                ctx.NumberOfUpdates++;
                UpdateDivisors(ctx, labeled, delta);
            }

            // Make a transfer, after a positive row has been reached
            ctx.NumberOfTransfers++;

            while (predecessor[rowColIndex] >= 0)
            {
                if (rowColIndex < ctx.Rows)
                {
                    // this is a row index
                    var i = rowColIndex;
                    var j = predecessor[i] - ctx.Rows;
                    ctx.Apportionment[i][j]--;
                }
                else
                {
                    // this is a column index
                    var j = rowColIndex - ctx.Rows;
                    var i = predecessor[j + ctx.Rows];
                    ctx.Apportionment[i][j]++;
                }

                rowColIndex = predecessor[rowColIndex];
            }
        }
    }

    /// <summary>
    /// Check for ties and update divisors.
    /// </summary>
    /// <param name="ctx">The TT-Method Context.</param>
    private void CheckForTiesAndUpdateDivisors(TieAndTransferMethodContext ctx)
    {
        var labeled = new bool[ctx.Rows + ctx.Cols];
        var predecessor = new int[ctx.Rows + ctx.Cols];

        // Check for ties and update divisors
        for (var i = 0; i < ctx.Rows; i++)
        {
            for (var j = 0; j < ctx.Cols; j++)
            {
                // Weight must be positive to be a tie...
                if (ctx.Weights[i][j].VoteCount <= 0)
                {
                    continue;
                }

                // Check if quotient is equal to rounding function
                var quotient = ctx.GetQuotient(i, j);
                var canRoundUp = quotient == SignPost.Get(ctx.Apportionment[i][j]);
                var canRoundDown = quotient == SignPost.Get(ctx.Apportionment[i][j]) - 1;

                if (canRoundUp || canRoundDown)
                {
                    // Initialize labels and predecessors
                    for (var k = 0; k < ctx.Rows + ctx.Cols; k++)
                    {
                        labeled[k] = false;
                        predecessor[k] = -1;
                    }

                    if (canRoundDown)
                    {
                        // Can round DOWN: Start search by labeling row i
                        labeled[i] = true;
                    }
                    else
                    {
                        // Can round UP: Start search by labeling column j
                        labeled[j + ctx.Rows] = true;
                    }

                    BfsRowColumnGraph(ctx, labeled, predecessor);

                    // If row i or column j is not labeled then update divisors
                    if ((canRoundDown && !labeled[j + ctx.Rows]) || (canRoundUp && !labeled[i]))
                    {
                        var delta = ComputeDelta(ctx, labeled);

                        if (delta != null)
                        {
                            delta = (delta + 1) / 2;
                        }
                        else
                        {
                            // Can choose any value > 1
                            delta = 2;
                        }

                        UpdateDivisors(ctx, labeled, delta.Value);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Scale divisors such that the first column divisor is 1.
    /// </summary>
    /// <param name="ctx">The TT-Method Context.</param>
    private void ScaleDivisors(TieAndTransferMethodContext ctx)
    {
        var divisorFactor = ctx.ColDivisors[0];

        for (var i = 0; i < ctx.Rows; i++)
        {
            ctx.RowDivisors[i] *= divisorFactor;
        }

        for (var j = 0; j < ctx.Cols; j++)
        {
            ctx.ColDivisors[j] /= divisorFactor;
        }
    }

    // This algorithm is described in Martin Zachariasen paper as "Algorithm 1 Breadth-first search (BFS) in row/column graph" and in BAZI.
    private void BfsRowColumnGraph(TieAndTransferMethodContext ctx, bool[] labeled, int[] predecessor)
    {
        var queue = new int[ctx.Rows + ctx.Cols]; // queue of labeled rows and columns
        var queueFrontIndex = 0;
        var queueEndIndex = 0;

        // Append (pre)labeled rows/columns to Q
        for (var i = 0; i < ctx.Rows + ctx.Cols; i++)
        {
            if (labeled[i])
            {
                queue[queueEndIndex++] = i;
            }
        }

        while (queueFrontIndex < queueEndIndex)
        {
            var rowColIndex = queue[queueFrontIndex++];
            if (rowColIndex < ctx.Rows)
            {
                // row index
                var i = rowColIndex;

                // label all unlabeled columns where rounding UP is possible
                for (var j = 0; j < ctx.Cols; j++)
                {
                    if (!labeled[j + ctx.Rows] && ctx.Weights[i][j].VoteCount > 0)
                    {
                        var quotient = ctx.GetQuotient(i, j);

                        if (quotient == SignPost.Get(ctx.Apportionment[i][j]))
                        {
                            queue[queueEndIndex++] = j + ctx.Rows;
                            labeled[j + ctx.Rows] = true;
                            predecessor[j + ctx.Rows] = i;
                        }
                    }
                }
            }
            else
            {
                // column index
                var j = rowColIndex - ctx.Rows;

                // label all unlabeled rows where rounding DOWN is possible
                for (var i = 0; i < ctx.Rows; i++)
                {
                    if (!labeled[i] && ctx.Weights[i][j].VoteCount > 0)
                    {
                        var quotient = ctx.GetQuotient(i, j);

                        if (quotient == SignPost.Get(ctx.Apportionment[i][j]) - 1)
                        {
                            queue[queueEndIndex++] = i;
                            labeled[i] = true;
                            predecessor[i] = ctx.Rows + j;
                        }
                    }
                }
            }
        }
    }

    // This algorithm is described in Martin Zachariasen paper as "Algorithm 2 Compute Delta for updaing multipiers" and in BAZI.
    private Rational? ComputeDelta(TieAndTransferMethodContext ctx, bool[] labeled)
    {
        Rational? minDelta = null;

        for (var i = 0; i < ctx.Rows; i++)
        {
            for (var j = 0; j < ctx.Cols; j++)
            {
                // Labeled row and unlabeled column
                if (labeled[i] && (!labeled[j + ctx.Rows]) && ctx.Weights[i][j].VoteCount > 0)
                {
                    var quotient = ctx.GetQuotient(i, j);
                    var delta = SignPost.Get(ctx.Apportionment[i][j]) / quotient;

                    if (minDelta == null || delta < minDelta)
                    {
                        minDelta = delta;
                    }
                }

                // Unlabeled row and labeled column
                if (!labeled[i] && labeled[j + ctx.Rows] && ctx.Weights[i][j].VoteCount > 0)
                {
                    var quotient = ctx.GetQuotient(i, j);
                    var dFunc = SignPost.Get(ctx.Apportionment[i][j]) - 1;

                    if (dFunc > 0)
                    {
                        var delta = quotient / dFunc;
                        if (minDelta == null || delta < minDelta)
                        {
                            minDelta = delta;
                        }
                    }
                }
            }
        }

        if (minDelta <= 1 && minDelta > 0)
        {
            throw new BiproportionalApportionmentException("Computed delta is between 0 and 1. Problem is infeasible");
        }

        return minDelta;
    }

    private void CalculateTies(TieAndTransferMethodContext ctx)
    {
        for (var i = 0; i < ctx.Rows; i++)
        {
            for (var j = 0; j < ctx.Cols; j++)
            {
                ctx.Ties[i][j] = TieState.Unique;

                if (ctx.Weights[i][j].VoteCount > 0)
                {
                    var quotient = ctx.GetQuotient(i, j);

                    if (quotient == SignPost.Get(ctx.Apportionment[i][j]))
                    {
                        ctx.Ties[i][j] = TieState.Positive;
                    }
                    else if (quotient == SignPost.Get(ctx.Apportionment[i][j] - 1))
                    {
                        ctx.Ties[i][j] = TieState.Negative;
                    }
                }
            }
        }
    }

    private void UpdateDivisors(TieAndTransferMethodContext ctx, bool[] labeled, Rational delta)
    {
        // Update row divisors for labeled rows
        for (var i = 0; i < ctx.Rows; i++)
        {
            if (labeled[i])
            {
                ctx.RowDivisors[i] /= delta;
            }
        }

        // Update column divisors for labeled columns
        for (var j = 0; j < ctx.Cols; j++)
        {
            if (labeled[j + ctx.Rows])
            {
                ctx.ColDivisors[j] *= delta;
            }
        }
    }
}
