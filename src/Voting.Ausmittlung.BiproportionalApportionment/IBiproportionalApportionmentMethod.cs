// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.BiproportionalApportionment;

/// <summary>
/// Interface of a method to solve a biproportional (matrix) apportionment problem by calculating row and column divisors.
/// In proportional elections this is used to distribute number of mandates per list, while all the union lists (=group of lists, column) and election (row) number of mandates must be distributed.
/// </summary>
public interface IBiproportionalApportionmentMethod
{
    /// <summary>
    /// Calculates the result of the biproportional apportionment problem.
    /// </summary>
    /// <param name="apportionmentData">Input data.</param>
    /// <returns>The result of the biproportional apportionment.</returns>
    BiproportionalApportionmentResult Calculate(BiproportionalApportionmentData apportionmentData);
}
