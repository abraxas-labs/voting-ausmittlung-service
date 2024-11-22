// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Rationals;
using Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;

namespace Voting.Ausmittlung.BiproportionalApportionment;

/// <summary>
/// Result of a biproportional apportionment calculation.
/// </summary>
public class BiproportionalApportionmentResult
{
    internal BiproportionalApportionmentResult(Rational[] columnDivisors, Rational[] rowDivisors, int numberOfTransfers, int numberOfUpdates, TieState[][] ties, int[][] apportionment)
    {
        ColumnDivisors = columnDivisors;
        RowDivisors = rowDivisors;
        NumberOfTransfers = numberOfTransfers;
        NumberOfUpdates = numberOfUpdates;
        Ties = ties;
        Apportionment = apportionment;
        HasTies = Ties.SelectMany(x => x).Any(t => t != TieState.Unique);
    }

    /// <summary>
    /// Gets the column divisors (union list divisors).
    /// </summary>
    public Rational[] ColumnDivisors { get; }

    /// <summary>
    /// Gets the row divisors (election / domain of influence divisors).
    /// </summary>
    public Rational[] RowDivisors { get; }

    /// <summary>
    /// Gets the number of transfer operations.
    /// </summary>
    public int NumberOfTransfers { get; }

    /// <summary>
    /// Gets the number of update operations.
    /// </summary>
    public int NumberOfUpdates { get; }

    /// <summary>
    /// Gets the tie states of the apportionment.
    /// </summary>
    public TieState[][] Ties { get; }

    /// <summary>
    /// Gets the apportionment (represents the number of mandates which a proportional election list receives).
    /// </summary>
    public int[][] Apportionment { get; }

    /// <summary>
    /// Gets a value indicating whether the apportionment has ties.
    /// </summary>
    public bool HasTies { get; }
}
