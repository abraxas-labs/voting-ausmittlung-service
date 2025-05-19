// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

    internal string DelimetedTableName => DelimitedSchemaAndTableName;

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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task<List<Guid>> GetHierarchicalLowerOrSelfDomainOfInfluenceIds(Guid domainOfInfluenceId)
    {
        var idColumnName = GetDelimitedColumnName(x => x.Id);
        var parentIdColumnName = GetDelimitedColumnName(x => x.ParentId);

        return await Context.DomainOfInfluences.FromSqlRaw(
                $@"
                WITH RECURSIVE children_or_self AS (
                    SELECT {idColumnName}, {parentIdColumnName}
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} = {{0}}
                    UNION
                    SELECT x.{idColumnName}, x.{parentIdColumnName}
                    FROM {DelimitedSchemaAndTableName} x
                    JOIN children_or_self c ON x.{parentIdColumnName} = c.{idColumnName}
                )
                SELECT * FROM children_or_self",
                domainOfInfluenceId)
            .Select(doi => doi.Id)
            .ToListAsync();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task UpdateInheritedCantons(Guid rootBasisDomainOfInfluenceId, List<Guid> snapshotIds, DomainOfInfluenceCanton rootCanton)
    {
        var idColumnName = GetDelimitedColumnName(x => x.Id);
        var parentIdColumnName = GetDelimitedColumnName(x => x.ParentId);
        var cantonColumnName = GetDelimitedColumnName(x => x.Canton);

        var combinedIds = snapshotIds
            .Append(rootBasisDomainOfInfluenceId)
            .ToList();

        await Context.Database.ExecuteSqlRawAsync(
            $@"
                WITH RECURSIVE d AS (
	                SELECT {idColumnName}
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} =  ANY({{0}})
                    UNION
                    SELECT x.{idColumnName}
                    FROM d
                    JOIN {DelimitedSchemaAndTableName} x ON x.{parentIdColumnName} = d.{idColumnName}
                )
                UPDATE {DelimitedSchemaAndTableName} r
                SET {cantonColumnName} = {{1}}
                FROM d
                WHERE d.{idColumnName} = r.{idColumnName}",
            combinedIds,
            rootCanton);
        await Context.SaveChangesAsync();
    }

    // This fetches all domain of influences with have the exact report level
    // In addition, DOIs with a different report level but also have the HideLowerDoisInReports flag enabled,
    // are also fetched.
    // If a DOI has a lower report level, it means it is higher in the hierarchy and hides DOIs lower in the hierarchy.
    // It takes the place of the hidden DOIs as a DOI result.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task<List<DomainOfInfluenceReportLevelEntry>> GetRelevantDomainOfInfluencesForReportingLevel(Guid domainOfInfluenceId, int reportingLevel)
    {
        var idColumnName = GetDelimitedColumnName(x => x.Id);
        var parentIdColumnName = GetDelimitedColumnName(x => x.ParentId);
        var hideLowerDoisColumnName = GetDelimitedColumnName(x => x.HideLowerDomainOfInfluencesInReports);

        // This fetches all domain of influences of the specified reporting level
        // It also returns DOIs if they have the HideLowerDois flag set, since we need to handle those specially
        var relevantDois = await Context.Database.SqlQueryRaw<DomainOfInfluenceReportLevelEntry>(
            $@"
                WITH RECURSIVE d AS (
	                SELECT {DelimitedSchemaAndTableName}.*, 0 AS depth, FALSE AS hidden
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} = {{0}}
                    UNION
                    SELECT x.*, d.depth + 1, (d.hidden IS TRUE OR d.{hideLowerDoisColumnName} IS TRUE) AS hidden
                    FROM d
                    JOIN {DelimitedSchemaAndTableName} x ON x.{parentIdColumnName} = d.{idColumnName}
                )
                SELECT {idColumnName}, depth AS ""ReportLevel"" FROM d
                WHERE (depth = {{1}} AND hidden IS FALSE)
                OR {hideLowerDoisColumnName} IS TRUE
                ",
            domainOfInfluenceId,
            reportingLevel)
            .ToListAsync();

        var ids = relevantDois.ConvertAll(x => x.Id);
        var doisById = await Query()
            .AsSplitQuery()
            .Where(doi => ids.Contains(doi.Id))
            .Include(doi => doi.CountingCircles)
            .ToDictionaryAsync(x => x.Id, x => x);
        foreach (var doi in relevantDois)
        {
            doi.DomainOfInfluence = doisById[doi.Id];
        }

        return relevantDois;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.", Justification = "Referencing hardened inerpolated string parameters.")]
    public async Task<List<DomainOfInfluence>> GetDoisWithHideLowerDoiInReportsFlag(Guid relevantParentDoiId)
    {
        var idColumnName = GetDelimitedColumnName(x => x.Id);
        var parentIdColumnName = GetDelimitedColumnName(x => x.ParentId);
        var hideLowerDoiInReportsColumnName = GetDelimitedColumnName(x => x.HideLowerDomainOfInfluencesInReports);

        var doiIdsWithFlagSet = await Context.DomainOfInfluences.FromSqlRaw(
            $@"
                WITH RECURSIVE d AS (
	                SELECT {DelimitedSchemaAndTableName}.*
                    FROM {DelimitedSchemaAndTableName}
                    WHERE {idColumnName} = {{0}}
                    UNION
                    SELECT x.*
                    FROM d
                    JOIN {DelimitedSchemaAndTableName} x ON x.{parentIdColumnName} = d.{idColumnName}
                )
                SELECT * FROM d
                WHERE {hideLowerDoiInReportsColumnName} = TRUE
                ",
            relevantParentDoiId)
            .Select(doi => doi.Id)
            .ToListAsync();

        // ef core does not support include on complex sql queries
        return await Query()
            .AsSplitQuery()
            .Where(doi => doiIdsWithFlagSet.Contains(doi.Id))
            .Include(doi => doi.CountingCircles)
            .ToListAsync();
    }

    internal string GetColumnName<TProp>(Expression<Func<DomainOfInfluence, TProp>> memberAccess)
        => GetDelimitedColumnName(memberAccess);
}
