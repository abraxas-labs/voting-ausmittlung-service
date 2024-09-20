// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionUnionEndResultBuilder
{
    private readonly IDbRepository<DataContext, ProportionalElectionUnionEndResult> _unionEndResultRepo;

    public ProportionalElectionUnionEndResultBuilder(IDbRepository<DataContext, ProportionalElectionUnionEndResult> unionEndResultRepo)
    {
        _unionEndResultRepo = unionEndResultRepo;
    }

    public async Task AdjustCountOfDoneElections(Guid electionId, int delta)
    {
        var endResults = await _unionEndResultRepo
            .Query()
            .Where(x => x.ProportionalElectionUnion.ProportionalElectionUnionEntries
                .Any(e => e.ProportionalElectionId == electionId))
            .ToListAsync();

        foreach (var endResult in endResults)
        {
            endResult.CountOfDoneElections += delta;
        }

        await _unionEndResultRepo.UpdateRange(endResults);
    }
}
