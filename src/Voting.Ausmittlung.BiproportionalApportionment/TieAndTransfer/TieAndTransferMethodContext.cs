// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;

internal class TieAndTransferMethodContext
{
    public TieAndTransferMethodContext(Weight[][] weights, int[] rowApportionment, int[] columnApportionment)
    {
        Weights = weights;
        RowApportionment = rowApportionment;
        ColumnApportionment = columnApportionment;

        Rows = rowApportionment.Length;
        Cols = columnApportionment.Length;

        RowDivisors = new decimal[Rows];
        ColDivisors = new decimal[Cols];

        Apportionment = new int[Rows][];
        Ties = new TieState[Rows][];
        for (var i = 0; i < Rows; i++)
        {
            Apportionment[i] = new int[Cols];
            Ties[i] = new TieState[Cols];
        }
    }

    public Weight[][] Weights { get; }

    public int[] RowApportionment { get; }

    public int[] ColumnApportionment { get; }

    public int Rows { get; }

    public int Cols { get; }

    public decimal[] RowDivisors { get; }

    public decimal[] ColDivisors { get; }

    public int[][] Apportionment { get; }

    public TieState[][] Ties { get; }

    public int NumberOfUpdates { get; set; }

    public int NumberOfTransfers { get; set; }

    /// <summary>
    /// Gets the quotient (number of mandates per list) on a certain cell.
    /// </summary>
    /// <param name="row">Row index.</param>
    /// <param name="col">Column index.</param>
    /// <returns>Returns the quotient (number of mandates per list).</returns>
    public decimal GetQuotient(int row, int col)
    {
        var quotient = decimal
            .Divide(Weights[row][col].VoteCount, RowDivisors[row]);
        return decimal.Divide(quotient, ColDivisors[col]);
    }
}
