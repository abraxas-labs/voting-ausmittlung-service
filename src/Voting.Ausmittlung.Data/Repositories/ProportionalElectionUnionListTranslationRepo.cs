// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionUnionListTranslationRepo : TranslationRepo<ProportionalElectionUnionListTranslation>
{
    public ProportionalElectionUnionListTranslationRepo(DataContext context)
        : base(context)
    {
    }

    protected override string MainEntityIdColumnName => GetDelimitedColumnName(x => x.ProportionalElectionUnionListId);

    public async Task DeleteTranslationsByUnionLists(IEnumerable<ProportionalElectionUnionList> unionLists)
    {
        var unionListIds = unionLists.Select(x => x.Id).ToList();
        var idsToDelete = await Query()
            .Where(x => unionListIds.Contains(x.ProportionalElectionUnionListId))
            .Select(x => x.Id)
            .ToListAsync();

        await DeleteRangeByKey(idsToDelete);
    }
}
