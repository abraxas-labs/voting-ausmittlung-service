// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProportionalElectionUnionEndResultProcessor :
    IEventProcessor<ProportionalElectionUnionEndResultFinalized>,
    IEventProcessor<ProportionalElectionUnionEndResultFinalizationReverted>
{
    private readonly IDbRepository<DataContext, ProportionalElectionUnionEndResult> _endResultRepo;
    private readonly DoubleProportionalResultBuilder _dpResultBuilder;

    public ProportionalElectionUnionEndResultProcessor(
        IDbRepository<DataContext, ProportionalElectionUnionEndResult> endResultRepo,
        DoubleProportionalResultBuilder dpResultBuilder)
    {
        _endResultRepo = endResultRepo;
        _dpResultBuilder = dpResultBuilder;
    }

    public Task Process(ProportionalElectionUnionEndResultFinalized eventData) => SetFinalized(GuidParser.Parse(eventData.ProportionalElectionUnionId), true);

    public Task Process(ProportionalElectionUnionEndResultFinalizationReverted eventData) => SetFinalized(GuidParser.Parse(eventData.ProportionalElectionUnionId), false);

    private async Task SetFinalized(Guid unionId, bool finalized)
    {
        var endResult = await _endResultRepo.Query()
                            .FirstOrDefaultAsync(x => x.ProportionalElectionUnionId == unionId)
                        ?? throw new EntityNotFoundException(unionId);
        endResult.Finalized = finalized;

        if (finalized)
        {
            await _dpResultBuilder.BuildForUnion(unionId);
        }
        else
        {
            await _dpResultBuilder.ResetForUnion(unionId);
        }

        await _endResultRepo.Update(endResult);
    }
}
