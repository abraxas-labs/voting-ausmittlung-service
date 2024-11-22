// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Rationals;

namespace Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;

/// <summary>
/// Divisor method to solve the apportionment problem for a vector (Martin Zachariasen, 2007).
/// The algorithm is described in the source code of BAZI.
/// </summary>
internal class VectorApportionmentMethod
{
    public VectorApportionmentMethodResult Calculate(Weight[] weights, int numberOfMandates)
    {
        if (numberOfMandates == 0)
        {
            throw new BiproportionalApportionmentException("Vector apportionment calculation failed because a column had no number of mandates");
        }

        var apportionment = CalculateApportionment(weights, numberOfMandates);
        var maxDivisor = GetMaxDivisor(weights, apportionment);
        return new VectorApportionmentMethodResult(apportionment, maxDivisor);
    }

    private int[] CalculateApportionment(Weight[] weights, int numberOfMandates)
    {
        var divisor = new Rational(weights.Sum(x => x.VoteCount), numberOfMandates);
        var apportionment = new int[weights.Length];

        // Initial distribution
        for (var i = 0; i < weights.Length; i++)
        {
            apportionment[i] = SignPost.Round(weights[i].VoteCount / divisor);
        }

        // Fix the error of the initial distribution by increasing the number of mandates if underrepresented or decreasing if overrepresented.
        var currentDistributedNumberOfMandates = apportionment.Sum();
        if (currentDistributedNumberOfMandates < numberOfMandates)
        {
            while (currentDistributedNumberOfMandates < numberOfMandates)
            {
                var underrepresentatedIndex = 0;
                Rational? minRatio = null;

                for (var i = 0; i < weights.Length; i++)
                {
                    if (weights[i].VoteCount >= 0)
                    {
                        var ratio = SignPost.Get(apportionment[i]) / weights[i].VoteCount;
                        if ((minRatio == null) || ratio < minRatio)
                        {
                            underrepresentatedIndex = i;
                            minRatio = ratio;
                        }
                    }
                }

                if (minRatio == null)
                {
                    throw new BiproportionalApportionmentException($"Problem is infeasible, cannot increase the number of mandates in {weights[0].Name} by the {nameof(VectorApportionmentMethod)}");
                }

                apportionment[underrepresentatedIndex]++;
                currentDistributedNumberOfMandates++;
            }
        }
        else if (currentDistributedNumberOfMandates > numberOfMandates)
        {
            while (currentDistributedNumberOfMandates > numberOfMandates)
            {
                var overrepresentedIndex = 0;
                Rational? maxRatio = null;

                for (var i = 0; i < weights.Length; i++)
                {
                    if (weights[i].VoteCount > 0)
                    {
                        var ratio = SignPost.Get(apportionment[i] - 1) / weights[i].VoteCount;

                        if (maxRatio == null || ratio > maxRatio)
                        {
                            overrepresentedIndex = i;
                            maxRatio = ratio;
                        }
                    }
                }

                if (maxRatio == null)
                {
                    throw new BiproportionalApportionmentException($"Problem is infeasible, cannot reduce the number of mandates in {weights[0].Name} by the {nameof(VectorApportionmentMethod)}");
                }

                apportionment[overrepresentedIndex]--;
                currentDistributedNumberOfMandates--;
            }
        }

        return apportionment;
    }

    private Rational GetMaxDivisor(Weight[] weights, int[] apportionment)
    {
        Rational? maxRatio = null;

        for (var i = 0; i < weights.Length; i++)
        {
            if (weights[i].VoteCount <= 0)
            {
                continue;
            }

            var ratio = SignPost.Get(apportionment[i] - 1) / weights[i].VoteCount;
            if (maxRatio == null || ratio > maxRatio)
            {
                maxRatio = ratio;
            }
        }

        if (maxRatio == null || maxRatio < 0)
        {
            throw new BiproportionalApportionmentException($"Cannot find the max divisor of {weights[0].Name} by the {nameof(VectorApportionmentMethod)}");
        }

        return 1 / maxRatio.Value;
    }
}
