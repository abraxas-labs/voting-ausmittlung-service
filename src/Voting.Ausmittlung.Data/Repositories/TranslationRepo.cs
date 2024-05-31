// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Lib.Database.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public abstract class TranslationRepo<T> : DbRepository<DataContext, T>
    where T : BaseEntity, new()
{
    protected TranslationRepo(DataContext context)
        : base(context)
    {
    }

    protected abstract string MainEntityIdColumnName { get; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public Task DeleteRelatedTranslations(Guid mainEntityId)
    {
        return Context.Database.ExecuteSqlRawAsync($"DELETE FROM {DelimitedSchemaAndTableName} WHERE {MainEntityIdColumnName} = {{0}}", mainEntityId);
    }
}
