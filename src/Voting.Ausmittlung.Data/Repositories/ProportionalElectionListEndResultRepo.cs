// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Data.Repositories;

public class ProportionalElectionListEndResultRepo : DbRepository<DataContext, ProportionalElectionListEndResult>
{
    public ProportionalElectionListEndResultRepo(DataContext context)
        : base(context)
    {
    }

    public Task<ProportionalElectionListEndResult?> GetByListIdAsTracked(Guid listId)
    {
        return Set
            .AsTracking()
            .Include(x => x.ElectionEndResult)
            .Include(x => x.CandidateEndResults)
            .FirstOrDefaultAsync(x => x.ListId == listId);
    }
}
