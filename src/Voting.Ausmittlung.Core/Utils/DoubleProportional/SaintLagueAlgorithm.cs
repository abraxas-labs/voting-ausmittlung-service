// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using Rationals;
using Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils.DoubleProportional.Models;

namespace Voting.Ausmittlung.Core.Utils.DoubleProportional;

public class SaintLagueAlgorithm
{
    private const int MaxCorrectionAttempts = 100000;

    public SaintLagueAlgorithmResult? Calculate(Rational[] voterNumbers, int numberOfMandates)
    {
        var context = new SaintLagueAlgorithmContext(voterNumbers, numberOfMandates);
        var columns = context.Columns;

        // Step 1: Calculate the initial divisor and check whether we can distribute all number of mandates.
        context.Divisor = context.SumVoterNumber / context.NumberOfMandates;
        if (context.Divisor == 0)
        {
            throw new DoubleProportionalAlgorithmException("Saint lague divisor is 0");
        }

        var distributedNumberOfMandates = SetSuperApportionmentOnColumns(context);

        // Step 2: Try to reduce the distribution error iteratively.
        var countOfCorrectionAttempts = 0;
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

            countOfCorrectionAttempts++;
            if (countOfCorrectionAttempts == MaxCorrectionAttempts)
            {
                throw new DoubleProportionalAlgorithmException("Max correction attempts in saint lague algorithm reached");
            }
        }

        if (!context.HasLotDecisions)
        {
            context.ElectionKey = DivisorUtils.CalculateSelectDivisor(
                context.Columns.Select(co => new DivisorApportionment(co.VoterNumber, co.NumberOfMandates)).ToArray());
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
        var divisors = GetColumnDivisors(context.Columns, new Rational(1, 2));
        var maxDivisor = divisors.Max();
        var countOfEqualMaxDivisors = divisors.Count(d => d == maxDivisor);
        if (countOfEqualMaxDivisors > 1 && countOfEqualMaxDivisors > context.DistributionError)
        {
            context.CountOfMissingNumberOfMandates = countOfEqualMaxDivisors - context.DistributionError;
            context.ElectionKey = maxDivisor;

            for (var l = 0; l < context.Columns.Length; l++)
            {
                if (divisors[l] != maxDivisor)
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
        var divisors = GetColumnDivisors(context.Columns, new Rational(-1, 2));
        var minDivisor = divisors.Min();

        if (minDivisor == DivisorUtils.ParseToRational(decimal.MaxValue))
        {
            throw new DoubleProportionalAlgorithmException("Invalid min divisor. Problem is not feasible.");
        }

        var countOfEqualMinDivisors = divisors.Count(d => d == minDivisor);
        if (countOfEqualMinDivisors > 1 && countOfEqualMinDivisors > context.DistributionError)
        {
            context.CountOfMissingNumberOfMandates = countOfEqualMinDivisors - context.DistributionError;
            context.ElectionKey = minDivisor;

            for (var l = 0; l < context.Columns.Length; l++)
            {
                if (divisors[l] == minDivisor)
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
        var divisorsWithoutMinDivisor = divisors.Where(d => d != minDivisor).ToList();

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
            column.NumberOfMandates = column.Quotient.Round();
        }

        return context.Columns.Sum(x => x.NumberOfMandates);
    }

    private Rational[] GetColumnDivisors(SaintLagueVectorItem[] columns, Rational deltaNumberOfMandates)
    {
        return DivisorUtils.GetDivisors(
            columns.Select(c => new DivisorApportionment(c.VoterNumber, c.NumberOfMandates)).ToArray(),
            deltaNumberOfMandates);
    }

    private class SaintLagueAlgorithmContext
    {
        public SaintLagueAlgorithmContext(Rational[] voterNumbers, int numberOfMandates)
        {
            Columns = voterNumbers.Select(v => new SaintLagueVectorItem { VoterNumber = v }).ToArray();
            Cols = Columns.Length;
            NumberOfMandates = numberOfMandates;
            TieStates = new TieState[Cols];

            SumVoterNumber = Rational.Zero;

            foreach (var voterNumber in voterNumbers)
            {
                SumVoterNumber += voterNumber;
            }
        }

        public SaintLagueVectorItem[] Columns { get; }

        public int NumberOfMandates { get; }

        public Rational SumVoterNumber { get; }

        public int Cols { get; }

        public TieState[] TieStates { get; }

        public int DistributionError { get; set; }

        public int CountOfMissingNumberOfMandates { get; set; }

        public Rational ElectionKey { get; set; }

        public Rational Divisor { get; set; } = Rational.Zero;

        public bool HasLotDecisions => Columns.Any(c => c.LotDecisionRequired);
    }

    private class SaintLagueVectorItem
    {
        public Rational VoterNumber { get; set; } = Rational.Zero;

        public int NumberOfMandates { get; set; }

        public Rational Quotient { get; set; } = Rational.Zero;

        public bool LotDecisionRequired { get; set; }
    }
}
