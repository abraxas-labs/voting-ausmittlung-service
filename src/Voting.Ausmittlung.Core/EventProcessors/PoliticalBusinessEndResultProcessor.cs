// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file
using System;
using System.Threading.Tasks;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Exceptions;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public abstract class PoliticalBusinessEndResultProcessor
{
    private readonly IDbRepository<DataContext, SimplePoliticalBusiness> _simplePoliticalBusinessRepo;

    protected PoliticalBusinessEndResultProcessor(IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepo)
    {
        _simplePoliticalBusinessRepo = simplePoliticalBusinessRepo;
    }

    protected virtual async Task SetFinalized(Guid politicalBusinessId, bool finalized)
    {
        var pb = await _simplePoliticalBusinessRepo.GetByKey(politicalBusinessId)
            ?? throw new EntityNotFoundException(nameof(SimplePoliticalBusiness), politicalBusinessId);
        pb.EndResultFinalized = finalized;
        await _simplePoliticalBusinessRepo.UpdateIgnoreRelations(pb);
    }
}
