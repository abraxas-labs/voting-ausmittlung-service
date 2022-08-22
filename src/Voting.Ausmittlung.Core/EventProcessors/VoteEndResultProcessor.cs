// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class VoteEndResultProcessor :
    IEventProcessor<VoteEndResultFinalized>,
    IEventProcessor<VoteEndResultFinalizationReverted>
{
    private readonly IDbRepository<DataContext, VoteEndResult> _repo;

    public VoteEndResultProcessor(IDbRepository<DataContext, VoteEndResult> repo)
    {
        _repo = repo;
    }

    public Task Process(VoteEndResultFinalized eventData)
        => SetFinalized(eventData.VoteId, true);

    public Task Process(VoteEndResultFinalizationReverted eventData)
        => SetFinalized(eventData.VoteId, false);

    private async Task SetFinalized(string politicalBusinessId, bool finalized)
    {
        var endResult = await _repo.Query()
                            .IgnoreQueryFilters() // do not filter translations
                            .Include(x => x.Vote.Translations)
                            .FirstOrDefaultAsync(x => x.VoteId == GuidParser.Parse(politicalBusinessId))
                        ?? throw new EntityNotFoundException(politicalBusinessId);
        endResult.Finalized = finalized;
        await _repo.Update(endResult);
    }
}
