// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils.DoubleProportional.Models;

namespace Voting.Ausmittlung.Core.Utils.DoubleProportional;

public class SaintLagueAlgorithm
{
    public SaintLagueAlgorithmResult? Calculate(decimal[] voterNumbers, int numberOfMandates)
    {
        var context = new SaintLagueAlgorithmContext(voterNumbers, numberOfMandates);
        var voterNumber = context.Columns.Sum(c => c.VoterNumber);
        var columns = context.Columns;

        // Step 1: Calculate the initial divisor and check whether we can distribute all number of mandates.
        context.Divisor = decimal.Round(voterNumber / context.NumberOfMandates);
        if (context.Divisor == 0)
        {
            throw new DoubleProportionalAlgorithmException("Saint lague divisor is 0");
        }

        var distributedNumberOfMandates = SetSuperApportionmentOnColumns(context);

        // Step 2: Try to reduce the distribution error iteratively.
        while (distributedNumberOfMandates != context.NumberOfMandates && !columns.Any(c => c.LotDecisionRequired))
        {
            context.DistributionError = Math.Abs(distributedNumberOfMandates - context.NumberOfMandates);

            if (distributedNumberOfMandates > context.NumberOfMandates)
            {
                DecreaseDistributedNumberOfMandates(context);
            }
            else
            {
                IncreaseDistributedNumberOfMandates(context);
            }

            distributedNumberOfMandates = SetSuperApportionmentOnColumns(context);
        }

        if (!context.HasLotDecisions)
        {
            var upperLimitDivisor = GetColumnDivisors(context.Columns, -0.5M).Min();
            var lowerLimitDivisor = GetColumnDivisors(context.Columns, 0.5M).Max();
            var selectDivisor = decimal.Divide(lowerLimitDivisor + upperLimitDivisor, 2);

            // If possible, try to get an integer election key.
            context.ElectionKey = upperLimitDivisor - lowerLimitDivisor >= 1 || decimal.Floor(upperLimitDivisor) != decimal.Floor(lowerLimitDivisor)
                ? decimal.Round(selectDivisor)
                : selectDivisor;
            context.Divisor = context.ElectionKey;
        }
        else
        {
            context.ElectionKey = context.Divisor;
        }

        SetSuperApportionmentOnColumns(context);

        return new SaintLagueAlgorithmResult(
            context.Columns.Select(c => c.Quotient).ToArray(),
            context.Columns.Select(c => c.NumberOfMandates).ToArray(),
            context.TieStates,
            context.CountOfMissingNumberOfMandates,
            context.ElectionKey);
    }

    private void IncreaseDistributedNumberOfMandates(SaintLagueAlgorithmContext context)
    {
        // The divisor needs to be decreased to distribute more number of mandates.
        var divisors = GetColumnDivisors(context.Columns, 0.5M);
        var maxDivisor = divisors.Max();
        var countOfEqualMaxDivisors = divisors.Count(d => d.ApproxEquals(maxDivisor));
        if (countOfEqualMaxDivisors > 1 && countOfEqualMaxDivisors > context.DistributionError)
        {
            context.CountOfMissingNumberOfMandates = countOfEqualMaxDivisors - context.DistributionError;
            context.ElectionKey = maxDivisor;

            for (var l = 0; l < context.Columns.Length; l++)
            {
                if (!divisors[l].ApproxEquals(maxDivisor))
                {
                    continue;
                }

                context.TieStates[l] = TieState.Negative;
                context.Columns[l].LotDecisionRequired = true;
            }
        }

        // With the largest divisor we increase the distributed number of mandates by 1 (if not lot decision is involved).
        context.Divisor = maxDivisor;
    }

    private void DecreaseDistributedNumberOfMandates(SaintLagueAlgorithmContext context)
    {
        // The divisor needs to be increased to distribute fewer number of mandates.
        var divisors = GetColumnDivisors(context.Columns, -0.5M);
        var minDivisor = divisors.Min();

        if (minDivisor == decimal.MaxValue)
        {
            throw new DoubleProportionalAlgorithmException("Invalid min divisor. Problem is not feasible.");
        }

        var countOfEqualMinDivisors = divisors.Count(d => d.ApproxEquals(minDivisor));
        if (countOfEqualMinDivisors > 1 && countOfEqualMinDivisors > context.DistributionError)
        {
            context.CountOfMissingNumberOfMandates = countOfEqualMinDivisors - context.DistributionError;
            context.ElectionKey = minDivisor;

            for (var l = 0; l < context.Columns.Length; l++)
            {
                if (divisors[l].ApproxEquals(minDivisor))
                {
                    context.TieStates[l] = TieState.Negative;
                    context.Columns[l].LotDecisionRequired = true;
                }
            }

            context.Divisor = minDivisor;
            return;
        }

        // Set the 2nd smallest divisor as the divisor, because with the min divisor it will still round up the closest number to the rounding function.
        // With the 2nd smallest divisor we decrease the distributed number of mandates by 1 (if no lot decision is involved).
        var divisorsWithoutMinDivisor = divisors.Where(d => !d.ApproxEquals(minDivisor)).ToList();

        if (divisorsWithoutMinDivisor.Count == 0)
        {
            throw new DoubleProportionalAlgorithmException("Cannot determine a valid min divisor");
        }

        context.Divisor = divisorsWithoutMinDivisor.MinBy(d => d);
    }

    private int SetSuperApportionmentOnColumns(SaintLagueAlgorithmContext context)
    {
        foreach (var column in context.Columns)
        {
            column.Quotient = column.VoterNumber / context.Divisor;
            column.NumberOfMandates = (int)decimal.Round(column.Quotient, MidpointRounding.AwayFromZero);
        }

        return context.Columns.Sum(x => x.NumberOfMandates);
    }

    private decimal[] GetColumnDivisors(SaintLagueVectorItem[] columns, decimal deltaNumberOfMandates)
    {
        var x = columns.Select(c => new
        {
            c.VoterNumber,
            NumberOfMandates = c.NumberOfMandates + deltaNumberOfMandates,
        }).ToArray();

        return x.Select(v => v.NumberOfMandates > 0 ? v.VoterNumber / v.NumberOfMandates : decimal.MaxValue).ToArray();
    }

    private class SaintLagueAlgorithmContext
    {
        public SaintLagueAlgorithmContext(decimal[] voterNumbers, int numberOfMandates)
        {
            Columns = voterNumbers.Select(v => new SaintLagueVectorItem { VoterNumber = v }).ToArray();
            VoterNumber = Columns.Sum(c => c.VoterNumber);
            Cols = Columns.Length;
            NumberOfMandates = numberOfMandates;
            TieStates = new TieState[Cols];
        }

        public SaintLagueVectorItem[] Columns { get; }

        public int NumberOfMandates { get; }

        public decimal VoterNumber { get; }

        public int Cols { get; }

        public TieState[] TieStates { get; }

        public int DistributionError { get; set; }

        public int CountOfMissingNumberOfMandates { get; set; }

        public decimal ElectionKey { get; set; }

        public decimal Divisor { get; set; }

        public bool HasLotDecisions => Columns.Any(c => c.LotDecisionRequired);
    }

    private class SaintLagueVectorItem
    {
        public decimal VoterNumber { get; set; }

        public int NumberOfMandates { get; set; }

        public decimal Quotient { get; set; }

        public bool LotDecisionRequired { get; set; }
    }
}
