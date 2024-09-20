// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Utils;

public class ProportionalElectionUnionEndResultInitializer
{
    private readonly IDbRepository<DataContext, ProportionalElectionUnionEndResult> _endResultRepo;
    private readonly IDbRepository<DataContext, DoubleProportionalResultRow> _electionDpResultRepo;
    private readonly ProportionalElectionUnionRepo _unionRepo;

    public ProportionalElectionUnionEndResultInitializer(
        IDbRepository<DataContext, ProportionalElectionUnionEndResult> endResultRepo,
        ProportionalElectionUnionRepo unionRepo,
        IDbRepository<DataContext, DoubleProportionalResultRow> electionDpResultRepo)
    {
        _endResultRepo = endResultRepo;
        _unionRepo = unionRepo;
        _electionDpResultRepo = electionDpResultRepo;
    }

    internal async Task ResetForUnion(Guid proportionalElectionUnionId)
    {
        var unionEndResultId = await _endResultRepo.Query()
            .Where(x => x.ProportionalElectionUnionId == proportionalElectionUnionId)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        await _endResultRepo.DeleteByKey(unionEndResultId);
        await RebuildForUnion(proportionalElectionUnionId, true);
    }

    internal async Task RebuildForUnion(Guid proportionalElectionUnionId, bool testingPhaseEnded)
    {
        var endResultId = AusmittlungUuidV5.BuildPoliticalBusinessUnionEndResult(proportionalElectionUnionId, testingPhaseEnded);
        var countOfElections = await _unionRepo.CountOfElections(proportionalElectionUnionId);

        var existingEndResult = await _endResultRepo
            .Query()
            .FirstOrDefaultAsync(x => x.ProportionalElectionUnionId == proportionalElectionUnionId);

        if (testingPhaseEnded && existingEndResult != null)
        {
            if (existingEndResult.Id == endResultId)
            {
                throw new InvalidOperationException("Cannot build end result after testing phase ended when it is already built");
            }

            await _endResultRepo.DeleteByKey(existingEndResult.Id);
            existingEndResult = null;
        }

        if (existingEndResult == null)
        {
            existingEndResult = new ProportionalElectionUnionEndResult
            {
                Id = endResultId,
                ProportionalElectionUnionId = proportionalElectionUnionId,
                TotalCountOfElections = countOfElections,
            };
            await _endResultRepo.Create(existingEndResult);
        }
        else
        {
            existingEndResult.TotalCountOfElections = countOfElections;
            await _endResultRepo.Update(existingEndResult);
        }
    }
}
