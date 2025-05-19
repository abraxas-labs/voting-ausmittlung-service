// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public static class VoteCountExtensions
{
    private static readonly VotingDataSource[] DataSources = (VotingDataSource[])Enum.GetValues(typeof(VotingDataSource));

    public static void ResetSubTotal(this IHasVoteCounts counts, VotingDataSource dataSource)
        => counts.SetVoteCountOfDataSource(dataSource, 0);

    public static void ResetSubTotal(this IHasNullableConventionalVoteCounts counts, VotingDataSource dataSource, bool setZeroInsteadNull = false)
        => counts.SetVoteCountOfDataSource(dataSource, setZeroInsteadNull ? 0 : (int?)null, 0);

    public static int GetVoteCountOfDataSource(this IHasVoteCounts counts, VotingDataSource dataSource)
    {
        return dataSource switch
        {
            VotingDataSource.Conventional => counts.ConventionalVoteCount,
            VotingDataSource.EVoting => counts.EVotingVoteCount,
            VotingDataSource.ECounting => counts.ECountingVoteCount,
            _ => throw new ArgumentOutOfRangeException(nameof(dataSource), dataSource, null),
        };
    }

    public static int GetVoteCountOfDataSource(this IHasNullableConventionalVoteCounts counts, VotingDataSource dataSource)
    {
        return dataSource switch
        {
            VotingDataSource.Conventional => counts.ConventionalVoteCount.GetValueOrDefault(),
            VotingDataSource.EVoting => counts.EVotingInclWriteInsVoteCount,
            VotingDataSource.ECounting => counts.ECountingInclWriteInsVoteCount,
            _ => throw new ArgumentOutOfRangeException(nameof(dataSource), dataSource, null),
        };
    }

    public static void SetVoteCountOfDataSource(this IHasVoteCounts counts, VotingDataSource dataSource, int value)
    {
        switch (dataSource)
        {
            case VotingDataSource.Conventional:
                counts.ConventionalVoteCount = value;
                return;
            case VotingDataSource.EVoting:
                counts.EVotingVoteCount = value;
                return;
            case VotingDataSource.ECounting:
                counts.ECountingVoteCount = value;
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(dataSource), dataSource, null);
        }
    }

    public static void AddWriteInsVoteCount(this IHasNullableConventionalVoteCounts counts, VotingDataSource dataSource, int value)
    {
        switch (dataSource)
        {
            case VotingDataSource.EVoting:
                counts.EVotingWriteInsVoteCount += value;
                return;
            case VotingDataSource.ECounting:
                counts.ECountingWriteInsVoteCount += value;
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(dataSource), dataSource, null);
        }
    }

    public static void SetVoteCountOfDataSource(this IHasNullableConventionalVoteCounts counts, VotingDataSource dataSource, int? value, int? writeInVoteCount)
    {
        switch (dataSource)
        {
            case VotingDataSource.Conventional:
                counts.ConventionalVoteCount = value;
                return;
            case VotingDataSource.EVoting:
                counts.EVotingExclWriteInsVoteCount = value.GetValueOrDefault();
                counts.EVotingWriteInsVoteCount = writeInVoteCount.GetValueOrDefault();
                return;
            case VotingDataSource.ECounting:
                counts.ECountingExclWriteInsVoteCount = value.GetValueOrDefault();
                counts.ECountingWriteInsVoteCount = writeInVoteCount.GetValueOrDefault();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(dataSource), dataSource, null);
        }
    }

    public static void AdjustVoteCountOfDataSource(this IHasVoteCounts counts, VotingDataSource dataSource, int value)
    {
        switch (dataSource)
        {
            case VotingDataSource.Conventional:
                counts.ConventionalVoteCount += value;
                return;
            case VotingDataSource.EVoting:
                counts.EVotingVoteCount += value;
                return;
            case VotingDataSource.ECounting:
                counts.ECountingVoteCount += value;
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(dataSource), dataSource, null);
        }
    }

    public static void AdjustVoteCounts(
        this IHasVoteCounts target,
        IHasVoteCounts source,
        int deltaFactor)
    {
        foreach (var dataSource in DataSources)
        {
            target.AdjustVoteCountOfDataSource(dataSource, source.GetVoteCountOfDataSource(dataSource) * deltaFactor);
        }
    }

    public static void AdjustVoteCounts(
        this IHasVoteCounts target,
        IHasNullableConventionalVoteCounts source,
        int deltaFactor)
    {
        foreach (var dataSource in DataSources)
        {
            target.AdjustVoteCountOfDataSource(dataSource, source.GetVoteCountOfDataSource(dataSource) * deltaFactor);
        }
    }
}
