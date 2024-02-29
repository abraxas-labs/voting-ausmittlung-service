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

    public Task DeleteRelatedTranslations(Guid mainEntityId)
    {
        return Context.Database.ExecuteSqlRawAsync($"DELETE FROM {DelimitedSchemaAndTableName} WHERE {MainEntityIdColumnName} = {{0}}", mainEntityId);
    }
}
