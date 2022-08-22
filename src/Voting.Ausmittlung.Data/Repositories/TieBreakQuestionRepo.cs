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

public class TieBreakQuestionRepo : DbRepository<DataContext, TieBreakQuestion>
{
    public TieBreakQuestionRepo(DataContext context)
        : base(context)
    {
    }

    public async Task Replace(Guid ballotId, ICollection<TieBreakQuestion> entries)
    {
        var existingEntries = await Set.Where(e => e.BallotId == ballotId).ToArrayAsync();

        Set.RemoveRange(existingEntries);
        await Context.SaveChangesAsync();

        if (entries.Count == 0)
        {
            return;
        }

        Set.AddRange(entries);
        await Context.SaveChangesAsync();
    }
}
