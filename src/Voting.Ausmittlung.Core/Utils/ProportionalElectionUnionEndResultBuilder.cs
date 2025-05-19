// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionUnionEndResultBuilder
{
    private readonly IDbRepository<DataContext, ProportionalElection> _electionRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionUnionEndResult> _unionEndResultRepo;
    private readonly ProportionalElectionEndResultRepo _electionEndResultRepo;

    public ProportionalElectionUnionEndResultBuilder(
        IDbRepository<DataContext, ProportionalElectionUnionEndResult> unionEndResultRepo,
        ProportionalElectionEndResultRepo electionEndResultRepo,
        IDbRepository<DataContext, ProportionalElection> electionRepo)
    {
        _unionEndResultRepo = unionEndResultRepo;
        _electionEndResultRepo = electionEndResultRepo;
        _electionRepo = electionRepo;
    }

    public async Task AdjustCountOfDoneElections(Guid electionId, int delta)
    {
        var endResults = await GetEndResults(electionId);

        foreach (var endResult in endResults)
        {
            endResult.CountOfDoneElections += delta;
        }

        await _unionEndResultRepo.UpdateRange(endResults);
    }

    public async Task AdjustElectionsCount(Guid electionId, int delta)
    {
        var endResults = await GetEndResults(electionId);
        var allCountingCirclesDone = (await _electionEndResultRepo
            .Query()
            .FirstOrDefaultAsync(x => x.ProportionalElectionId == electionId))?.AllCountingCirclesDone
          ?? throw new EntityNotFoundException(nameof(ProportionalElectionEndResult), electionId);

        foreach (var endResult in endResults)
        {
            endResult.TotalCountOfElections += delta;

            if (allCountingCirclesDone)
            {
                endResult.CountOfDoneElections += delta;
            }
        }

        await _unionEndResultRepo.UpdateRange(endResults);
    }

    private Task<List<ProportionalElectionUnionEndResult>> GetEndResults(Guid electionId)
    {
        return _unionEndResultRepo
            .Query()
            .Where(x => x.ProportionalElectionUnion.ProportionalElectionUnionEntries
                .Any(e => e.ProportionalElectionId == electionId))
            .ToListAsync();
    }
}
