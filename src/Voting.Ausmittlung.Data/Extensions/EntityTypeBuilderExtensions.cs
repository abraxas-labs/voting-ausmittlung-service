// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
}
