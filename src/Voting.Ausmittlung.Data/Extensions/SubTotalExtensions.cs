// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public static class SubTotalExtensions
{
    private static readonly VotingDataSource[] DataSources = (VotingDataSource[])Enum.GetValues(typeof(VotingDataSource));

    public static TSubTotal GetSubTotal<TSubTotal>(this IHasSubTotals<TSubTotal> container, VotingDataSource source)
        where TSubTotal : class
    {
        return source switch
        {
            VotingDataSource.Conventional => container.ConventionalSubTotal,
            VotingDataSource.EVoting => container.EVotingSubTotal,
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
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }
    }

    public static void ResetSubTotal<TSubTotal, TNullableSubTotal>(this IHasSubTotals<TSubTotal, TNullableSubTotal> container, VotingDataSource source, bool setZeroInsteadNull = false)
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
            default:
                throw new ArgumentOutOfRangeException(nameof(source), source, null);
        }
    }

    public static void ForEachSubTotal<TSubTotal>(
        this IHasSubTotals<TSubTotal> container,
        IHasSubTotals<TSubTotal> otherContainer,
        Action<TSubTotal, TSubTotal> action)
        where TSubTotal : class
    {
        foreach (var dataSource in DataSources)
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
        foreach (var dataSource in DataSources)
        {
            action(container.GetSubTotal(dataSource), otherContainer.GetSubTotal(dataSource));
        }
    }

    // Is used to simplify calculations by avoiding handling null values.
    // Should only be used for the right side of an assigment, since a copy of conventional is returned an not the reference itself.
    private static TSubTotal GetSubTotal<TSubTotal, TNullableSubTotal>(this IHasSubTotals<TSubTotal, TNullableSubTotal> container, VotingDataSource source)
        where TSubTotal : class
        where TNullableSubTotal : INullableSubTotal<TSubTotal>
    {
        return source switch
        {
            VotingDataSource.Conventional => container.ConventionalSubTotal.MapToNonNullableSubTotal(),
            VotingDataSource.EVoting => container.EVotingSubTotal,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };
    }
}
