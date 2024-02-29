// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.ValueConversion;
using Voting.Ausmittlung.Data.ModelBuilders;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Extensions;

internal static class EntityTypeBuilderExtensions
{
    internal static void HasLanguageQueryFilter<T>(this EntityTypeBuilder<T> builder)
        where T : TranslationEntity
    {
        // Note that we need to use the same DbContext instance here that has been called with OnModelCreating, so that EF Core can recognize the Language field
        // During requests, the correct DbContext instance will be used (that is, the instance that is executing the query, not the one from the DbContextAccessor)
        builder
            .HasQueryFilter(t => DbContextAccessor.DbContext.Language == null || t.Language == DbContextAccessor.DbContext.Language);
    }

    /// <summary>
    /// Sets a int list conversion on a enum list property using the <see cref="NpgsqlPropertyBuilderExtensions"/>.
    /// </summary>
    /// <typeparam name="TEnum">The enum.</typeparam>
    /// <param name="builder">The PropertyBuilder.</param>
    /// <returns>The updated property builder.</returns>
    internal static PropertyBuilder<List<TEnum>> HasPostgresEnumListToIntListConversion<TEnum>(this PropertyBuilder<List<TEnum>> builder)
        where TEnum : struct
    {
        var elementValueConverter = new ValueConverter<TEnum, int>(
                p => (int)(object)p,
                p => (TEnum)(object)p);

        var converter = new NpgsqlArrayConverter<List<TEnum>, List<int>>(elementValueConverter);

        var comparer = new ValueComparer<List<TEnum>>(
            (l, r) => l != null && r != null && l.SequenceEqual(r),
            v => v.GetSequenceHashCode(),
            v => v.ToList());

        builder.HasPostgresArrayConversion<TEnum, int>(elementValueConverter);
        builder.Metadata.SetValueConverter(converter);
        builder.Metadata.SetValueComparer(comparer);

        return builder;
    }
}
