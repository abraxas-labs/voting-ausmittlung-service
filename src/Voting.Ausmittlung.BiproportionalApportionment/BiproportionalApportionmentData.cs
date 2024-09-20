// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.BiproportionalApportionment;

/// <summary>
/// Input data for a biproportional apportionment method.
/// </summary>
public class BiproportionalApportionmentData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiproportionalApportionmentData"/> class.
    /// </summary>
    /// <param name="weightMatrix">Weight matrix (represents proportional election list as elements).</param>
    /// <param name="rowApportionment">Row apportionment.</param>
    /// <param name="columnApportionment">Column apportionment.</param>
    public BiproportionalApportionmentData(Weight[][] weightMatrix, int[] rowApportionment, int[] columnApportionment)
    {
        WeightMatrix = weightMatrix;
        RowApportionment = rowApportionment;
        ColumnApportionment = columnApportionment;
    }

    /// <summary>
    /// Gets the weight matrix (represents proportional election list as elements).
    /// </summary>
    public Weight[][] WeightMatrix { get; }

    /// <summary>
    /// Gets the row apportionment (number of mandates per election).
    /// </summary>
    public int[] RowApportionment { get; }

    /// <summary>
    /// Gets the column apportionment (number of mandates per union list).
    /// </summary>
    public int[] ColumnApportionment { get; }
}
