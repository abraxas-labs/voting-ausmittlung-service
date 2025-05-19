// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public static class SubTotalExtensions
{
    private static readonly VotingDataSource[] _dataSources =
        (VotingDataSource[])Enum.GetValues(typeof(VotingDataSource));

    public static TSubTotal GetSubTotal<TSubTotal>(this IHasSubTotals<TSubTotal> container, VotingDataSource source)
        where TSubTotal : class
    {
        return source switch
        {
            VotingDataSource.Conventional => container.ConventionalSubTotal,
            VotingDataSource.EVoting => container.EVotingSubTotal,
            VotingDataSource.ECounting => container.ECountingSubTotal,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };
    }

    public static TSubTotal GetNonNullableSubTotal<TSubTotal, TNullableSubTotal>(this IHasSubTotals<TSubTotal, TNullableSubTotal> container, VotingDataSource source)
        where TSubTotal : class
        where TNullableSubTotal : INullableSubTotal<TSubTotal>
    {
        return source switch
        {
            VotingDataSource.Conventional => throw new InvalidOperationException("Cannot return a non-nullable SubTotal for the nullable conventional Subtotal."),
            VotingDataSource.EVoting => container.EVotingSubTotal,
            VotingDataSource.ECounting => container.ECountingSubTotal,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };
    }

    public static void ResetSubTotal<TSubTotal>(this IHasSubTotals<TSubTotal> container, VotingDataSource source)
        where TSubTotal : class, new()
    {
        switch (source)
        {
            case VotingDataSource.Conventional:
                container.ConventionalSubTotal = new TSubTotal();
                break;
            case VotingDataSource.EVoting:
                container.EVotingSubTotal = new TSubTotal();
                break;
            case VotingDataSource.ECounting:
                container.ECountingSubTotal = new TSubTotal();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }
    }

    public static void ResetSubTotal<TSubTotal, TNullableSubTotal>(
        this IHasSubTotals<TSubTotal, TNullableSubTotal> container,
        VotingDataSource source,
        bool setZeroInsteadNull = false)
        where TSubTotal : class, new()
        where TNullableSubTotal : class, INullableSubTotal<TSubTotal>, new()
    {
        switch (source)
        {
            case VotingDataSource.Conventional:
                container.ConventionalSubTotal = new TNullableSubTotal();
                if (setZeroInsteadNull)
                {
                    container.ConventionalSubTotal.ReplaceNullValuesWithZero();
                }

                break;
            case VotingDataSource.EVoting:
                container.EVotingSubTotal = new TSubTotal();
                break;
            case VotingDataSource.ECounting:
                container.ECountingSubTotal = new TSubTotal();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }
    }

    public static IEnumerable<(VotingDataSource DataSource, TSubTotal SubTotal)> SubTotalAsEnumerable<TSubTotal>(this IHasSubTotals<TSubTotal> container)
        where TSubTotal : class
    {
        foreach (var dataSource in _dataSources)
        {
            yield return (dataSource, container.GetSubTotal(dataSource));
        }
    }

    public static IEnumerable<(TSubTotal Primary, TOtherSubTotal Secondary)> SubTotalsAsPairEnumerable<TSubTotal, TOtherSubTotal>(
        this IHasSubTotals<TSubTotal> container,
        IHasSubTotals<TOtherSubTotal> container2)
        where TSubTotal : class
        where TOtherSubTotal : class
    {
        foreach (var dataSource in _dataSources)
        {
            yield return (container.GetSubTotal(dataSource), container2.GetSubTotal(dataSource));
        }
    }

    public static void ForEachSubTotal<TSubTotal>(
        this IHasSubTotals<TSubTotal> container,
        IHasSubTotals<TSubTotal> otherContainer,
        Action<TSubTotal, TSubTotal> action)
        where TSubTotal : class
    {
        foreach (var dataSource in _dataSources)
        {
            action(container.GetSubTotal(dataSource), otherContainer.GetSubTotal(dataSource));
        }
    }

    public static void ForEachSubTotal<TSubTotal, TNullableSubTotal>(
        this IHasSubTotals<TSubTotal> container,
        IHasSubTotals<TSubTotal, TNullableSubTotal> otherContainer,
        Action<TSubTotal, TSubTotal> action)
        where TSubTotal : class
        where TNullableSubTotal : INullableSubTotal<TSubTotal>
    {
        foreach (var dataSource in _dataSources)
        {
            action(container.GetSubTotal(dataSource), otherContainer.GetSubTotalAsNonNullable(dataSource));
        }
    }

    public static void MoveECountingSubTotalsToConventional<TSubTotal>(
        this IHasSubTotals<TSubTotal> target)
        where TSubTotal : class, ISummableSubTotal<TSubTotal>, new()
    {
        target.ConventionalSubTotal.Add(target.ECountingSubTotal);
        target.ResetSubTotal(VotingDataSource.ECounting);
    }

    public static void MoveECountingSubTotalsToConventional<TSubTotal, TNullableSubTotal>(
        this IHasSubTotals<TSubTotal, TNullableSubTotal> target)
        where TSubTotal : class, ISummableSubTotal<TSubTotal>, new()
        where TNullableSubTotal : class, ISummableSubTotal<TSubTotal>, INullableSubTotal<TSubTotal>, new()
    {
        target.ConventionalSubTotal.Add(target.ECountingSubTotal);
        target.ResetSubTotal(VotingDataSource.ECounting);
    }

    public static void AddForAllSubTotals<TSubTotal, TNullableSubTotal>(
        this IHasSubTotals<TSubTotal> target,
        IHasSubTotals<TSubTotal, TNullableSubTotal> toAdd,
        int deltaFactor = 1)
        where TSubTotal : class, ISummableSubTotal<TSubTotal>
        where TNullableSubTotal : ISummableSubTotal<TSubTotal>, INullableSubTotal<TSubTotal>
    {
        foreach (var (dataSource, targetSubTotal) in target.SubTotalAsEnumerable())
        {
            var toAddSubTotal = toAdd.GetSubTotalAsNonNullable(dataSource);
            targetSubTotal.Add(toAddSubTotal, deltaFactor);
        }
    }

    public static void AddForAllSubTotals<TSubTotal, TNullableSubTotal>(
        this IHasSubTotals<TSubTotal, TNullableSubTotal> target,
        IHasSubTotals<TSubTotal, TNullableSubTotal> toAdd,
        int deltaFactor = 1)
        where TSubTotal : class, ISummableSubTotal<TSubTotal>
        where TNullableSubTotal : INullableSubTotal<TSubTotal>, ISummableSubTotal<TSubTotal>
    {
        foreach (var dataSource in _dataSources)
        {
            // conventional is the only nullable subTotal.
            if (dataSource == VotingDataSource.Conventional)
            {
                target.ConventionalSubTotal.Add(toAdd.ConventionalSubTotal.MapToNonNullableSubTotal(), deltaFactor);
                continue;
            }

            target.GetNonNullableSubTotal(dataSource).Add(toAdd.GetNonNullableSubTotal(dataSource), deltaFactor);
        }
    }

    // Is used to simplify calculations by avoiding handling null values.
    // Should only be used for the right side of an assigment, since a copy of conventional is returned an not the reference itself.
    private static TSubTotal GetSubTotalAsNonNullable<TSubTotal, TNullableSubTotal>(this IHasSubTotals<TSubTotal, TNullableSubTotal> container, VotingDataSource source)
        where TSubTotal : class
        where TNullableSubTotal : INullableSubTotal<TSubTotal>
    {
        return source switch
        {
            VotingDataSource.Conventional => container.ConventionalSubTotal.MapToNonNullableSubTotal(),
            VotingDataSource.EVoting => container.EVotingSubTotal,
            VotingDataSource.ECounting => container.ECountingSubTotal,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };
    }
}
