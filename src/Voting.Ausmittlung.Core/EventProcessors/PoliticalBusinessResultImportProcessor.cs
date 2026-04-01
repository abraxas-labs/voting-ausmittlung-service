// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public abstract class PoliticalBusinessResultImportProcessor
{
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;

    protected PoliticalBusinessResultImportProcessor(IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo)
    {
        _simpleResultRepo = simpleResultRepo;
    }

    protected async Task SetSimpleResultECountingImported(Guid resultId)
    {
        await _simpleResultRepo.Query()
            .Where(ccr => ccr.Id == resultId)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.ECountingImported, _ => true));
    }
}
