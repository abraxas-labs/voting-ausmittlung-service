// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class DomainOfInfluenceRepo : DbRepository<DataContext, DomainOfInfluence>
{
    public DomainOfInfluenceRepo(DataContext context)
        : base(context)
    {
    }

    public Task<List<DomainOfInfluence>> ListWithContestsInTestingPhase(Guid basisDomainOfInfluenceId)
    {
        return Query()
            .Where(doi => doi.BasisDomainOfInfluenceId == basisDomainOfInfluenceId && doi.SnapshotContest!.State <= ContestState.TestingPhase)
            .ToListAsync();
    }

    public Task<List<DomainOfInfluence>> ListWithContestsInState(Guid basisDomainOfInfluenceId, params ContestState[] states)
    {
        return Query()
            .Where(doi => doi.BasisDomainOfInfluenceId == basisDomainOfInfluenceId && states.Contains(doi.SnapshotContest!.State))
            .ToListAsync();
    }

    public async Task<List<Guid>> GetHierarchicalGreaterOrSelfDomainOfInfluenceIds(Guid domainOfInfluenceId)
    {
        var idColumnName = GetDelimitedColumnName(x => x.Id);
        var parentIdColumnName = GetDelimitedColumnName(x => x.ParentId);

        return await Context.DomainOfInfluences.FromSqlRaw(
            $@"
                WITH RECURSIVE parents_or_self AS (
                    SELECT {idColumnName}, {parentIdColumnName}
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} = {{0}}
                    UNION
                    SELECT x.{idColumnName}, x.{parentIdColumnName}
                    FROM {DelimitedSchemaAndTableName} x
                    JOIN parents_or_self p ON x.{idColumnName} = p.{parentIdColumnName}
                )
                SELECT * FROM parents_or_self",
            domainOfInfluenceId)
            .Select(doi => doi.Id)
            .ToListAsync();
    }

    public async Task<DomainOfInfluenceCanton> GetRootCanton(Guid domainOfInfluenceId)
    {
        var idColumnName = GetDelimitedColumnName(x => x.Id);
        var parentIdColumnName = GetDelimitedColumnName(x => x.ParentId);
        var cantonColumnName = GetDelimitedColumnName(x => x.Canton);

        return (await Context.DomainOfInfluences.FromSqlRaw(
            $@"
                WITH RECURSIVE parents_or_self AS (
                    SELECT {idColumnName}, {parentIdColumnName}, {cantonColumnName}
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} = {{0}}
                    UNION
                    SELECT x.{idColumnName}, x.{parentIdColumnName}, x.{cantonColumnName}
                    FROM {DelimitedSchemaAndTableName} x
                    JOIN parents_or_self p ON x.{idColumnName} = p.{parentIdColumnName}
                )
                SELECT * FROM parents_or_self
                WHERE {parentIdColumnName} IS NULL",
            domainOfInfluenceId)
            .Select(doi => doi.Canton)
            .ToListAsync()).FirstOrDefault();
    }

    public async Task UpdateInheritedCantons(Guid rootDomainOfInfluenceId, DomainOfInfluenceCanton rootCanton)
    {
        var idColumnName = GetDelimitedColumnName(x => x.Id);
        var parentIdColumnName = GetDelimitedColumnName(x => x.ParentId);
        var cantonColumnName = GetDelimitedColumnName(x => x.Canton);

        await Context.Database.ExecuteSqlRawAsync(
            $@"
                WITH RECURSIVE d AS (
	                SELECT {idColumnName}
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} = {{0}}
                    UNION
                    SELECT x.{idColumnName}
                    FROM d
                    JOIN {DelimitedSchemaAndTableName} x ON x.{parentIdColumnName} = d.{idColumnName}
                )
                UPDATE {DelimitedSchemaAndTableName} r
                SET {cantonColumnName} = {{1}}
                FROM d
                WHERE d.{idColumnName} = r.{idColumnName}",
            rootDomainOfInfluenceId,
            rootCanton);
        await Context.SaveChangesAsync();
    }

    public async Task<List<DomainOfInfluence>> GetDomainOfInfluencesByReportingLevel(Guid domainOfInfluenceId, int reportingLevel)
    {
        var idColumnName = GetDelimitedColumnName(x => x.Id);
        var parentIdColumnName = GetDelimitedColumnName(x => x.ParentId);

        var doiIds = await Context.DomainOfInfluences.FromSqlRaw(
            $@"
                WITH RECURSIVE d AS (
	                SELECT {DelimitedSchemaAndTableName}.*, 0 AS depth
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} = {{0}}
                    UNION
                    SELECT x.*, d.depth + 1
                    FROM d
                    JOIN {DelimitedSchemaAndTableName} x ON x.{parentIdColumnName} = d.{idColumnName}
                )
                SELECT * FROM d
                WHERE depth = {{1}}
                ",
            domainOfInfluenceId,
            reportingLevel)
            .Select(doi => doi.Id)
            .ToListAsync();

        // ef core does not support include on complex sql queries
        return await Query()
            .AsSplitQuery()
            .Where(doi => doiIds.Contains(doi.Id))
            .Include(doi => doi.CountingCircles)
            .ToListAsync();
    }
}
