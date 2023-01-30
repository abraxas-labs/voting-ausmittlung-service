// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Repositories;

public class VoteRepo : PoliticalBusinessRepo<Vote>
{
    public VoteRepo(DataContext context)
        : base(context)
    {
    }

    public override IQueryable<Vote> QueryWithResults()
    {
        return Query().AsSplitQuery().Include(v => v.Results).ThenInclude(r => r.CountingCircle);
    }
}
