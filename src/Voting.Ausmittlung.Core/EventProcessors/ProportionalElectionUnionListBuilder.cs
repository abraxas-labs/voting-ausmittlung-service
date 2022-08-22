// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProportionalElectionUnionListBuilder
{
    private readonly ProportionalElectionUnionListRepo _unionListRepo;
    private readonly ProportionalElectionUnionListTranslationRepo _unionListTranslationRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionUnionEntry> _unionEntryRepo;
    private readonly IDbRepository<DataContext, ProportionalElection> _electionRepo;

    public ProportionalElectionUnionListBuilder(
        ProportionalElectionUnionListRepo unionListRepo,
        ProportionalElectionUnionListTranslationRepo unionListTranslationRepo,
        IDbRepository<DataContext, ProportionalElectionUnionEntry> unionEntryRepo,
        IDbRepository<DataContext, ProportionalElection> electionRepo)
    {
        _unionListRepo = unionListRepo;
        _unionListTranslationRepo = unionListTranslationRepo;
        _unionEntryRepo = unionEntryRepo;
        _electionRepo = electionRepo;
    }

    public async Task RebuildLists(
        Guid unionId,
        List<Guid> proportionalElectionIds)
    {
        var proportionalElections = await _electionRepo
            .Query()
            .AsSplitQuery()
            .IgnoreQueryFilters() // do not filter translations
            .Where(p => proportionalElectionIds.Contains(p.Id))
            .Include(p => p.ProportionalElectionLists)
            .ThenInclude(l => l.Translations)
            .ToListAsync();

        var lists = proportionalElections
            .SelectMany(p => p.ProportionalElectionLists)
            .ToList();

        var listsByOrderNumberAndShortDescription = lists
            .GroupBy(l => l.OrderNumber);

        var unionLists = listsByOrderNumberAndShortDescription
            .Select(l => new ProportionalElectionUnionList(
                unionId,
                l.Key,
                l.First().Translations,
                l.ToList()))
            .ToList();

        await _unionListRepo.Replace(unionId, unionLists);
    }

    public async Task RebuildForProportionalElection(Guid proportionalElectionId)
    {
        var unionIds = await _unionEntryRepo.Query()
            .Where(e => e.ProportionalElectionId == proportionalElectionId)
            .Select(e => e.ProportionalElectionUnionId)
            .ToListAsync();

        await RebuildListsForUnions(unionIds);
    }

    public async Task RemoveListsWithNoEntries()
    {
        var listIdsWithNoEntries = await _unionListRepo.Query()
            .Include(l => l.ProportionalElectionUnionListEntries)
            .Where(l => l.ProportionalElectionUnionListEntries.Count == 0)
            .Select(x => x.Id)
            .ToListAsync();
        await _unionListRepo.DeleteRangeByKey(listIdsWithNoEntries);
    }

    public async Task ChangeListShortDescription(ProportionalElectionList list)
    {
        var affectedUnionLists = await _unionListRepo.Query()
            .Where(u => u.ProportionalElectionUnionListEntries.Any(e => e.ProportionalElectionListId == list.Id) && u.OrderNumber == list.OrderNumber)
            .ToListAsync();

        foreach (var unionList in affectedUnionLists)
        {
            unionList.Translations = list.Translations
                .Select(x => new ProportionalElectionUnionListTranslation { Language = x.Language, ShortDescription = x.ShortDescription })
                .ToList();
        }

        await _unionListTranslationRepo.DeleteTranslationsByUnionLists(affectedUnionLists);
        await _unionListRepo.UpdateRange(affectedUnionLists);
    }

    private async Task RebuildListsForUnions(List<Guid> unionIds)
    {
        // complex groupby are not support in ef
        var electionIdsByUnion = (await _unionEntryRepo.Query()
            .Where(e => unionIds.Contains(e.ProportionalElectionUnionId))
            .ToListAsync())
            .GroupBy(e => e.ProportionalElectionUnionId, e => e.ProportionalElectionId)
            .ToList();

        foreach (var electionIds in electionIdsByUnion)
        {
            await RebuildLists(electionIds.Key, electionIds.ToList());
        }
    }
}
