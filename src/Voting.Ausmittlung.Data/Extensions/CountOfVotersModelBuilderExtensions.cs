// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Extensions;

internal static class CountOfVotersModelBuilderExtensions
{
    public static void OwnsCountOfVoters<T>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, PoliticalBusinessNullableCountOfVoters?>> countOfVotersSelector)
        where T : class
    {
        // use adjusted column names since ef core by default
        // just uses one level of names when building nested owned column names.
        // In our scenario this leads to duplicated column names.
        builder.OwnsOne(countOfVotersSelector, x =>
        {
            x.OwnsCountOfVotersSubTotal("CountOfVoters_ConventionalSubTotal_", y => y.ConventionalSubTotal);
            x.OwnsCountOfVotersSubTotal("CountOfVoters_ECountingSubTotal_", y => y.ECountingSubTotal);
            x.OwnsCountOfVotersSubTotal("CountOfVoters_EVotingSubTotal_", y => y.EVotingSubTotal);
            x.Property(y => y.VoterParticipation).IsRequired();
        });
        builder.Navigation(countOfVotersSelector).IsRequired();
    }

    public static void OwnsCountOfVoters<T>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, PoliticalBusinessCountOfVoters?>> countOfVotersSelector)
        where T : class
    {
        // use adjusted column names since ef core by default
        // just uses one level of names when building nested owned column names.
        // In our scenario this leads to duplicated column names.
        builder.OwnsOne(countOfVotersSelector, x =>
        {
            x.OwnsCountOfVotersSubTotal("CountOfVoters_ConventionalSubTotal_", y => y.ConventionalSubTotal);
            x.OwnsCountOfVotersSubTotal("CountOfVoters_ECountingSubTotal_", y => y.ECountingSubTotal);
            x.OwnsCountOfVotersSubTotal("CountOfVoters_EVotingSubTotal_", y => y.EVotingSubTotal);
            x.Property(y => y.VoterParticipation).IsRequired();
        });
        builder.Navigation(countOfVotersSelector).IsRequired();
    }

    private static void OwnsCountOfVotersSubTotal<TOwned, TDependent>(
        this OwnedNavigationBuilder<TOwned, TDependent> builder,
        string columnPrefix,
        Expression<Func<TDependent, PoliticalBusinessCountOfVotersNullableSubTotal?>> subTotalSelector)
        where TOwned : class
        where TDependent : class
    {
        builder.OwnsOne(subTotalSelector, x =>
        {
            x.Property(y => y.AccountedBallots).HasColumnName(columnPrefix + nameof(PoliticalBusinessCountOfVotersSubTotal.AccountedBallots));
            x.Property(y => y.BlankBallots).HasColumnName(columnPrefix + nameof(PoliticalBusinessCountOfVotersSubTotal.BlankBallots));
            x.Property(y => y.InvalidBallots).HasColumnName(columnPrefix + nameof(PoliticalBusinessCountOfVotersSubTotal.InvalidBallots));
            x.Property(y => y.ReceivedBallots).HasColumnName(columnPrefix + nameof(PoliticalBusinessCountOfVotersSubTotal.ReceivedBallots));
        });
        builder.Navigation(subTotalSelector).IsRequired();
    }

    private static void OwnsCountOfVotersSubTotal<TOwned, TDependent>(
        this OwnedNavigationBuilder<TOwned, TDependent> builder,
        string columnPrefix,
        Expression<Func<TDependent, PoliticalBusinessCountOfVotersSubTotal?>> subTotalSelector)
        where TOwned : class
        where TDependent : class
    {
        builder.OwnsOne(subTotalSelector, x =>
        {
            x.Property(y => y.AccountedBallots)
                .HasColumnName(columnPrefix + nameof(PoliticalBusinessCountOfVotersSubTotal.AccountedBallots))
                .IsRequired();
            x.Property(y => y.BlankBallots)
                .HasColumnName(columnPrefix + nameof(PoliticalBusinessCountOfVotersSubTotal.BlankBallots))
                .IsRequired();
            x.Property(y => y.InvalidBallots)
                .HasColumnName(columnPrefix + nameof(PoliticalBusinessCountOfVotersSubTotal.InvalidBallots))
                .IsRequired();
            x.Property(y => y.ReceivedBallots)
                .HasColumnName(columnPrefix + nameof(PoliticalBusinessCountOfVotersSubTotal.ReceivedBallots))
                .IsRequired();
        });
        builder.Navigation(subTotalSelector).IsRequired();
    }
}
